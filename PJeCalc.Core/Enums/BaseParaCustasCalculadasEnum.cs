using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Base sobre a qual as custas calculadas incidem.</summary>
public enum BaseParaCustasCalculadasEnum
{
    [Description("Bruto devido ao reclamante")]
    BrutoDevidoAoReclamante,

    [Description("Bruto devido ao reclamante (+) débitos do reclamado")]
    BrutoDevidoAoReclamanteMaisDebitosReclamado
}
