using System.Globalization;
using PJeCalc.Core.Services.Irpf;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida o rateio das deduções da base do IRPF pelos baldes no regime de caixa
/// (<see cref="AlocadorDeDeducoesDoIrpf"/>) contra o motor oficial — cenário C1 do harness
/// <c>tools/golden/GoldenGenIrpfGeracao.java</c>, que liga as deduções de previdência privada,
/// pensão e honorários e lê, por tipo de ocorrência, a soma rateada na base.
/// </summary>
public sealed class IrpfAlocacaoDeducoesGoldenTests
{
    private const decimal Tolerancia = 1e-10m;

    [Fact]
    public void Rateio_das_deducoes_no_regime_de_caixa_bate_com_o_motor()
    {
        // Mesmas verbas do cenário C1: férias 5000, 13º 3000, comum 4000 (sem juros).
        var verbas = new[]
        {
            new VerbaParaIrpf
            {
                Caracteristica = CaracteristicaParaIrpf.Ferias,
                DataInicial = new DateOnly(2024, 1, 5),
                DiferencaCorrigida = 5000m,
                BaseParaIncidencias = 5000m,
            },
            new VerbaParaIrpf
            {
                Caracteristica = CaracteristicaParaIrpf.DecimoTerceiroSalario,
                DataInicial = new DateOnly(2024, 1, 5),
                DiferencaCorrigida = 3000m,
                BaseParaIncidencias = 3000m,
            },
            new VerbaParaIrpf
            {
                Caracteristica = CaracteristicaParaIrpf.Demais,
                DataInicial = new DateOnly(2024, 1, 5),
                DiferencaCorrigida = 4000m,
                BaseParaIncidencias = 4000m,
            },
        };

        var ctx = new ContextoDeAlocacaoDeDeducoes
        {
            PrevidenciaPrivadaTotal = 500m,
            PensaoTributavel = 300m,
            HonorariosDevidosPeloReclamante = 400m,
            BrutoDevidoAoReclamante = 10000m,
        };

        var d = AlocadorDeDeducoesDoIrpf.AlocarRegimeCaixa(verbas, ctx);
        var golden = LerGolden();

        Conferir(golden["C1_CAIXA_DEDUCOES;TRIBUTACAO_EXCLUSIVA.deducoesRateadas"], d.DecimoTerceiro);
        Conferir(golden["C1_CAIXA_DEDUCOES;TRIBUTACAO_EM_SEPARADO.deducoesRateadas"], d.Ferias);
        Conferir(golden["C1_CAIXA_DEDUCOES;NORMAL.deducoesRateadas"], d.DemaisVerbas);
    }

    private static void Conferir(decimal esperado, decimal obtido)
    {
        var limite = Tolerancia * Math.Max(1m, Math.Abs(esperado));
        Assert.True(Math.Abs(obtido - esperado) <= limite, $"esperado {esperado}, obtido {obtido}");
    }

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_irpf_geracao.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l) && l.StartsWith("C1_CAIXA_DEDUCOES", StringComparison.Ordinal))
            .Select(l => l.Split(';'))
            .ToDictionary(c => $"{c[0]};{c[1]}", c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
