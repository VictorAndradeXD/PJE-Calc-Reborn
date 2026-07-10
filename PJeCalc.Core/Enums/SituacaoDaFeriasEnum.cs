using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Situação das férias do trabalhador no período aquisitivo.
/// </summary>
public enum SituacaoDaFeriasEnum
{
    [Description("Gozadas")]
    Gozadas,

    [Description("Gozadas Parcialmente")]
    GozadasParcialmente,

    [Description("Não Gozadas")]
    NaoGozadas,

    [Description("Indenizadas")]
    Indenizadas,

    [Description("Perdidas")]
    Perdidas
}
