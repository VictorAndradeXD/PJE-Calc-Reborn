using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Como as custas de conhecimento (2%) são apuradas.</summary>
public enum TipoDeCustasDeConhecimentoEnum
{
    [Description("Não se aplica")]
    NaoSeAplica,

    [Description("Calculada (2%)")]
    Calculada2PorCento,

    [Description("Informada")]
    Informada
}
