using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Inss;

/// <summary>Resultado de uma cota do INSS (segurado, empresa, SAT ou terceiros).</summary>
public sealed record CotaDeInss(decimal Corrigido, decimal Juros, decimal Multa, decimal Total);

/// <summary>Dados de uma ocorrência mensal do INSS, comuns às quatro cotas.</summary>
public sealed record OcorrenciaDeInssEntrada
{
    public decimal ValorDevidoSegurado { get; init; }
    public decimal ValorDevidoEmpresa { get; init; }
    public decimal ValorDevidoSAT { get; init; }
    public decimal ValorDevidoTerceiros { get; init; }

    /// <summary>Fator de correção trabalhista aplicado (1 quando não há).</summary>
    public required decimal IndiceTrabalhista { get; init; }

    /// <summary>Fator de correção previdenciária aplicado (1 quando não há).</summary>
    public required decimal IndicePrevidenciaria { get; init; }

    public decimal TaxaDeJuros { get; init; }
    public decimal TaxaDeMulta { get; init; }

    /// <summary>
    /// Quando verdadeiro, os juros são truncados a 2 casas (para baixo) — a regra do
    /// original para juros e multa previdenciários (a multa, porém, não é truncada).
    /// </summary>
    public bool JurosEMultaPrevidenciario { get; init; }
}

/// <summary>
/// Aplicação por ocorrência do INSS: correção, juros e multa de cada cota.
///
/// <para>O índice de correção é o produto do trabalhista pelo previdenciário. Os juros
/// incidem sobre o valor corrigido e são <b>truncados</b> a 2 casas (para baixo) quando
/// previdenciários; a multa incide sobre o corrigido e não é truncada.</para>
/// </summary>
public static class ApuracaoInssPorOcorrencia
{
    /// <summary>Calcula as quatro cotas da ocorrência.</summary>
    public static (CotaDeInss Segurado, CotaDeInss Empresa, CotaDeInss SAT, CotaDeInss Terceiros)
        Calcular(OcorrenciaDeInssEntrada e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return (
            CalcularCota(e.ValorDevidoSegurado, e),
            CalcularCota(e.ValorDevidoEmpresa, e),
            CalcularCota(e.ValorDevidoSAT, e),
            CalcularCota(e.ValorDevidoTerceiros, e));
    }

    /// <summary>Calcula uma cota a partir do seu valor devido.</summary>
    public static CotaDeInss CalcularCota(decimal valorDevido, OcorrenciaDeInssEntrada e)
    {
        var indice = e.IndiceTrabalhista * e.IndicePrevidenciaria;
        var corrigido = AplicacaoDeFator.Aplicar(valorDevido, indice);

        var juros = corrigido * e.TaxaDeJuros / 100m;
        if (e.JurosEMultaPrevidenciario)
            juros = TruncarDuasCasas(juros);

        var multa = corrigido * e.TaxaDeMulta / 100m;

        return new CotaDeInss(corrigido, juros, multa, corrigido + juros + multa);
    }

    private static decimal TruncarDuasCasas(decimal valor) => Math.Truncate(valor * 100m) / 100m;
}
