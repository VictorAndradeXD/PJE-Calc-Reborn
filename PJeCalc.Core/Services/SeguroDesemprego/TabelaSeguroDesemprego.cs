namespace PJeCalc.Core.Services.SeguroDesemprego;

/// <summary>
/// Tabela do seguro-desemprego de uma competência: o valor da parcela é a remuneração média
/// vezes o percentual da primeira faixa; acima do teto da faixa, o excedente entra pela segunda
/// faixa somado à parcela fixa. O resultado é limitado ao piso e ao teto do benefício.
/// </summary>
public sealed record TabelaSeguroDesemprego
{
    public required DateOnly Competencia { get; init; }
    public decimal? FinalFaixa1 { get; init; }
    public decimal PercentualFaixa1 { get; init; }
    public decimal? PercentualFaixa2 { get; init; }
    public decimal? SomaFaixa2 { get; init; }
    public decimal Piso { get; init; }
    public decimal Teto { get; init; }

    private static readonly decimal LimiteAusente = 9999999999999.00m;

    /// <summary>Valor da parcela do seguro-desemprego para a remuneração média (piso e teto aplicados).</summary>
    public decimal ValorDaParcela(decimal remuneracaoMensal)
    {
        var limiteFaixa1 = FinalFaixa1 ?? LimiteAusente;

        var valor = remuneracaoMensal <= limiteFaixa1
            ? remuneracaoMensal * (PercentualFaixa1 / 100m)
            : (remuneracaoMensal - limiteFaixa1) * ((PercentualFaixa2 ?? 0m) / 100m) + (SomaFaixa2 ?? 0m);

        if (valor < Piso)
            valor = Piso;
        if (valor > Teto)
            valor = Teto;
        return valor;
    }
}
