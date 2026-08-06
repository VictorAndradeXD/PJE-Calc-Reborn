namespace PJeCalc.Core.Services.SalarioFamilia;

/// <summary>
/// Tabela do salário-família de uma competência: duas faixas de remuneração, cada uma com o
/// valor da cota por filho. A cota é a da primeira faixa cujo teto comporta a remuneração;
/// acima da segunda faixa não há benefício (cota nula).
/// </summary>
public sealed record TabelaSalarioFamilia
{
    public required DateOnly Competencia { get; init; }
    public decimal? FinalFaixa1 { get; init; }
    public decimal CotaFaixa1 { get; init; }
    public decimal? FinalFaixa2 { get; init; }
    public decimal CotaFaixa2 { get; init; }

    /// <summary>Cota por filho para a remuneração; nula quando acima do teto do benefício.</summary>
    public decimal? ObterCota(decimal remuneracaoMensal)
    {
        if (FinalFaixa1 is { } fim1 && remuneracaoMensal <= fim1)
            return CotaFaixa1;
        if (FinalFaixa2 is { } fim2 && remuneracaoMensal <= fim2)
            return CotaFaixa2;
        return null;
    }
}
