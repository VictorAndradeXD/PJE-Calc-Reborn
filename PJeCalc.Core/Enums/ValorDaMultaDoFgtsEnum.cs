using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Percentual da multa do FGTS.
/// </summary>
public enum ValorDaMultaDoFgtsEnum
{
    [Description("20%")]
    VintePorCento,

    [Description("40%")]
    QuarentaPorCento
}
