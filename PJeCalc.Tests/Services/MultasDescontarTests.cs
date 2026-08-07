using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Multas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida o split das multas que descontam do crédito do reclamante no
/// <see cref="TotalizadorDeMulta"/> (reclamado→reclamante e terceiro→reclamante marcadas para
/// descontar), como nas seções de crédito/débito do resumo do cálculo. O valor de cada multa já é
/// validado no golden de penalidades; aqui confere-se o agrupamento por credor/devedor e cobrança.
/// </summary>
public sealed class MultasDescontarTests
{
    [Fact]
    public void Separa_o_que_desconta_do_credito_por_credor_devedor_e_cobranca()
    {
        var totais = TotalizadorDeMulta.Calcular(
        [
            (CredorDevedorMultaEnum.ReclamanteReclamado, TipoCobrancaReclamanteEnum.DescontarCredito, 100.00m),
            // Reclamado→reclamante: uma desconta do crédito, outra é apenas cobrada (não desconta).
            (CredorDevedorMultaEnum.ReclamadoReclamante, TipoCobrancaReclamanteEnum.DescontarCredito, 80.00m),
            (CredorDevedorMultaEnum.ReclamadoReclamante, TipoCobrancaReclamanteEnum.Cobrar, 50.00m),
            // Terceiro→reclamante: só a que desconta entra no débito do reclamante.
            (CredorDevedorMultaEnum.TerceiroReclamante, TipoCobrancaReclamanteEnum.DescontarCredito, 30.00m),
            (CredorDevedorMultaEnum.TerceiroReclamante, TipoCobrancaReclamanteEnum.Cobrar, 20.00m),
            (CredorDevedorMultaEnum.TerceiroReclamado, TipoCobrancaReclamanteEnum.DescontarCredito, 40.00m),
        ]);

        // Baldes do bruto: reclamado→reclamante soma tudo (80 + 50); terceiro→reclamante não entra.
        Assert.Equal(100.00m, totais.ReclamanteReclamado);
        Assert.Equal(130.00m, totais.ReclamadoReclamante);
        Assert.Equal(40.00m, totais.TerceiroReclamado);

        // Descontos do crédito: só os marcados para descontar.
        Assert.Equal(80.00m, totais.ReclamadoReclamanteDescontar);
        Assert.Equal(30.00m, totais.TerceiroReclamanteDescontar);
    }
}
