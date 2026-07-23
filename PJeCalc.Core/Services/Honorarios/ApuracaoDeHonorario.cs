using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;
using PJeCalc.Core.Services.Irpf;

namespace PJeCalc.Core.Services.Honorarios;

/// <summary>Bases já liquidadas que um honorário CALCULADO consome.</summary>
public sealed record BasesDoHonorario
{
    /// <summary>Bruto devido ao reclamante (principal corrigido + juros + multas + FGTS).</summary>
    public decimal BrutoDevidoAoReclamante { get; init; }

    /// <summary>Soma das verbas selecionadas que não compõem o principal (corrigidas).</summary>
    public decimal SomaVerbasNaoPrincipal { get; init; }

    /// <summary>Cota do segurado reclamante a descontar, já arredondada a 2 casas.</summary>
    public decimal DescontoContribuicaoSocial { get; init; }

    /// <summary>Total do devido corrigido da previdência privada a descontar.</summary>
    public decimal DescontoPrevidenciaPrivada { get; init; }
}

/// <summary>Configuração de um honorário.</summary>
public sealed record ParametrosDoHonorario
{
    public TipoValorEnum TipoValor { get; init; } = TipoValorEnum.Calculado;
    public BaseParaApuracaoDeHonorarioEnum Base { get; init; } = BaseParaApuracaoDeHonorarioEnum.Bruto;
    public TipoDeDevedorDoHonorarioEnum Devedor { get; init; } = TipoDeDevedorDoHonorarioEnum.Reclamado;
    public TipoCobrancaReclamanteEnum Cobranca { get; init; } = TipoCobrancaReclamanteEnum.DescontarCredito;

    public decimal Aliquota { get; init; }
    public decimal ValorInformado { get; init; }
    public decimal IndiceDeCorrecao { get; init; } = 1m;
    public decimal? TaxaDeJuros { get; init; }

    public bool ApurarIRRF { get; init; }
    public TipoDeImpostoDeRendaEnum TipoImposto { get; init; } = TipoDeImpostoDeRendaEnum.PessoaFisica;
    public bool ApurarIRPFSobreJuros { get; init; }

    /// <summary>Tabela do IRPF da competência da liquidação (obrigatória para pessoa física).</summary>
    public TabelaIrpf? TabelaIrpf { get; init; }
}

/// <summary>Resultado da liquidação de um honorário.</summary>
public sealed record ResultadoDoHonorario(
    decimal Base,
    decimal ValorCorrigido,
    decimal? Juros,
    decimal ValorTotal,
    decimal Imposto);

/// <summary>
/// Liquidação de honorários (advocatícios, sucumbenciais, periciais, assistenciais — uma só
/// máquina no original; o tipo é apenas rótulo). Modo calculado incide a alíquota sobre o bruto
/// (menos contribuição social / previdência privada) ou sobre verbas que não compõem o principal;
/// modo informado corrige o valor digitado. O IRRF é retido a 1,50% (pessoa jurídica) ou pela
/// tabela progressiva (pessoa física); como no original, o imposto na liquidação <b>não</b> é
/// arredondado nem tem piso zero.
/// </summary>
public static class ApuracaoDeHonorario
{
    private const decimal AliquotaImpostoPessoaJuridica = 1.50m;

    public static ResultadoDoHonorario Calcular(ParametrosDoHonorario p, BasesDoHonorario bases)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(bases);

        var informado = p.TipoValor == TipoValorEnum.Informado;
        var baseHonorario = informado ? p.ValorInformado : CalcularBase(p, bases);
        var valorBruto = informado ? p.ValorInformado : baseHonorario * (p.Aliquota / 100m);

        var indice = informado ? p.IndiceDeCorrecao : 1m;
        var valorCorrigido = AplicacaoDeFator.Aplicar(valorBruto, indice);

        decimal? juros = p.TaxaDeJuros is { } taxa ? valorCorrigido * (taxa / 100m) : null;
        var valorTotal = valorCorrigido + (juros ?? 0m);

        var imposto = CalcularImposto(p, valorCorrigido, valorTotal);
        return new ResultadoDoHonorario(baseHonorario, valorCorrigido, juros, valorTotal, imposto);
    }

    private static decimal CalcularBase(ParametrosDoHonorario p, BasesDoHonorario bases)
    {
        var bruto = p.Base == BaseParaApuracaoDeHonorarioEnum.VerbasQueNaoCompoeOPrincipal
            ? bases.SomaVerbasNaoPrincipal
            : bases.BrutoDevidoAoReclamante;

        var descontoContribuicao = p.Base is BaseParaApuracaoDeHonorarioEnum.BrutoMenosContribuicaoSocial
            or BaseParaApuracaoDeHonorarioEnum.BrutoMenosContribuicaoSocialMenosPrevidenciaPrivada
            ? bases.DescontoContribuicaoSocial
            : 0m;

        var descontoPrevidencia = p.Base == BaseParaApuracaoDeHonorarioEnum.BrutoMenosContribuicaoSocialMenosPrevidenciaPrivada
            ? bases.DescontoPrevidenciaPrivada
            : 0m;

        return bruto - descontoContribuicao - descontoPrevidencia;
    }

    private static decimal CalcularImposto(ParametrosDoHonorario p, decimal valorCorrigido, decimal valorTotal)
    {
        if (!p.ApurarIRRF)
            return 0m;

        var baseImposto = p.ApurarIRPFSobreJuros ? valorTotal : valorCorrigido;

        if (p.TipoImposto == TipoDeImpostoDeRendaEnum.PessoaJuridica)
            return baseImposto * (AliquotaImpostoPessoaJuridica / 100m);

        var tabela = p.TabelaIrpf
            ?? throw new InvalidOperationException("IRRF de pessoa física exige a tabela do IRPF da competência.");
        var faixa = tabela.ObterFaixaParaValor(baseImposto);
        return baseImposto * (faixa.Aliquota / 100m) - faixa.Deducao;
    }
}
