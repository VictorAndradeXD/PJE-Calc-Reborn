using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Base para apuracao de multas.
/// </summary>
public enum BaseParaApuracaoDeMultaEnum
{
    [Description("Principal")]
    Principal,

    [Description("Principal (-) Contribuição Social")]
    PrincipalMenosContribuicaoSocial,

    [Description("Principal (-) Contribuição Social (-) Previdência Privada")]
    PrincipalMenosContribuicaoSocialMenosPrevidenciaPrivada,

    [Description("Valor Corrigido da Causa")]
    ValorCausa
}
