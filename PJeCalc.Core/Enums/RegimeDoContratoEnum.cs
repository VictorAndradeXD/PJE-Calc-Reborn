using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Regime do contrato de trabalho.
/// </summary>
public enum RegimeDoContratoEnum
{
    [Description("Trabalho Intermitente")]
    Intermitente,

    [Description("Tempo Integral")]
    Integral,

    [Description("Tempo Parcial")]
    Parcial
}
