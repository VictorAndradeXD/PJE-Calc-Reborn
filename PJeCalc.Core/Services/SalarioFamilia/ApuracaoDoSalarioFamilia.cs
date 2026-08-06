using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services.SalarioFamilia;

/// <summary>Ocorrência mensal do salário-família.</summary>
public sealed record OcorrenciaDeSalarioFamilia(
    DateOnly Competencia,
    int QuantidadeFilhos,
    decimal RemuneracaoMensal,
    decimal? Cota,
    decimal ValorDevido);

/// <summary>Contexto da apuração do salário-família.</summary>
public sealed record ContextoDoSalarioFamilia
{
    public required DateOnly DataInicial { get; init; }
    public required DateOnly DataFinal { get; init; }
    public required DateOnly Admissao { get; init; }
    public DateOnly? Demissao { get; init; }

    /// <summary>Quantidade de filhos com direito ao benefício na competência.</summary>
    public required Func<DateOnly, int> FilhosNoMes { get; init; }

    /// <summary>Remuneração mensal (diferenças das verbas + salário pago) na competência.</summary>
    public required Func<DateOnly, decimal> RemuneracaoNoMes { get; init; }

    /// <summary>Tabela do salário-família vigente na competência.</summary>
    public required Func<DateOnly, TabelaSalarioFamilia?> TabelaNoMes { get; init; }
}

/// <summary>Resultado da apuração do salário-família.</summary>
public sealed record ResultadoDoSalarioFamilia(
    IReadOnlyList<OcorrenciaDeSalarioFamilia> Ocorrencias,
    decimal TotalDevido);

/// <summary>
/// Apuração do salário-família mês a mês: para cada competência, a cota da faixa em que cai a
/// remuneração é multiplicada pelo número de filhos; a cota é proporcionalizada por dias nos
/// meses de admissão e de demissão. O devido de cada mês é arredondado a 2 casas (HALF_EVEN).
/// </summary>
public static class ApuracaoDoSalarioFamilia
{
    public static ResultadoDoSalarioFamilia Apurar(ContextoDoSalarioFamilia contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var ocorrencias = new List<OcorrenciaDeSalarioFamilia>();
        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(contexto.DataInicial, contexto.DataFinal))
        {
            var competencia = PeriodoDeApuracao.Competencia(mes.Inicio);
            var filhos = contexto.FilhosNoMes(competencia);
            var remuneracao = contexto.RemuneracaoNoMes(competencia);
            var cota = contexto.TabelaNoMes(competencia)?.ObterCota(remuneracao);

            decimal devido;
            if (cota is { } valorCota)
            {
                var cotaProporcional = Proporcionalizar(valorCota, competencia, contexto);
                devido = Math.Round(cotaProporcional * filhos, 2, MidpointRounding.ToEven);
            }
            else
            {
                devido = 0m;
            }

            ocorrencias.Add(new OcorrenciaDeSalarioFamilia(competencia, filhos, remuneracao, cota, devido));
        }

        return new ResultadoDoSalarioFamilia(ocorrencias, ocorrencias.Sum(o => o.ValorDevido));
    }

    /// <summary>Proporcionaliza a cota por dias nos meses de admissão e de demissão.</summary>
    private static decimal Proporcionalizar(decimal cota, DateOnly competencia, ContextoDoSalarioFamilia contexto)
    {
        if (MesmoMes(competencia, contexto.Admissao))
            cota = Proporcionalizacao.Proporcionalizar(contexto.Admissao, PeriodoDeApuracao.UltimoDiaDoMes(contexto.Admissao), cota);

        if (contexto.Demissao is { } demissao && MesmoMes(competencia, demissao))
            cota = Proporcionalizacao.Proporcionalizar(PeriodoDeApuracao.Competencia(demissao), demissao, cota);

        return cota;
    }

    private static bool MesmoMes(DateOnly a, DateOnly b) => a.Year == b.Year && a.Month == b.Month;
}
