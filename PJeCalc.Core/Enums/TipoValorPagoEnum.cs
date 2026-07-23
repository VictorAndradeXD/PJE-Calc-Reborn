using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Como o valor pago de uma verba é obtido: informado diretamente ou calculado
/// a partir de uma base tabelada.
/// </summary>
public enum TipoValorPagoEnum
{
    [Description("Informado")]
    Informado,

    [Description("Calculado")]
    Calculado
}
