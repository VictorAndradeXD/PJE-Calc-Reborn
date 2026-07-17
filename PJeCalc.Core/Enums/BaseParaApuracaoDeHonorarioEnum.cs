using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Base para apuracao de honorarios.
/// </summary>
public enum BaseParaApuracaoDeHonorarioEnum
{
    [Description("Bruto")]
    Bruto,

    [Description("Bruto (-) Contribuição Social")]
    BrutoMenosContribuicaoSocial,

    [Description("Verbas que não compõem o Principal")]
    VerbasQueNaoCompoeOPrincipal
}
