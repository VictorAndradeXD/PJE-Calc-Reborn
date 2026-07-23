using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Como as custas de liquidação (0,5%) são apuradas.</summary>
public enum TipoDeCustasDeLiquidacaoEnum
{
    [Description("Não se aplica")]
    NaoSeAplica,

    [Description("Calculada (0,5%)")]
    CalculadaMeioPorCento,

    [Description("Informada")]
    Informada
}
