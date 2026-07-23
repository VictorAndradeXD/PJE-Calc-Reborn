namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Regra do sábado útil do contrato: um booleano com períodos de exceção que INVERTEM o
/// valor (a primeira exceção que contém a data decide) — o "LogicoFuzzy" do motor.
/// </summary>
public sealed record SabadoUtil(bool Considerar, IReadOnlyList<PeriodoDeApuracao> Excecoes)
{
    public static readonly SabadoUtil Sim = new(true, []);
    public static readonly SabadoUtil Nao = new(false, []);

    public bool EhUtil(DateOnly data)
    {
        foreach (var excecao in Excecoes)
        {
            if (data >= excecao.Inicio && data <= excecao.Fim)
                return !Considerar;
        }
        return Considerar;
    }
}

/// <summary>
/// Contagens de calendário do motor (inclusivas nas duas pontas):
/// <list type="bullet">
/// <item><b>Dia útil</b> — segunda a sexta não feriado, mais o sábado quando útil
/// (feriado exclui mesmo o sábado útil); domingo nunca.</item>
/// <item><b>Repouso</b> — domingo, mais o sábado não útil; feriado em dia de semana
/// NÃO conta como repouso.</item>
/// <item><b>Feriado</b> — qualquer feriado, mesmo em sábado/domingo.</item>
/// <item><b>Repousos e feriados</b> — união (um domingo-feriado conta uma vez).</item>
/// </list>
/// </summary>
public static class CalendarioTrabalhista
{
    public static int TotalDeDiasUteis(PeriodoDeApuracao periodo, SabadoUtil sabado, Func<DateOnly, bool> ehFeriado) =>
        Contar(periodo, d => (sabado.EhUtil(d) || d.DayOfWeek != DayOfWeek.Saturday)
                             && d.DayOfWeek != DayOfWeek.Sunday && !ehFeriado(d));

    public static int TotalDeRepousos(PeriodoDeApuracao periodo, SabadoUtil sabado) =>
        Contar(periodo, d => (!sabado.EhUtil(d) && d.DayOfWeek == DayOfWeek.Saturday)
                             || d.DayOfWeek == DayOfWeek.Sunday);

    public static int TotalDeFeriados(PeriodoDeApuracao periodo, Func<DateOnly, bool> ehFeriado) =>
        Contar(periodo, ehFeriado);

    public static int TotalDeRepousosEFeriados(PeriodoDeApuracao periodo, SabadoUtil sabado, Func<DateOnly, bool> ehFeriado) =>
        Contar(periodo, d => (!sabado.EhUtil(d) && d.DayOfWeek == DayOfWeek.Saturday)
                             || d.DayOfWeek == DayOfWeek.Sunday || ehFeriado(d));

    private static int Contar(PeriodoDeApuracao periodo, Func<DateOnly, bool> criterio)
    {
        var total = 0;
        for (var d = periodo.Inicio; d <= periodo.Fim; d = d.AddDays(1))
        {
            if (criterio(d))
                total++;
        }
        return total;
    }
}
