using System.Globalization;
using PJeCalc.Core.Services.Irpf;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da apuração do IRPF (<see cref="ApuracaoDeIrpf"/>) contra o motor oficial do
/// PJe-Calc (Java): base tributável, seleção de faixa e imposto devido, no regime normal e
/// no de rendimentos acumulados (RRA — tabela × NM). A tabela progressiva vem do banco
/// oficial do TRT-8 (Fixtures/Irpf/irpf_tabela.csv). Casos em Fixtures/golden_irpf.csv.
/// </summary>
public class IrpfGoldenTests
{
    private static readonly Dictionary<DateOnly, TabelaIrpf> Tabelas = CarregarTabelas();

    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_irpf.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            // competencia;base;nm;verbas;juros;inss;pensao;honorarios;dependentes;faixaInicial;faixaFinal;aliquota;deducao;imposto
            var c = linha.Split(';');
            var entrada = new OcorrenciaDeIrpfEntrada
            {
                Verbas = Num(c[3]),
                Juros = Num(c[4]),
                ContribuicaoSocial = Num(c[5]),
                PensaoAlimenticia = Num(c[6]),
                Honorarios = Num(c[7]),
                Dependentes = Num(c[8]),
                NumeroDeMeses = int.Parse(c[2], CultureInfo.InvariantCulture),
            };
            yield return [Data(c[0]), entrada, Num(c[1]), Num(c[11]), Num(c[12]), Num(c[13])];
        }
    }

    [Theory]
    [MemberData(nameof(Golden))]
    public void Imposto_bate_com_o_motor_oficial(
        DateOnly competencia, OcorrenciaDeIrpfEntrada entrada,
        decimal baseEsperada, decimal aliquotaEsperada, decimal deducaoEsperada, decimal impostoEsperado)
    {
        var r = ApuracaoDeIrpf.Calcular(Tabelas[competencia], entrada);

        Assert.Equal(baseEsperada, r.Base);
        Assert.Equal(aliquotaEsperada, r.Aliquota);
        Assert.Equal(deducaoEsperada, r.Deducao);
        Assert.Equal(impostoEsperado, r.Imposto);
    }

    [Fact]
    public void Rra_multiplica_os_limites_das_faixas_pelo_numero_de_meses()
    {
        var tabela = Tabelas[new DateOnly(2024, 2, 1)];

        // Base de 53.400 com NM=12: cai na 4ª faixa (fim 4.664,68 x 12 = 55.976,16).
        var faixa = tabela.ObterFaixaParaValor(53400m, numeroDeMeses: 12);
        Assert.Equal(22.50m, faixa.Aliquota);

        // A mesma base sem RRA estaria muito acima do teto, na última faixa.
        var faixaNormal = tabela.ObterFaixaParaValor(53400m);
        Assert.Equal(27.50m, faixaNormal.Aliquota);
    }

    [Fact]
    public void Base_tem_piso_zero()
    {
        var r = ApuracaoDeIrpf.Calcular(Tabelas[new DateOnly(2024, 2, 1)], new OcorrenciaDeIrpfEntrada
        {
            Verbas = 1000m,
            ContribuicaoSocial = 2000m, // dedução maior que a base
        });

        Assert.Equal(0m, r.Base);
        Assert.Equal(0m, r.Imposto);
    }

    private static Dictionary<DateOnly, TabelaIrpf> CarregarTabelas()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Irpf", "irpf_tabela.csv");
        var tabelas = new Dictionary<DateOnly, TabelaIrpf>();

        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(',').Select(campo => campo.Trim().Trim('"')).ToArray();
            var competencia = Data(c[0]);
            tabelas[competencia] = new TabelaIrpf
            {
                Competencia = competencia,
                Faixa1 = Faixa(c[1], c[2], c[3], c[4])!,
                Faixa2 = Faixa(c[5], c[6], c[7], c[8]),
                Faixa3 = Faixa(c[9], c[10], c[11], c[12]),
                Faixa4 = Faixa(c[13], c[14], c[15], c[16]),
                Faixa5 = Faixa(c[17], c[18], c[19], c[20]),
                DeducaoPorDependente = Num(c[21]),
                DeducaoAposentadoMaior65 = Num(c[22]),
            };
        }
        return tabelas;
    }

    private static FaixaFiscal? Faixa(string inicial, string final, string aliquota, string deducao) =>
        string.IsNullOrWhiteSpace(aliquota)
            ? null
            : new FaixaFiscal(Num(inicial), Opcional(final), Num(aliquota), Num(deducao));

    private static DateOnly Data(string s) =>
        DateOnly.ParseExact(s.Trim().Trim('"'), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static decimal Num(string s) =>
        string.IsNullOrWhiteSpace(s) ? 0m : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static decimal? Opcional(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
