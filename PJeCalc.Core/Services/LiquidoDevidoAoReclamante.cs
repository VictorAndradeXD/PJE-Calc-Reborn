namespace PJeCalc.Core.Services;

/// <summary>
/// Crédito bruto do reclamante: as verbas (principal corrigido + juros + salário-família +
/// seguro-desemprego + multa do art. 467), o FGTS (depósitos + multa − saldo/saque deduzido) e as
/// multas a favor do reclamante (reclamante→reclamado somam; reclamado→reclamante que descontam do
/// crédito subtraem).
/// </summary>
public sealed record CreditoDoReclamante
{
    public decimal Verbas { get; init; }
    public decimal Fgts { get; init; }
    public decimal MultasReclamanteReclamado { get; init; }
    public decimal MultasReclamadoReclamanteDescontar { get; init; }

    public decimal Bruto =>
        Verbas + Fgts + MultasReclamanteReclamado - MultasReclamadoReclamanteDescontar;
}

/// <summary>
/// Débitos do reclamante, descontados do seu crédito para formar o líquido (todos como valores
/// positivos; a subtração é feita no cálculo do líquido).
/// </summary>
public sealed record DescontosDoReclamante
{
    /// <summary>Contribuição social (INSS) do segurado reclamante, quando cobrada dele.</summary>
    public decimal ContribuicaoSocialSegurado { get; init; }
    public decimal PrevidenciaPrivada { get; init; }
    public decimal PensaoAlimenticia { get; init; }

    /// <summary>Multas de terceiro contra o reclamante que descontam do crédito.</summary>
    public decimal MultasTerceiroReclamanteDescontar { get; init; }

    /// <summary>Honorários devidos pelo reclamante que descontam do crédito.</summary>
    public decimal HonorariosReclamanteDescontar { get; init; }

    public decimal IrpfDoReclamante { get; init; }
    public decimal CustasDoReclamante { get; init; }

    /// <summary>Depósito do FGTS na conta vinculada (quando o destino é depositar, não pagar).</summary>
    public decimal DepositoFgts { get; init; }

    public decimal Total =>
        ContribuicaoSocialSegurado + PrevidenciaPrivada + PensaoAlimenticia
        + MultasTerceiroReclamanteDescontar + HonorariosReclamanteDescontar
        + IrpfDoReclamante + CustasDoReclamante + DepositoFgts;
}

/// <summary>Resultado do líquido devido ao reclamante.</summary>
public sealed record ResultadoDoLiquido(decimal CreditoBruto, decimal TotalDeDescontos, decimal Liquido);

/// <summary>
/// Líquido devido ao reclamante: crédito bruto menos os descontos, como o resumo do cálculo do
/// original (seções de crédito e débito do reclamante).
/// </summary>
public static class LiquidoDevidoAoReclamante
{
    /// <summary>
    /// Crédito das verbas (<c>calculaValorVerbaParaCreditoDoReclamante</c>): principal corrigido +
    /// juros + salário-família + seguro-desemprego + multa do art. 467 (os acessórios só entram se
    /// apurados e compondo o principal — passe 0 caso contrário).
    /// </summary>
    public static decimal CreditoDeVerbas(
        decimal principalCorrigido,
        decimal jurosDeMora,
        decimal salarioFamilia = 0m,
        decimal seguroDesemprego = 0m,
        decimal multaDoArtigo467 = 0m) =>
        principalCorrigido + jurosDeMora + salarioFamilia + seguroDesemprego + multaDoArtigo467;

    /// <summary>
    /// Crédito do FGTS (<c>calculaValorFgtsParaCreditoDoReclamante</c>): FGTS corrigido + multa −
    /// saldo/saque deduzido (quando "deduzir do FGTS").
    /// </summary>
    public static decimal CreditoDeFgts(
        decimal fgtsCorrigido,
        decimal multaDoFgts = 0m,
        decimal depositadoOuSacadoDeduzido = 0m) =>
        fgtsCorrigido + multaDoFgts - depositadoOuSacadoDeduzido;

    public static ResultadoDoLiquido Calcular(CreditoDoReclamante credito, DescontosDoReclamante descontos)
    {
        ArgumentNullException.ThrowIfNull(credito);
        ArgumentNullException.ThrowIfNull(descontos);

        var bruto = credito.Bruto;
        var totalDescontos = descontos.Total;
        return new ResultadoDoLiquido(bruto, totalDescontos, bruto - totalDescontos);
    }
}
