namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Taxa mensal de um índice de correção, tal como publicada (em pontos percentuais).
/// </summary>
/// <param name="Competencia">Primeiro dia do mês de competência.</param>
/// <param name="TaxaPercentual">Taxa do mês em pontos percentuais (ex.: 0,45 significa 0,45%).</param>
public sealed record IndiceMensal(DateOnly Competencia, decimal TaxaPercentual)
{
    /// <summary>
    /// Fator multiplicativo do mês: (taxa / 100 + 1). Ex.: 0,45% =&gt; 1,0045.
    /// </summary>
    public decimal Fator => 1m + TaxaPercentual / 100m;
}
