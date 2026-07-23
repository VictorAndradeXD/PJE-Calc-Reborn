using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Multas;

/// <summary>
/// Bases já liquidadas que uma multa CALCULADA consome. São entradas porque nascem de
/// outros módulos já validados (apuração de juros, FGTS, INSS, correção do valor da causa).
/// </summary>
public sealed record BasesDaMulta
{
    /// <summary>Total do principal corrigido (<c>totalDeValorCorrigidoDaApuracaoDeJuros</c>).</summary>
    public decimal PrincipalCorrigido { get; init; }

    /// <summary>Total dos juros de mora (<c>totalDeJurosDaApuracaoDeJuros</c>).</summary>
    public decimal JurosDeMora { get; init; }

    /// <summary>FGTS que compõe o principal (depósitos + multa + art. 467 − sacado), 0 se não compõe.</summary>
    public decimal ComponenteFgts { get; init; }

    /// <summary>Cota do segurado reclamante a descontar, <b>já arredondada a 2 casas</b> (0 se não incide).</summary>
    public decimal DescontoContribuicaoSocial { get; init; }

    /// <summary>Total do devido corrigido da previdência privada a descontar (0 se não incide).</summary>
    public decimal DescontoPrevidenciaPrivada { get; init; }

    /// <summary>Valor da causa já corrigido do ajuizamento à liquidação e arredondado a 2 casas.</summary>
    public decimal ValorDaCausaCorrigido { get; init; }
}

/// <summary>Configuração de uma multa/indenização.</summary>
public sealed record ParametrosDaMulta
{
    public TipoValorEnum TipoValor { get; init; } = TipoValorEnum.Calculado;
    public CredorDevedorMultaEnum CredorDevedor { get; init; } = CredorDevedorMultaEnum.ReclamanteReclamado;
    public BaseParaApuracaoDeMultaEnum Base { get; init; } = BaseParaApuracaoDeMultaEnum.Principal;

    /// <summary>Alíquota em pontos percentuais (modo calculado).</summary>
    public decimal Aliquota { get; init; }

    /// <summary>Valor bruto digitado (modo informado).</summary>
    public decimal ValorInformado { get; init; }

    /// <summary>Índice acumulado do vencimento à liquidação (modo informado). 1 = sem correção.</summary>
    public decimal IndiceDeCorrecao { get; init; } = 1m;

    /// <summary>Taxa de juros da multa em pontos percentuais; nulo = sem juros.</summary>
    public decimal? TaxaDeJuros { get; init; }

    /// <summary>Forma de cobrança quando o devedor é o reclamante.</summary>
    public TipoCobrancaReclamanteEnum Cobranca { get; init; } = TipoCobrancaReclamanteEnum.DescontarCredito;
}

/// <summary>Resultado da liquidação de uma multa.</summary>
public sealed record ResultadoDaMulta(decimal Base, decimal ValorCorrigido, decimal Juros, decimal ValorTotal);

/// <summary>
/// Liquidação de multas/indenizações (entidade genérica <c>Multa</c> do original).
///
/// <para>No modo <b>calculado</b>, a base nasce do principal (corrigido + juros + FGTS) menos os
/// descontos escolhidos, ou do valor da causa; o valor é <c>base × alíquota</c> e só vira centavos
/// em <see cref="ResultadoDaMulta.ValorCorrigido"/> — como no original, onde o índice é 1 e o
/// arredondamento acontece em <c>getValorCorrigido</c>. No modo <b>informado</b>, o valor digitado
/// é corrigido do vencimento à liquidação. Os juros incidem sobre o corrigido, sem novo
/// arredondamento a 2 casas.</para>
/// </summary>
public static class ApuracaoDeMulta
{
    public static ResultadoDaMulta Calcular(ParametrosDaMulta p, BasesDaMulta bases)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(bases);

        var informado = p.TipoValor == TipoValorEnum.Informado;
        var baseDaMulta = informado ? p.ValorInformado : CalcularBase(p, bases);
        var valorBruto = informado ? p.ValorInformado : baseDaMulta * (p.Aliquota / 100m);

        var indice = informado ? p.IndiceDeCorrecao : 1m;
        var valorCorrigido = AplicacaoDeFator.Aplicar(valorBruto, indice);

        var juros = p.TaxaDeJuros is { } taxa ? valorCorrigido * (taxa / 100m) : 0m;
        return new ResultadoDaMulta(baseDaMulta, valorCorrigido, juros, valorCorrigido + juros);
    }

    /// <summary>Base do modo calculado. Os casos "principal" caem em cascata como no original.</summary>
    private static decimal CalcularBase(ParametrosDaMulta p, BasesDaMulta bases)
    {
        if (p.Base == BaseParaApuracaoDeMultaEnum.ValorCausa)
            return bases.ValorDaCausaCorrigido;

        var principal = bases.PrincipalCorrigido + bases.JurosDeMora + bases.ComponenteFgts;

        var descontoContribuicao = p.Base is BaseParaApuracaoDeMultaEnum.PrincipalMenosContribuicaoSocial
            or BaseParaApuracaoDeMultaEnum.PrincipalMenosContribuicaoSocialMenosPrevidenciaPrivada
            ? bases.DescontoContribuicaoSocial
            : 0m;

        var descontoPrevidencia = p.Base == BaseParaApuracaoDeMultaEnum.PrincipalMenosContribuicaoSocialMenosPrevidenciaPrivada
            ? bases.DescontoPrevidenciaPrivada
            : 0m;

        return principal - descontoContribuicao - descontoPrevidencia;
    }
}
