using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de cálculo conforme a parte responsável pela elaboração.
/// </summary>
public enum TipoCalculoEnum
{
    [Description("Advogado")]
    Advogado,

    [Description("Credor")]
    Credor,

    [Description("Devedor")]
    Devedor,

    [Description("Vara")]
    Vara,

    [Description("Gabinete")]
    Gabinete
}
