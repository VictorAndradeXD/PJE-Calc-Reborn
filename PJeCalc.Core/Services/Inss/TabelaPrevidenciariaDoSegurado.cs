namespace PJeCalc.Core.Services.Inss;

/// <summary>
/// Tabela previdenciária do segurado empregado de uma competência.
///
/// <para>Resolve a <b>alíquota</b> aplicável a uma base — nunca o valor da contribuição.
/// Há duas eras, separadas pela Reforma da Previdência (01/03/2020):</para>
/// <list type="bullet">
/// <item>até 02/2020: alíquota <b>única</b> da faixa em que a base cai, aplicada sobre o
/// total (não é progressivo);</item>
/// <item>a partir de 03/2020: soma-se a contribuição faixa a faixa (progressivo) e o
/// resultado é convertido de volta para uma <b>alíquota efetiva</b>, que só então
/// multiplica a base. Essa ida-e-volta é a do motor oficial e precisa ser preservada.</item>
/// </list>
/// </summary>
public sealed record TabelaPrevidenciariaDoSegurado
{
    /// <summary>Marco da Reforma da Previdência.</summary>
    public static readonly DateOnly DataDaReformaDaPrevidencia = new(2020, 3, 1);

    public required DateOnly Competencia { get; init; }

    public required FaixaPrevidenciaria Faixa1 { get; init; }
    public FaixaPrevidenciaria? Faixa2 { get; init; }
    public FaixaPrevidenciaria? Faixa3 { get; init; }
    public FaixaPrevidenciaria? Faixa4 { get; init; }
    public FaixaPrevidenciaria? Faixa5 { get; init; }

    /// <summary>Teto do <b>valor</b> da contribuição.</summary>
    public decimal TetoMaximo { get; init; }

    /// <summary>Teto do <b>salário-de-contribuição</b> (usado só a partir da reforma).</summary>
    public decimal TetoBeneficio { get; init; }

    /// <summary>Alíquota (em pontos percentuais) aplicável à base informada.</summary>
    public decimal ObterAliquotaParaValor(decimal valor) =>
        Competencia < DataDaReformaDaPrevidencia
            ? AliquotaUnicaDaFaixa(valor)
            : AliquotaEfetiva(valor);

    /// <summary>Até 02/2020: a alíquota da primeira faixa que comporta o valor.</summary>
    private decimal AliquotaUnicaDaFaixa(decimal valor)
    {
        var faixas = FaixasDefinidas();
        foreach (var faixa in faixas)
        {
            if (faixa.ValorFinal is null || valor <= faixa.ValorFinal)
                return faixa.Aliquota;
        }
        return faixas[^1].Aliquota;
    }

    /// <summary>A partir de 03/2020: contribuição progressiva reduzida a alíquota efetiva.</summary>
    private decimal AliquotaEfetiva(decimal valor)
    {
        if (Faixa1.ValorFinal is not null && valor <= Faixa1.ValorFinal)
            return Faixa1.Aliquota;

        // No teto, a alíquota efetiva é a que satura a contribuição máxima.
        if (TetoBeneficio > 0m && valor >= TetoBeneficio)
            return TetoMaximo / (TetoBeneficio / 100m);

        var contribuicao = ContribuicaoProgressiva(valor);
        return valor == 0m ? 0m : contribuicao / (valor / 100m);
    }

    /// <summary>
    /// Soma a contribuição da faixa em que o valor para com o máximo de todas as inferiores.
    /// </summary>
    private decimal ContribuicaoProgressiva(decimal valor)
    {
        var faixas = FaixasDefinidas();

        for (var i = faixas.Count - 1; i >= 1; i--)
        {
            if (valor < faixas[i].ValorInicial)
                continue;

            var contribuicao = faixas[i].CalcularValorNaFaixa(valor);
            for (var j = i - 1; j >= 0; j--)
                contribuicao += faixas[j].ValorMaximoDaFaixa ?? 0m;
            return contribuicao;
        }

        return 0m;
    }

    private List<FaixaPrevidenciaria> FaixasDefinidas()
    {
        var faixas = new List<FaixaPrevidenciaria>(5) { Faixa1 };
        if (Faixa2 is not null) faixas.Add(Faixa2);
        if (Faixa3 is not null) faixas.Add(Faixa3);
        if (Faixa4 is not null) faixas.Add(Faixa4);
        if (Faixa5 is not null) faixas.Add(Faixa5);
        return faixas;
    }
}
