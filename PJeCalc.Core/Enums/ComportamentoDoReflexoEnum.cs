using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Como a base de uma verba-reflexo é derivada das ocorrências da verba de origem.
/// </summary>
public enum ComportamentoDoReflexoEnum
{
    [Description("Valor Mensal")]
    ValorMensal,

    [Description("Média Pela Quantidade")]
    MediaPelaQuantidade,

    [Description("Média Pelo Valor Absoluto")]
    MediaPeloValor,

    [Description("Média Pelo Valor Corrigido")]
    MediaPeloValorCorrigido
}
