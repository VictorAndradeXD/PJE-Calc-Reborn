using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Modo de calculo a ser utilizado.
/// </summary>
public enum ModoDeCalculoEnum
{
    [Description("Geração de Ocorrência")]
    GeracaoDeOcorrencia,

    [Description("Liquidação")]
    Liquidacao
}
