using System.Globalization;
using PJeCalc.Core.Services;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida os agregados de crédito do reclamante contra o motor oficial (harness
/// <c>tools/golden/GoldenGenLiquido.java</c>: <c>calculaValorVerbaParaCreditoDoReclamante</c> e
/// <c>calculaValorFgtsParaCreditoDoReclamante</c>) e a montagem do líquido (crédito − descontos).
/// </summary>
public sealed class LiquidoGoldenTests
{
    [Fact]
    public void Agregados_de_credito_batem_com_o_motor_oficial()
    {
        var golden = LerGolden();

        var creditoVerbas = LiquidoDevidoAoReclamante.CreditoDeVerbas(
            principalCorrigido: 5000m, jurosDeMora: 1000m,
            salarioFamilia: 300m, seguroDesemprego: 800m, multaDoArtigo467: 600m);
        Assert.Equal(golden["SEM_DEDUCAO;creditoVerbas"], creditoVerbas);

        var creditoFgts = LiquidoDevidoAoReclamante.CreditoDeFgts(fgtsCorrigido: 3000m, multaDoFgts: 1200m);
        Assert.Equal(golden["SEM_DEDUCAO;creditoFgts"], creditoFgts);

        var creditoFgtsComDeducao = LiquidoDevidoAoReclamante.CreditoDeFgts(
            fgtsCorrigido: 3000m, multaDoFgts: 1200m, depositadoOuSacadoDeduzido: 2500m);
        Assert.Equal(golden["COM_DEDUCAO_FGTS;creditoFgts"], creditoFgtsComDeducao);
    }

    [Fact]
    public void Liquido_e_o_credito_bruto_menos_os_descontos()
    {
        var credito = new CreditoDoReclamante
        {
            Verbas = 7700m,
            Fgts = 4200m,
            MultasReclamanteReclamado = 400m,
            MultasReclamadoReclamanteDescontar = 200m,
        };
        Assert.Equal(12100m, credito.Bruto);

        var descontos = new DescontosDoReclamante
        {
            ContribuicaoSocialSegurado = 550m,
            PrevidenciaPrivada = 200m,
            PensaoAlimenticia = 300m,
            MultasTerceiroReclamanteDescontar = 100m,
            HonorariosReclamanteDescontar = 400m,
            IrpfDoReclamante = 250m,
            CustasDoReclamante = 132.23m,
        };
        Assert.Equal(1932.23m, descontos.Total);

        var resultado = LiquidoDevidoAoReclamante.Calcular(credito, descontos);
        Assert.Equal(12100m, resultado.CreditoBruto);
        Assert.Equal(1932.23m, resultado.TotalDeDescontos);
        Assert.Equal(10167.77m, resultado.Liquido);
    }

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_liquido.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => $"{c[0]};{c[1]}", c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
