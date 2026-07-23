using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Gera as férias do contrato a partir da admissão: períodos aquisitivos completos de
/// 12 meses (aniversário a aniversário — o resto fracionário vira férias proporcionais
/// nas verbas), reiniciados no dia seguinte ao término de cada falta marcada com
/// "reinicia férias" e quebrados no início das férias coletivas. O concessivo é o ano
/// seguinte ao aquisitivo; o prazo vem do art. 130 (faltas não justificadas do PA) e a
/// situação é sugerida por demissão/término do cálculo.
/// </summary>
public static class GeradorDePeriodosDeFerias
{
    public static List<FeriasDoCalculo> Gerar(ContextoDeVerbas contexto)
    {
        var resultado = new List<FeriasDoCalculo>();
        var fimDoContrato = contexto.DataDemissao ?? contexto.DataTerminoCalculo;
        if (fimDoContrato is not { } dataFinal)
            return resultado;

        foreach (var aquisitivo in EncontrarPeriodosAquisitivos(contexto, dataFinal))
        {
            var concessivo = PeriodoConcessivoDe(aquisitivo);
            var ferias = new FeriasDoCalculo
            {
                PeriodoAquisitivo = aquisitivo,
                PeriodoConcessivo = concessivo,
                Prazo = PrazoDeFerias.Calcular(
                    aquisitivo.Fim, contexto.RegimeDoContrato, contexto.ObterFaltasNaoJustificadas(aquisitivo)),
            };
            ferias.Situacao = SugerirSituacao(contexto, ferias);
            SugerirPrimeiroPeriodoDeGozo(contexto, ferias);
            resultado.Add(ferias);
        }
        return resultado;
    }

    private static List<PeriodoDeApuracao> EncontrarPeriodosAquisitivos(ContextoDeVerbas contexto, DateOnly dataFinal)
    {
        var periodos = new List<PeriodoDeApuracao>();
        var inicio = contexto.DataAdmissao;

        // Férias coletivas quebram o primeiro período: [admissão, véspera] + recomeço.
        if (contexto.InicioFeriasColetivas is { } coletivas && coletivas != inicio)
        {
            periodos.Add(new PeriodoDeApuracao(inicio, coletivas.AddDays(-1)));
            inicio = coletivas;
        }

        foreach (var reinicio in ReiniciosPorFalta(contexto))
        {
            periodos.AddRange(PeriodoDeApuracao.QuebrarEmAnos(inicio, reinicio, incluirResto: false));
            inicio = reinicio;
        }
        periodos.AddRange(PeriodoDeApuracao.QuebrarEmAnos(inicio, dataFinal, incluirResto: false));
        return periodos;
    }

    /// <summary>Dia seguinte ao término de cada falta que reinicia férias, em ordem.</summary>
    public static List<DateOnly> ReiniciosPorFalta(ContextoDeVerbas contexto) =>
        contexto.Faltas.Where(f => f.ReiniciaFerias)
            .Select(f => f.Fim.AddDays(1))
            .Order()
            .ToList();

    /// <summary>
    /// Concessivo = dia seguinte ao fim do aquisitivo + 1 ano; quando o aniversário cai
    /// no mesmo dia do mês, recua 1 dia (fim na véspera).
    /// </summary>
    public static PeriodoDeApuracao PeriodoConcessivoDe(PeriodoDeApuracao aquisitivo)
    {
        var inicio = aquisitivo.Fim.AddDays(1);
        var fim = inicio.AddYears(1);
        if (fim.Day == inicio.Day)
            fim = fim.AddDays(-1);
        return new PeriodoDeApuracao(inicio, fim);
    }

    private static SituacaoDaFeriasEnum SugerirSituacao(ContextoDeVerbas contexto, FeriasDoCalculo ferias)
    {
        if (ferias.Prazo == 0)
            return SituacaoDaFeriasEnum.Perdidas;

        var coletivas = EhPeriodoDeFeriasColetivas(contexto, ferias);
        if (!coletivas && contexto.DataDemissao is { } demissao && demissao < ferias.PeriodoConcessivo.Fim)
            return SituacaoDaFeriasEnum.Indenizadas;
        if (!coletivas && contexto.DataDemissao is null && ferias.PeriodoConcessivo.Fim > contexto.DataTerminoCalculo)
            return SituacaoDaFeriasEnum.NaoGozadas;
        return SituacaoDaFeriasEnum.Gozadas;
    }

    /// <summary>Período "quebrado" pelas coletivas: começa na admissão sem completar 12 meses.</summary>
    private static bool EhPeriodoDeFeriasColetivas(ContextoDeVerbas contexto, FeriasDoCalculo ferias) =>
        ferias.PeriodoAquisitivo.Inicio == contexto.DataAdmissao &&
        ferias.PeriodoAquisitivo.Fim != contexto.DataAdmissao.AddYears(1).AddDays(-1);

    private static void SugerirPrimeiroPeriodoDeGozo(ContextoDeVerbas contexto, FeriasDoCalculo ferias)
    {
        if (contexto.InicioFeriasColetivas is { } coletivas && ferias.PeriodoAquisitivo.Fim < coletivas)
        {
            ferias.PeriodoDeGozo1 = new PeriodoDeApuracao(coletivas, coletivas.AddDays(ferias.Prazo - 1));
        }
        else if (ferias.Situacao == SituacaoDaFeriasEnum.Gozadas && ferias.Prazo > 0)
        {
            // Gozo sugerido "encostado" no fim do período concessivo.
            var fim = ferias.PeriodoConcessivo.Fim;
            ferias.PeriodoDeGozo1 = new PeriodoDeApuracao(fim.AddDays(-(ferias.Prazo - 1)), fim);
        }
    }
}
