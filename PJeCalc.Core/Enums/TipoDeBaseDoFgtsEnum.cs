using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de base do FGTS (calculada ou informada).
/// </summary>
public enum TipoDeBaseDoFgtsEnum
{
    [Description("Calculado")]
    Calculada,

    [Description("Informado")]
    Informada
}
