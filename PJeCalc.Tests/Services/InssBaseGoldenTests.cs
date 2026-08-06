using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Inss;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a geração da base do INSS sobre as verbas (harness
/// <c>tools/golden/GoldenGenInssBase.java</c>, que dirige <c>calcularValorBaseVerbas</c> por
/// reflection) e a cota do segurado devido (base × alíquota efetiva do total, com teto).
/// </summary>
public sealed class InssBaseGoldenTests
{
    [Fact]
    public void Base_das_verbas_por_competencia_bate_com_o_motor_oficial()
    {
        var verbas = Verbas();
        var calculado = new Dictionary<string, decimal>
        {
            ["2020-03_comum"] = GeradorDaBaseDeInss.BaseVerbasNoMes(verbas, new(2020, 3, 1), decimoTerceiro: false),
            ["2020-03_decimoterceiro"] = GeradorDaBaseDeInss.BaseVerbasNoMes(verbas, new(2020, 3, 1), decimoTerceiro: true),
            ["2020-12_comum"] = GeradorDaBaseDeInss.BaseVerbasNoMes(verbas, new(2020, 12, 1), decimoTerceiro: false),
            ["2020-12_decimoterceiro"] = GeradorDaBaseDeInss.BaseVerbasNoMes(verbas, new(2020, 12, 1), decimoTerceiro: true),
        };

        var golden = LerGolden();
        Assert.NotEmpty(golden);
        foreach (var (cenario, esperado) in golden)
        {
            Assert.True(calculado.ContainsKey(cenario), $"cenário não reproduzido: {cenario}");
            Assert.Equal(esperado, calculado[cenario]);
        }
    }

    [Fact]
    public void Segurado_devido_incide_sobre_as_verbas_com_teto()
    {
        // Tabela pré-reforma: alíquota única de 11% para qualquer base.
        var tabela = new TabelaPrevidenciariaDoSegurado
        {
            Competencia = new(2019, 1, 1),
            Faixa1 = new FaixaPrevidenciaria(0m, null, 11m),
        };

        var semTeto = ApuracaoDoInssSegurado.Calcular(baseHistorico: 2000m, baseVerbas: 1000m, tabela);
        Assert.Equal(220m, semTeto.SeguradoSobreHistorico); // 2000 × 11%
        Assert.Equal(110m, semTeto.SeguradoDevido);         // 1000 × 11%

        var comTeto = ApuracaoDoInssSegurado.Calcular(2000m, 1000m, tabela, tetoSegurado: 250m);
        Assert.Equal(220m, comTeto.SeguradoSobreHistorico);
        Assert.Equal(30m, comTeto.SeguradoDevido);          // min(250 − 220, 110)
    }

    private static List<VerbaEmCalculo> Verbas()
    {
        var a = Nova("A", CaracteristicaDaVerbaEnum.Comum);
        Add(a, new(2020, 3, 1), 3000m, 0m);
        Add(a, new(2020, 12, 1), 2000m, 0m);

        var b = Nova("B", CaracteristicaDaVerbaEnum.Comum);
        Add(b, new(2020, 3, 1), 500m, 200m);

        var c = Nova("C", CaracteristicaDaVerbaEnum.DecimoTerceiroSalario);
        Add(c, new(2020, 12, 1), 2500m, 0m);

        return [a, b, c];
    }

    private static VerbaEmCalculo Nova(string nome, CaracteristicaDaVerbaEnum caracteristica) =>
        new() { Nome = nome, Tipo = TipoDaVerbaEnum.Informada, Caracteristica = caracteristica };

    private static void Add(VerbaEmCalculo verba, DateOnly competencia, decimal devido, decimal pago) =>
        verba.Ocorrencias.Add(new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = competencia,
            DataFinal = PeriodoDeApuracao.UltimoDiaDoMes(competencia),
            Devido = devido,
            Pago = pago,
            Ativo = true,
        });

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_inss_base.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => c[0], c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
