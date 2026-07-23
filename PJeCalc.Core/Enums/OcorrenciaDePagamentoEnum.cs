using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Quando as ocorrências (competências) de uma verba são geradas.
/// </summary>
public enum OcorrenciaDePagamentoEnum
{
    [Description("Desligamento")]
    Desligamento,

    [Description("Dezembro")]
    Dezembro,

    [Description("Mensal")]
    Mensal,

    [Description("Período Aquisitivo")]
    PeriodoAquisitivo
}
