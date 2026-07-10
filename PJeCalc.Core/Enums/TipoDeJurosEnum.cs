using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de capitalização dos juros: simples ou compostos.
/// </summary>
public enum TipoDeJurosEnum
{
    [Description("Simples")]
    Simples,

    [Description("Compostos")]
    Compostos
}
