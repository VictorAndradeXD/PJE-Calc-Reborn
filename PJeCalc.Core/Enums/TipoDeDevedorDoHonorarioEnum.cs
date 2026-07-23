using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Quem deve o honorário: reclamante ou reclamado.</summary>
public enum TipoDeDevedorDoHonorarioEnum
{
    [Description("Reclamante")]
    Reclamante,

    [Description("Reclamado")]
    Reclamado
}
