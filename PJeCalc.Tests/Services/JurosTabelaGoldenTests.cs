using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da taxa acumulada de juros por faixas (<see cref="TabelaDeJurosService"/>)
/// contra o motor oficial do PJe-Calc (Java), dirigido headless em modo teste. Casos em
/// Fixtures/golden_juros_tabela.csv (regime JurosPadrão: 0,5% → 1% composto → 1% simples).
/// </summary>
public class JurosTabelaGoldenTests
{
    private static readonly TabelaDeJurosService Service = new(new CsvJurosFaixaProvider());

    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_juros_tabela.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return
            [
                Enum.Parse<JurosEnum>(c[1]),                                            // regime
                DateOnly.ParseExact(c[2], "yyyy-MM-dd", CultureInfo.InvariantCulture),  // início dos juros
                DateOnly.ParseExact(c[3], "yyyy-MM-dd", CultureInfo.InvariantCulture),  // liquidação
                decimal.Parse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture),  // taxa (Java)
            ];
        }
    }

    [Theory]
    [MemberData(nameof(Golden))]
    public void Taxa_acumulada_bate_com_o_motor_oficial(
        JurosEnum regime, DateOnly inicio, DateOnly liquidacao, decimal taxaEsperada)
    {
        var taxa = Service.CalcularTaxaAcumulada(regime, inicio, liquidacao);

        var escala = Math.Max(Math.Abs(taxaEsperada), 1m);
        Assert.True(Math.Abs(taxa - taxaEsperada) <= escala * 0.0000000001m,
            $"Taxa {taxa} distante do esperado {taxaEsperada}.");
    }

    [Fact]
    public void Sem_juros_e_liquidacao_anterior_dao_zero()
    {
        Assert.Equal(0m, Service.CalcularTaxaAcumulada(JurosEnum.SemJuros, new(2015, 1, 1), new(2020, 1, 1)));
        Assert.Equal(0m, Service.CalcularTaxaAcumulada(JurosEnum.JurosPadrao, new(2020, 1, 1), new(2015, 1, 1)));
    }
}
