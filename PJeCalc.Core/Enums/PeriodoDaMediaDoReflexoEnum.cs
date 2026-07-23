using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Janela de competências da verba de origem considerada nas médias de reflexo.
/// </summary>
public enum PeriodoDaMediaDoReflexoEnum
{
    [Description("Período Aquisitivo")]
    PeriodoAquisitivo,

    [Description("Ano Civil")]
    AnoCivil,

    [Description("Últimos 12 Meses do Contrato")]
    UltimosDozeMesesDoContrato,

    [Description("12 Meses Anteriores ao Vencimento da Parcela")]
    DozeMesesAnterioresAoVencimentoDaParcela
}
