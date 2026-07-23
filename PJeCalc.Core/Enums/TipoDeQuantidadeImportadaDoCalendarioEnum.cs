using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Métrica do calendário usada quando a quantidade da verba é importada do calendário.
/// </summary>
public enum TipoDeQuantidadeImportadaDoCalendarioEnum
{
    [Description("Repousos")]
    Repousos,

    [Description("Dias Úteis")]
    DiasUteis,

    [Description("Feriados")]
    Feriados,

    [Description("Repousos e Feriados")]
    RepousosFeriados
}
