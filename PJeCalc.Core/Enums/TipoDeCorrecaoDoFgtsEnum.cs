using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Qual índice acumulado usar ao corrigir um valor de FGTS: o da data de demissão
/// (base da multa) ou o da data de liquidação (valor a pagar/depositar).
/// </summary>
public enum TipoDeCorrecaoDoFgtsEnum
{
    [Description("Pela data de demissão")]
    PelaDataDeDemissao,

    [Description("Pela data de liquidação")]
    PelaDataDeLiquidacao
}
