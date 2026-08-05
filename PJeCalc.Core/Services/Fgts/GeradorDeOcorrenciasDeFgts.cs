using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services.Fgts;

/// <summary>
/// Histórico salarial com incidência de FGTS: salário por competência (dia 1 do mês). Quando
/// <see cref="AplicarProporcionalidade"/>, o salário do mês é proporcionalizado por dias
/// (descontando férias/faltas). <see cref="Recolhido"/> marca o que já foi depositado no mês.
/// </summary>
public sealed record HistoricoSalarialDeFgts(
    IReadOnlyDictionary<DateOnly, decimal> SalarioPorCompetencia,
    bool AplicarProporcionalidade = false,
    bool Recolhido = false);

/// <summary>Contexto da geração das ocorrências mensais de FGTS.</summary>
public sealed record ContextoDeGeracaoDeFgts
{
    public required DateOnly Admissao { get; init; }
    public DateOnly? Demissao { get; init; }
    public required DateOnly TerminoCalculo { get; init; }
    public required DateOnly Ajuizamento { get; init; }
    public bool AplicarPrescricao { get; init; }

    public AliquotaDoFgtsEnum Aliquota { get; init; } = AliquotaDoFgtsEnum.OitoPorCento;

    public IReadOnlyList<HistoricoSalarialDeFgts> Historicos { get; init; } = [];

    /// <summary>Dias de férias no mês (proporcionalidade); regra do 31 quando dias − férias = 31.</summary>
    public Func<DateOnly, int>? DiasDeFeriasNoMes { get; init; }

    /// <summary>Faltas não justificadas no mês (proporcionalidade).</summary>
    public Func<DateOnly, int>? FaltasNaoJustificadasNoMes { get; init; }

    // Complementos vindos de módulos já validados (verbas/correção/juros):
    public Func<DateOnly, decimal>? BaseVerbaNoMes { get; init; }
    public Func<DateOnly, decimal>? BaseVerbaSemAvisoNoMes { get; init; }
    public Func<DateOnly, decimal>? IndiceDeLiquidacaoNoMes { get; init; }
    public Func<DateOnly, decimal>? IndiceDeDemissaoNoMes { get; init; }
    public Func<DateOnly, decimal>? TaxaDeJurosNoMes { get; init; }
}

/// <summary>
/// Gera as ocorrências mensais de FGTS ao longo da janela do contrato (recortada pela
/// prescrição), montando a base de cada competência a partir do histórico salarial (com a
/// proporcionalidade do FGTS) e das verbas. O resultado alimenta <see cref="TotaisDeFgts"/> e
/// <see cref="MultaDoFgts"/>.
/// </summary>
public static class GeradorDeOcorrenciasDeFgts
{
    public static IReadOnlyList<ApuracaoMensalDeFgts> Gerar(ContextoDeGeracaoDeFgts contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var (inicial, final) = PrescricaoDoFgts.JanelaDeGeracao(
            contexto.Admissao, contexto.Demissao, contexto.TerminoCalculo,
            contexto.Ajuizamento, contexto.AplicarPrescricao);

        var fator = contexto.Aliquota == AliquotaDoFgtsEnum.DoisPorCento ? 0.02m : 0.08m;
        var ocorrencias = new List<ApuracaoMensalDeFgts>();

        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(inicial, final))
        {
            var competencia = PeriodoDeApuracao.Competencia(mes.Inicio);
            decimal baseHistorico = 0m, baseRecolhida = 0m;

            foreach (var historico in contexto.Historicos)
            {
                if (!historico.SalarioPorCompetencia.TryGetValue(competencia, out var salario))
                    continue;

                var valor = historico.AplicarProporcionalidade
                    ? Proporcionalizar(mes, salario, contexto)
                    : salario;

                baseHistorico += valor;
                if (historico.Recolhido)
                    baseRecolhida += valor;
            }

            ocorrencias.Add(new ApuracaoMensalDeFgts
            {
                Competencia = competencia,
                BaseHistorico = baseHistorico,
                BaseVerba = contexto.BaseVerbaNoMes?.Invoke(competencia) ?? 0m,
                BaseVerbaSemAvisoPrevio =
                    contexto.BaseVerbaSemAvisoNoMes?.Invoke(competencia)
                    ?? contexto.BaseVerbaNoMes?.Invoke(competencia) ?? 0m,
                Aliquota = contexto.Aliquota,
                Depositado = fator * baseRecolhida,
                IndiceAcumulado = contexto.IndiceDeLiquidacaoNoMes?.Invoke(competencia) ?? 1m,
                IndiceAcumuladoDaMulta = contexto.IndiceDeDemissaoNoMes?.Invoke(competencia) ?? 1m,
                TaxaDeJuros = contexto.TaxaDeJurosNoMes?.Invoke(competencia) ?? 0m,
            });
        }

        return ocorrencias;
    }

    /// <summary>
    /// Proporcionaliza o salário do mês: exclui os dias de férias; se os dias líquidos ficarem
    /// em 31, a regra do 31 conta 1 dia a excluir; então soma as faltas não justificadas.
    /// </summary>
    private static decimal Proporcionalizar(PeriodoDeApuracao mes, decimal salario, ContextoDeGeracaoDeFgts contexto)
    {
        var ferias = contexto.DiasDeFeriasNoMes?.Invoke(mes.Inicio) ?? 0;
        var exclusoes = ferias;
        if (mes.TotalDeDias - ferias == 31)
            exclusoes = 1;
        exclusoes += contexto.FaltasNaoJustificadasNoMes?.Invoke(mes.Inicio) ?? 0;

        return Proporcionalizacao.Proporcionalizar(mes.Inicio, mes.Fim, salario, exclusoes);
    }
}
