using System.Globalization;
using PJeCalc.Core.Services.Inss;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da aplicação por ocorrência do INSS (<see cref="ApuracaoInssPorOcorrencia"/>)
/// contra o motor oficial do PJe-Calc (Java): correção, juros e multa das quatro cotas,
/// incluindo o truncamento dos juros previdenciários. Casos em
/// Fixtures/golden_inss_ocorrencia.csv, com as mesmas entradas que geraram os golden.
/// </summary>
public class InssOcorrenciaGoldenTests
{
    private static readonly OcorrenciaDeInssEntrada Previdenciario = new()
    {
        IndiceTrabalhista = 1m,
        IndicePrevidenciaria = 1.35m,
        TaxaDeJuros = 12.5m,
        TaxaDeMulta = 8m,
        JurosEMultaPrevidenciario = true,
        ValorDevidoSegurado = 200.00m,
        ValorDevidoEmpresa = 300.33m,
        ValorDevidoSAT = 15.07m,
        ValorDevidoTerceiros = 57.19m,
    };

    private static readonly OcorrenciaDeInssEntrada Trabalhista = new()
    {
        IndiceTrabalhista = 1.2m,
        IndicePrevidenciaria = 1m,
        TaxaDeJuros = 12.5m,
        TaxaDeMulta = 0m,
        JurosEMultaPrevidenciario = false,
        ValorDevidoSegurado = 200.00m,
        ValorDevidoEmpresa = 300.33m,
        ValorDevidoSAT = 15.07m,
        ValorDevidoTerceiros = 57.19m,
    };

    private static readonly IReadOnlyDictionary<(string, string), decimal[]> Esperado = CarregarGolden();

    [Theory]
    [InlineData("previdenciario")]
    [InlineData("trabalhista")]
    public void Ocorrencia_bate_com_o_motor_oficial(string cenario)
    {
        var entrada = cenario == "previdenciario" ? Previdenciario : Trabalhista;
        var (segurado, empresa, sat, terceiros) = ApuracaoInssPorOcorrencia.Calcular(entrada);

        Conferir(cenario, "segurado", segurado);
        Conferir(cenario, "empresa", empresa);
        Conferir(cenario, "sat", sat);
        Conferir(cenario, "terceiros", terceiros);
    }

    [Fact]
    public void Juros_previdenciarios_truncam_mas_a_multa_nao()
    {
        // Empresa: corrigido 405,45; juros 405,45 x 12,5% = 50,68125 -> trunca para 50,68.
        var cota = ApuracaoInssPorOcorrencia.CalcularCota(Previdenciario.ValorDevidoEmpresa, Previdenciario);
        Assert.Equal(405.45m, cota.Corrigido);
        Assert.Equal(50.68m, cota.Juros);            // truncado (não 50,69)
        Assert.Equal(32.4360m, cota.Multa);          // não truncado
    }

    private static void Conferir(string cenario, string cota, CotaDeInss obtido)
    {
        var e = Esperado[(cenario, cota)];
        Assert.Equal(e[0], obtido.Corrigido);
        Assert.Equal(e[1], obtido.Juros);
        Assert.Equal(e[2], obtido.Multa);
        Assert.Equal(e[3], obtido.Total);
    }

    private static Dictionary<(string, string), decimal[]> CarregarGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_inss_ocorrencia.csv");
        var mapa = new Dictionary<(string, string), decimal[]>();
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            mapa[(c[0], c[2])] = [Num(c[3]), Num(c[4]), Num(c[5]), Num(c[6])];
        }
        return mapa;
    }

    private static decimal Num(string s) => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
