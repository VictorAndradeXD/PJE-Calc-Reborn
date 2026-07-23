using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Forma de cobrança de um débito do reclamante.</summary>
public enum TipoCobrancaReclamanteEnum
{
    [Description("Descontar do crédito")]
    DescontarCredito,

    [Description("Cobrar")]
    Cobrar
}
