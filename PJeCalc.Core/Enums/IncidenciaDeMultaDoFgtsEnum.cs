using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Sobre o que a multa do FGTS incide.
/// </summary>
public enum IncidenciaDeMultaDoFgtsEnum
{
    [Description("Sobre o total devido")]
    SobreOTotalDevido,

    [Description("Sobre o depositado/sacado")]
    SobreDepositadoSacado,

    [Description("Sobre a diferença")]
    SobreDiferenca,

    [Description("Sobre o total devido mais saque e/ou saldo")]
    SobreTotalDevidoMaisSaqueOuSaldo,

    [Description("Sobre o total devido menos saque e/ou saldo")]
    SobreTotalDevidoMenosSaqueOuSaldo
}
