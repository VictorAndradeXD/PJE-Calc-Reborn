using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Um balde de juros de mora: as ocorrências de uma mesma competência mensal que começam a
/// render juros na mesma data são somadas num capital único e rendem à taxa acumulada dessa
/// data até a liquidação.
/// </summary>
public sealed class ApuracaoDeJuros
{
    /// <summary>Competência (1º dia do mês) das ocorrências agrupadas.</summary>
    public required DateOnly Competencia { get; init; }

    /// <summary>Data em que os juros passam a incidir (pivô; a contagem começa no dia seguinte).</summary>
    public required DateOnly DataInicial { get; init; }

    /// <summary>Taxa acumulada de juros, em pontos percentuais.</summary>
    public required decimal TaxaDeJuros { get; init; }

    /// <summary>Capital corrigido acumulado (soma das diferenças corrigidas, cada uma a 2 casas).</summary>
    public decimal ValorCorrigido { get; internal set; }

    /// <summary>Base sobre a qual os juros incidem (no caso padrão = valor corrigido).</summary>
    public decimal Capital => ValorCorrigido;

    /// <summary>Juros = capital × taxa/100, sem arredondamento (arredonda só na totalização).</summary>
    public decimal Juros => Capital * (TaxaDeJuros / 100m);

    /// <summary>Capital + juros.</summary>
    public decimal Total => Capital + Juros;
}

/// <summary>Contexto da apuração de juros de mora.</summary>
public sealed record ContextoDeApuracaoDeJuros
{
    public required DateOnly DataAjuizamento { get; init; }

    /// <summary>Fase pré-judicial: os juros podem começar antes do ajuizamento (no vencimento).</summary>
    public bool FasePreJudicial { get; init; }

    /// <summary>
    /// Taxa acumulada (pontos percentuais) do dia informado (inclusive) até a liquidação —
    /// tipicamente <c>TabelaDeJurosService.CalcularTaxaAcumulada(regime, dia, liquidação)</c>.
    /// </summary>
    public required Func<DateOnly, decimal> TaxaAcumuladaAPartirDe { get; init; }
}

/// <summary>Resultado da apuração: os baldes e os totais que alimentam o bruto do reclamante.</summary>
public sealed record ResultadoDaApuracaoDeJuros(
    IReadOnlyList<ApuracaoDeJuros> Apuracoes,
    decimal TotalDeValorCorrigido,
    decimal TotalDeJuros);

/// <summary>
/// Apuração dos juros de mora sobre as verbas que compõem o principal (caso padrão
/// <c>BaseDeJurosDasVerbas = VERBAS</c>, juros habilitado).
///
/// <para>As ocorrências ativas das verbas com <see cref="VerbaEmCalculo.ComporPrincipal"/> são
/// agrupadas por (mês/ano da competência, data de início dos juros). O vencimento que dispara os
/// juros é a data inicial da ocorrência (férias) ou a final (demais); o início efetivo nunca é
/// anterior ao ajuizamento, salvo fase pré-judicial, e a contagem começa no dia seguinte. O
/// capital é a soma das diferenças corrigidas (cada uma a 2 casas HALF_EVEN) e os juros são
/// <c>capital × taxa/100</c>. Os totais arredondam cada balde a 2 casas antes de somar.</para>
/// </summary>
public static class ApuradorDeJuros
{
    public static ResultadoDaApuracaoDeJuros Apurar(
        IEnumerable<VerbaEmCalculo> verbas, ContextoDeApuracaoDeJuros contexto)
    {
        ArgumentNullException.ThrowIfNull(verbas);
        ArgumentNullException.ThrowIfNull(contexto);

        var baldes = new Dictionary<(int, int, DateOnly), ApuracaoDeJuros>();
        var ordem = new List<(int, int, DateOnly)>();

        foreach (var verba in verbas)
        {
            if (!verba.ComporPrincipal)
                continue;

            var ehFerias = verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias;
            foreach (var ocorrencia in verba.OcorrenciasAtivas)
            {
                var vencimento = ehFerias ? ocorrencia.DataInicial : ocorrencia.DataFinal;
                var inicioDosJuros = ResolverInicioDosJuros(vencimento, verba.JurosDoAjuizamento, contexto);

                var chave = (ocorrencia.DataInicial.Year, ocorrencia.DataInicial.Month, inicioDosJuros);
                if (!baldes.TryGetValue(chave, out var apuracao))
                {
                    apuracao = new ApuracaoDeJuros
                    {
                        Competencia = new DateOnly(ocorrencia.DataInicial.Year, ocorrencia.DataInicial.Month, 1),
                        DataInicial = inicioDosJuros,
                        TaxaDeJuros = contexto.TaxaAcumuladaAPartirDe(inicioDosJuros.AddDays(1)),
                    };
                    baldes[chave] = apuracao;
                    ordem.Add(chave);
                }

                apuracao.ValorCorrigido += Arredondar(ocorrencia.DiferencaCorrigida ?? 0m);
            }
        }

        var apuracoes = ordem.Select(k => baldes[k]).ToList();
        var totalCorrigido = apuracoes.Sum(a => Arredondar(a.ValorCorrigido));
        var totalJuros = apuracoes.Sum(a => Arredondar(a.Juros));

        return new ResultadoDaApuracaoDeJuros(apuracoes, totalCorrigido, totalJuros);
    }

    /// <summary>
    /// Pivô do início dos juros: no ajuizamento para ocorrências vencidas e vincendas; no
    /// vencimento (fase pré-judicial) ou no maior entre vencimento e ajuizamento, caso contrário.
    /// </summary>
    private static DateOnly ResolverInicioDosJuros(
        DateOnly vencimento, JurosDoAjuizamentoEnum jurosDoAjuizamento, ContextoDeApuracaoDeJuros contexto)
    {
        if (jurosDoAjuizamento == JurosDoAjuizamentoEnum.OcorrenciasVencidasEVincendas)
            return contexto.DataAjuizamento;

        if (contexto.FasePreJudicial)
            return vencimento;

        return vencimento <= contexto.DataAjuizamento ? contexto.DataAjuizamento : vencimento;
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
