using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Característica da verba trabalhista para fins de incidência de reflexos.
/// </summary>
public enum CaracteristicaDaVerbaEnum
{
    [Description("Comum")]
    Comum,

    [Description("13º Salário")]
    DecimoTerceiroSalario,

    [Description("Aviso Prévio")]
    AvisoPrevio,

    [Description("Férias")]
    Ferias
}
