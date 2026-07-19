namespace PJeCalc.Core.Services.Inss;

/// <summary>
/// Faixa da tabela previdenciária do segurado. Os limites vêm do banco já com o início
/// deslocado, por isso a largura da faixa desconta um centavo do valor inicial.
/// </summary>
/// <param name="ValorInicial">Início da faixa.</param>
/// <param name="ValorFinal">Fim da faixa; nulo indica faixa aberta (última).</param>
/// <param name="Aliquota">Alíquota da faixa, em pontos percentuais.</param>
public sealed record FaixaPrevidenciaria(decimal ValorInicial, decimal? ValorFinal, decimal Aliquota)
{
    private const decimal UmCentavo = 0.01m;

    /// <summary>
    /// Contribuição máxima gerada pela faixa (largura inteira × alíquota).
    /// Nula quando a faixa é aberta.
    /// </summary>
    public decimal? ValorMaximoDaFaixa =>
        ValorFinal is null
            ? null
            : (ValorFinal.Value - (ValorInicial - UmCentavo)) * (Aliquota / 100m);

    /// <summary>
    /// Contribuição gerada pela faixa para uma base que para dentro dela.
    /// Zero se a base não alcança o início da faixa.
    /// </summary>
    public decimal CalcularValorNaFaixa(decimal valorBase) =>
        valorBase < ValorInicial
            ? 0m
            : (valorBase - (ValorInicial - UmCentavo)) * (Aliquota / 100m);
}
