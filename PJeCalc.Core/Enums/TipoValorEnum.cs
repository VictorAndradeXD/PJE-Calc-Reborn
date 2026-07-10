using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Indica se o valor foi informado manualmente ou calculado pelo sistema.
/// </summary>
public enum TipoValorEnum
{
    [Description("Informado")]
    Informado,

    [Description("Calculado")]
    Calculado
}
