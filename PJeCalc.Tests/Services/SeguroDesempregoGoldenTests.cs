using System.Globalization;
using PJeCalc.Core.Services.SeguroDesemprego;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a fórmula do valor da parcela do seguro-desemprego (duas faixas + piso/teto) contra o
/// motor oficial (harness <c>tools/golden/GoldenGenSeguroDesemprego.java</c>, que dirige
/// <c>encontraOValorDoSeguroDesemprego</c> por reflection) e a apuração (parcela × número de
/// parcelas, corrigida e com juros).
/// </summary>
public sealed class SeguroDesempregoGoldenTests
{
    private const decimal Tolerancia = 1e-10m;

    [Fact]
    public void Valor_da_parcela_bate_com_o_motor_oficial()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var tabela = new EfSeguroDesempregoProvider(contexto).ObterPara(new(2021, 1, 1));
        Assert.NotNull(tabela);

        var golden = LerGolden();
        Assert.NotEmpty(golden);
        foreach (var (cenario, esperado) in golden)
        {
            var remuneracao = decimal.Parse(cenario.Split('_')[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            var parcela = tabela!.ValorDaParcela(remuneracao);
            Assert.True(Math.Abs(parcela - esperado) <= Tolerancia * Math.Max(1m, Math.Abs(esperado)),
                $"{cenario}: esperado {esperado}, obtido {parcela}");
        }
    }

    [Fact]
    public void Apuracao_multiplica_parcela_por_numero_de_parcelas_e_corrige()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var tabela = new EfSeguroDesempregoProvider(contexto).ObterPara(new(2021, 1, 1))!;

        var resultado = ApuracaoDoSeguroDesemprego.Apurar(
            tabela, remuneracaoMensal: 1500m, numeroDeParcelas: 4, indiceDeCorrecao: 1.1m, taxaDeJuros: 2m);

        Assert.Equal(1200m, resultado.ValorDaParcela);        // 1500 × 80%
        Assert.Equal(4800m, resultado.ValorDevido);           // 1200 × 4
        Assert.Equal(5280m, resultado.ValorDevidoCorrigido);  // 4800 × 1,1
        Assert.Equal(105.60m, resultado.Juros);               // 5280 × 2%
        Assert.Equal(5385.60m, resultado.Total);
    }

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_seguro_desemprego.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => c[0], c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
