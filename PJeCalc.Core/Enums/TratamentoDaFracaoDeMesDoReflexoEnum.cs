using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// O que fazer com competências parcialmente cobertas pela verba de origem ao
/// calcular a base de um reflexo.
/// </summary>
public enum TratamentoDaFracaoDeMesDoReflexoEnum
{
    [Description("Manter")]
    Manter,

    [Description("Integralizar")]
    Integralizar,

    [Description("Desprezar")]
    Desprezar,

    [Description("Desprezar Menor que 15 Dias")]
    DesprezarMenorQue15Dias
}
