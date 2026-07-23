using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Qual valor das ocorrências de uma verba alimenta outra apuração
/// (principal ou reflexo): o devido bruto ou a diferença (devido − pago).
/// </summary>
public enum TipoDeGeracaoEnum
{
    [Description("Devido")]
    Devido,

    [Description("Diferença")]
    Diferenca
}
