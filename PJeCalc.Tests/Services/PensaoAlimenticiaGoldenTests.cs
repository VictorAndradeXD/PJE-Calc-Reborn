using System.Globalization;
using PJeCalc.Core.Services.PensaoAlimenticia;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a apuração da pensão alimentícia (bases sobre as verbas com incidência de pensão + FGTS,
/// alíquota sobre o total) contra o motor oficial (harness <c>tools/golden/GoldenGenPensao.java</c>,
/// que dirige <c>MaquinaDeCalculoDePensaoAlimenticia.liquidar</c> e lê <c>getValorDevido</c>).
/// </summary>
public sealed class PensaoAlimenticiaGoldenTests
{
    [Fact]
    public void Apuracao_bate_com_o_motor_oficial()
    {
        var contexto = new ContextoDaPensao
        {
            Aliquota = 30.00m,
            IncidirSobreJuros = false,
            Verbas =
            [
                new VerbaParaPensao { DiferencaCorrigida = 2000m, IncidenciaIrpf = true },
                new VerbaParaPensao { DiferencaCorrigida = 1000m, IncidenciaIrpf = false },
            ],
            BaseFgts = 5000m,
            BaseMultaFgts = 2000m,
        };

        var resultado = ApuracaoDaPensaoAlimenticia.Apurar(contexto);
        var golden = LerGolden();

        Assert.Equal(golden["baseVerbas"], resultado.BaseVerbas);
        Assert.Equal(golden["baseVerbasTributaveis"], resultado.BaseVerbasTributaveis);
        Assert.Equal(golden["baseFgts"], resultado.BaseFgts);
        Assert.Equal(golden["baseMultaFgts"], resultado.BaseMultaFgts);
        Assert.Equal(golden["valorDevido"], resultado.ValorDevido);
    }

    [Fact]
    public void Incidindo_sobre_juros_ferias_entram_proporcionalmente()
    {
        // Verba de férias: base = férias gozadas (600); juros proporcional = 100 × (600/1000) = 60.
        var contexto = new ContextoDaPensao
        {
            Aliquota = 10m,
            IncidirSobreJuros = true,
            Verbas =
            [
                new VerbaParaPensao
                {
                    DiferencaCorrigida = 1000m,
                    DiferencaCorrigidaDeFeriasGozadas = 600m,
                    Juros = 100m,
                    Ferias = true,
                    IncidenciaIrpf = false,
                },
            ],
        };

        var resultado = ApuracaoDaPensaoAlimenticia.Apurar(contexto);

        Assert.Equal(660m, resultado.BaseVerbas);        // 600 + round2(100 × 0,6)
        Assert.Equal(66m, resultado.ValorDevido);        // 660 × 10%
    }

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_pensao.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => c[1], c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
