using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de verba trabalhista (principal ou reflexa).
/// </summary>
public enum TipoDeVerbaEnum
{
    [Description("Principal")]
    Principal,

    [Description("Reflexa")]
    Reflexo
}
