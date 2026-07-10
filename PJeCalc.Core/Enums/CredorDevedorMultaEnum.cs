using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Credor e devedor para multas e indenizacoes.
/// </summary>
public enum CredorDevedorMultaEnum
{
    [Description("Reclamante e Reclamado")]
    ReclamanteReclamado,

    [Description("Reclamado e Reclamante")]
    ReclamadoReclamante,

    [Description("Terceiro e Reclamante")]
    TerceiroReclamante,

    [Description("Terceiro e Reclamado")]
    TerceiroReclamado
}
