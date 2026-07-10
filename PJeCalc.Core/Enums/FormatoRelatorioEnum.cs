using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Formato de saida do relatorio.
/// </summary>
public enum FormatoRelatorioEnum
{
    [Description("PDF")]
    PDF,

    [Description("HTML")]
    HTML
}
