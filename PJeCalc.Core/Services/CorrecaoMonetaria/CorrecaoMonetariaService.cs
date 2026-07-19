using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Corrige valores monetários pela variação acumulada de um índice entre a data
/// de vencimento e a data de liquidação.
///
/// <para>Escopo atual (walking skeleton): um único índice mensal, com acumulação
/// multiplicativa (produtório de fatores) ou aditiva (família SELIC), tratamento
/// de taxa negativa e conversão de moeda histórica.</para>
///
/// <para>Ainda NÃO cobre — a implementar nos próximos incrementos, calibrados por
/// valores de referência do sistema oficial: índices diários; pro-rata na troca
/// de índice no meio do mês (com dias úteis para a TR); combinação de índices; e o
/// calendário próprio de vencimento do JAM.</para>
/// </summary>
public sealed class CorrecaoMonetariaService
{
    private readonly IIndiceProvider _indices;

    public CorrecaoMonetariaService(IIndiceProvider indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        _indices = indices;
    }

    public ResultadoDaCorrecao Corrigir(PedidoDeCorrecao pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        if (pedido.DataLiquidacao < pedido.DataVencimento)
        {
            throw new ArgumentException(
                "A data de liquidação não pode ser anterior à data de vencimento.", nameof(pedido));
        }

        if (pedido.Indice == IndiceMonetarioEnum.SemCorrecao)
            return SemCorrecao(pedido);

        var competenciaInicial = AjustarCompetenciaInicial(pedido.DataVencimento, pedido.Regime);
        var competenciaFinal = PrimeiroDiaDoMes(pedido.DataLiquidacao);

        var serie = _indices.ObterSerieMensal(pedido.Indice)
            .Where(i => i.Competencia >= competenciaInicial && i.Competencia <= competenciaFinal)
            .OrderBy(i => i.Competencia)
            .ToList();

        var fator = EhSelic(pedido.Indice)
            ? AcumularAditivo(serie, pedido.IgnorarTaxaNegativa)
            : AcumularMultiplicativo(serie, pedido.IgnorarTaxaNegativa);

        return new ResultadoDaCorrecao
        {
            ValorOriginal = pedido.Valor,
            ValorCorrigido = Aplicar(pedido.Valor, fator),
            FatorAcumulado = fator,
            CompetenciaInicial = competenciaInicial,
            CompetenciaFinal = competenciaFinal,
            MesesConsiderados = serie.Count,
        };
    }

    /// <summary>
    /// Produtório dos fatores mensais (regra padrão: IPCA-E, TR, INPC, IGP-M...).
    /// A contribuição do mês que marca troca de moeda é dividida pelo divisor de
    /// conversão, por competência (igual ao motor oficial), o que também mantém o
    /// intermediário limitado em períodos de hiperinflação.
    /// </summary>
    private static decimal AcumularMultiplicativo(IEnumerable<IndiceMensal> serie, bool ignorarTaxaNegativa)
    {
        var fator = 1m;
        foreach (var mes in serie)
        {
            // Mês deflacionário, quando ignorado, contribui com fator 1.
            var contribuicao = ignorarTaxaNegativa && mes.TaxaPercentual < 0m ? 1m : mes.Fator;

            var divisor = ConversaoDeMoedas.DivisorNaCompetencia(mes.Competencia);
            if (divisor != 1m)
                contribuicao /= divisor;

            fator *= contribuicao;
        }
        return fator;
    }

    /// <summary>Soma linear das taxas mensais (família SELIC, que já embute juros + correção).</summary>
    private static decimal AcumularAditivo(IEnumerable<IndiceMensal> serie, bool ignorarTaxaNegativa)
    {
        var soma = 0m;
        foreach (var mes in serie)
        {
            if (ignorarTaxaNegativa && mes.TaxaPercentual < 0m)
                continue; // mês negativo não contribui para a soma
            soma += mes.TaxaPercentual / 100m;
        }
        return 1m + soma;
    }

    private static decimal Aplicar(decimal valor, decimal fator) =>
        AplicacaoDeFator.Aplicar(valor, fator);

    private static ResultadoDaCorrecao SemCorrecao(PedidoDeCorrecao pedido) => new()
    {
        ValorOriginal = pedido.Valor,
        ValorCorrigido = Math.Round(pedido.Valor, 2, MidpointRounding.ToEven),
        FatorAcumulado = 1m,
        CompetenciaInicial = PrimeiroDiaDoMes(pedido.DataVencimento),
        CompetenciaFinal = PrimeiroDiaDoMes(pedido.DataLiquidacao),
        MesesConsiderados = 0,
    };

    private static DateOnly AjustarCompetenciaInicial(DateOnly vencimento, IndicesAcumuladosEnum regime) => regime switch
    {
        IndicesAcumuladosEnum.MesDoVencimento => PrimeiroDiaDoMes(vencimento),
        IndicesAcumuladosEnum.MesSubsequenteAoVencimento => PrimeiroDiaDoMes(vencimento).AddMonths(1),
        // Skeleton: verbas mensais tratadas como subsequente. Anuais/rescisórias divergem
        // (usam o mês do vencimento) — a distinguir quando o tipo da verba entrar no cálculo.
        IndicesAcumuladosEnum.MesSubsequenteEMesDoVencimento => PrimeiroDiaDoMes(vencimento).AddMonths(1),
        // Skeleton: reatualização dia-a-dia ainda não modelada; usa o mês do vencimento.
        IndicesAcumuladosEnum.AtualizacaoCalculo => PrimeiroDiaDoMes(vencimento),
        _ => PrimeiroDiaDoMes(vencimento),
    };

    private static DateOnly PrimeiroDiaDoMes(DateOnly data) => new(data.Year, data.Month, 1);

    private static bool EhSelic(IndiceMonetarioEnum indice) => indice
        is IndiceMonetarioEnum.Selic
        or IndiceMonetarioEnum.SelicFazenda
        or IndiceMonetarioEnum.SelicBacen;
}
