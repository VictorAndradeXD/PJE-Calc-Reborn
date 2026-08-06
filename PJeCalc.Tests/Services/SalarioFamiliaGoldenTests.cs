using System.Globalization;
using PJeCalc.Core.Services.SalarioFamilia;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a seleção de faixa do salário-família contra o motor oficial (harness
/// <c>tools/golden/GoldenGenSalarioFamilia.java</c>, sobre a tabela real TBTABELASALARIOFAMILIA)
/// e a apuração mensal (quebra em meses, cota × filhos, proporcionalidade na admissão/demissão).
/// </summary>
public sealed class SalarioFamiliaGoldenTests
{
    private static EfSalarioFamiliaProvider Provider(ReferenciaDbContext ctx) => new(ctx);

    [Fact]
    public void Selecao_de_faixa_bate_com_o_motor_oficial()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var provider = Provider(contexto);
        var golden = LerGolden();

        Assert.NotEmpty(golden);
        foreach (var (cenario, esperado) in golden)
        {
            var partes = cenario.Split('_');
            var competencia = DateOnly.ParseExact(partes[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var remuneracao = decimal.Parse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture);

            var tabela = provider.ObterPorCompetencia(competencia);
            Assert.NotNull(tabela);
            var cota = tabela!.ObterCota(remuneracao);

            if (esperado is null)
                Assert.Null(cota);
            else
                Assert.Equal(esperado.Value, cota);
        }
    }

    [Fact]
    public void Apuracao_mensal_multiplica_cota_por_filhos_e_proporcionaliza()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var provider = Provider(contexto);

        var config = new ContextoDoSalarioFamilia
        {
            DataInicial = new(2021, 3, 10),
            DataFinal = new(2021, 6, 20),
            Admissao = new(2021, 3, 10),
            Demissao = new(2021, 6, 20),
            FilhosNoMes = _ => 2,
            RemuneracaoNoMes = _ => 800m, // faixa 1 de 2021 → cota 51,27
            TabelaNoMes = provider.ObterPorCompetencia,
        };

        var resultado = ApuracaoDoSalarioFamilia.Apurar(config);

        Assert.Equal(4, resultado.Ocorrencias.Count);
        // Março (admissão dia 10): 22 dias → 51,27 × 22/30 × 2 filhos.
        Assert.Equal(Round2(51.27m * 22m / 30m * 2m), resultado.Ocorrencias[0].ValorDevido);
        // Abril e maio cheios: 51,27 × 2.
        Assert.Equal(Round2(51.27m * 2m), resultado.Ocorrencias[1].ValorDevido);
        Assert.Equal(Round2(51.27m * 2m), resultado.Ocorrencias[2].ValorDevido);
        // Junho (demissão dia 20): 20 dias → 51,27 × 20/30 × 2.
        Assert.Equal(Round2(51.27m * 20m / 30m * 2m), resultado.Ocorrencias[3].ValorDevido);

        var total = resultado.Ocorrencias.Sum(o => o.ValorDevido);
        Assert.Equal(total, resultado.TotalDevido);
    }

    [Fact]
    public void Acima_da_segunda_faixa_nao_ha_beneficio()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var tabela = Provider(contexto).ObterPorCompetencia(new(2021, 1, 1));

        Assert.Null(tabela!.ObterCota(5000m));

        var config = new ContextoDoSalarioFamilia
        {
            DataInicial = new(2021, 1, 1),
            DataFinal = new(2021, 1, 31),
            Admissao = new(2020, 1, 1),
            FilhosNoMes = _ => 3,
            RemuneracaoNoMes = _ => 5000m,
            TabelaNoMes = new EfSalarioFamiliaProvider(contexto).ObterPorCompetencia,
        };
        var resultado = ApuracaoDoSalarioFamilia.Apurar(config);
        Assert.Equal(0m, resultado.TotalDevido);
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.ToEven);

    private static Dictionary<string, decimal?> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_salario_familia.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(
                c => c[0],
                c => string.IsNullOrWhiteSpace(c[2])
                    ? (decimal?)null
                    : decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
