using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação dos regimes de alíquota fixa (1%/0,5%/0,0333% a.d.) e da projeção da data
/// inicial dos juros (mês seguinte ao vencimento / piso do ajuizamento) contra o motor
/// oficial do PJe-Calc (Java) dirigido headless.
/// </summary>
public class JurosFixoProjecaoGoldenTests
{
    private static readonly TabelaDeJurosService Service = new(new CsvJurosFaixaProvider());

    private static IEnumerable<string[]> Ler(string arquivo) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", arquivo))
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'));

    private static DateOnly Data(string s) => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static decimal Num(string s) => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> Fixos() =>
        Ler("golden_juros_fixo.csv").Select(c => new object[]
        {
            Enum.Parse<JurosEnum>(c[0]), Data(c[1]), Data(c[2]), Num(c[3]),
        });

    public static IEnumerable<object[]> Projecoes() =>
        Ler("golden_juros_projecao.csv").Select(c => new object[]
        {
            Enum.Parse<JurosEnum>(c[0]), Data(c[1]), Data(c[2]), Data(c[3]), Data(c[4]), Num(c[5]),
        });

    [Theory]
    [MemberData(nameof(Fixos))]
    public void Regime_fixo_bate_com_o_motor_oficial(
        JurosEnum regime, DateOnly inicio, DateOnly liquidacao, decimal taxaEsperada)
    {
        AssertProximo(taxaEsperada, Service.CalcularTaxaAcumulada(regime, inicio, liquidacao));
    }

    [Theory]
    [MemberData(nameof(Projecoes))]
    public void Projecao_e_taxa_batem_com_o_motor_oficial(
        JurosEnum regime, DateOnly vencimento, DateOnly ajuizamento, DateOnly liquidacao,
        DateOnly inicioEsperado, decimal taxaEsperada)
    {
        var inicio = TabelaDeJurosService.ProjetarInicioDosJuros(vencimento, ajuizamento, aplicarFasePreJudicial: false);
        Assert.Equal(inicioEsperado, inicio);

        AssertProximo(taxaEsperada, Service.CalcularTaxaAcumulada(regime, inicio, liquidacao));
    }

    [Fact]
    public void Fase_pre_judicial_ignora_o_piso_do_ajuizamento()
    {
        // Vencimento em 05/2015 com ajuizamento em 01/2016: sem pré-judicial começa no
        // ajuizamento+1; com pré-judicial começa no mês seguinte ao vencimento.
        var venc = new DateOnly(2015, 5, 20);
        var ajuiz = new DateOnly(2016, 1, 10);

        Assert.Equal(new DateOnly(2016, 1, 11),
            TabelaDeJurosService.ProjetarInicioDosJuros(venc, ajuiz, aplicarFasePreJudicial: false));
        Assert.Equal(new DateOnly(2015, 6, 1),
            TabelaDeJurosService.ProjetarInicioDosJuros(venc, ajuiz, aplicarFasePreJudicial: true));
    }

    private static void AssertProximo(decimal esperado, decimal obtido)
    {
        var escala = Math.Max(Math.Abs(esperado), 1m);
        Assert.True(Math.Abs(obtido - esperado) <= escala * 0.0000000001m,
            $"Obtido {obtido} distante do esperado {esperado}.");
    }
}
