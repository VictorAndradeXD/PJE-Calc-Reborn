namespace PJeCalc.Core.Services.Irpf;

/// <summary>Faixa da tabela progressiva do IRPF.</summary>
/// <param name="ValorInicial">Início da faixa.</param>
/// <param name="ValorFinal">Fim da faixa; nulo indica o topo (última faixa).</param>
/// <param name="Aliquota">Alíquota em pontos percentuais.</param>
/// <param name="Deducao">Parcela a deduzir do imposto.</param>
public sealed record FaixaFiscal(decimal ValorInicial, decimal? ValorFinal, decimal Aliquota, decimal Deducao);

/// <summary>
/// Tabela progressiva do IRPF de uma competência: cinco faixas e as deduções por
/// dependente e para aposentado maior de 65 anos.
/// </summary>
public sealed record TabelaIrpf
{
    public required DateOnly Competencia { get; init; }
    public required FaixaFiscal Faixa1 { get; init; }
    public FaixaFiscal? Faixa2 { get; init; }
    public FaixaFiscal? Faixa3 { get; init; }
    public FaixaFiscal? Faixa4 { get; init; }
    public FaixaFiscal? Faixa5 { get; init; }
    public decimal DeducaoPorDependente { get; init; }
    public decimal DeducaoAposentadoMaior65 { get; init; }

    /// <summary>
    /// Faixa aplicável a uma base. No regime de rendimentos acumulados (RRA) os limites
    /// são multiplicados pelo número de meses; passe <paramref name="numeroDeMeses"/> = 1
    /// para o regime normal.
    /// </summary>
    public FaixaFiscal ObterFaixaParaValor(decimal valor, int numeroDeMeses = 1)
    {
        var faixas = Faixas();
        foreach (var faixa in faixas)
        {
            if (faixa.ValorFinal is null || valor <= faixa.ValorFinal.Value * numeroDeMeses)
                return faixa;
        }
        return faixas[^1];
    }

    private List<FaixaFiscal> Faixas()
    {
        var faixas = new List<FaixaFiscal>(5) { Faixa1 };
        if (Faixa2 is not null) faixas.Add(Faixa2);
        if (Faixa3 is not null) faixas.Add(Faixa3);
        if (Faixa4 is not null) faixas.Add(Faixa4);
        if (Faixa5 is not null) faixas.Add(Faixa5);
        return faixas;
    }
}
