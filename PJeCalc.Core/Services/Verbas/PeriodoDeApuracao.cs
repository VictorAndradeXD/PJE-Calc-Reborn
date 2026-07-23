namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Intervalo de datas (inclusivo) usado na apuração de verbas, com os helpers de
/// calendário do motor: quebra em meses (primeiro/último possivelmente parciais)
/// e competência (dia 1 do mês).
/// </summary>
public readonly record struct PeriodoDeApuracao(DateOnly Inicio, DateOnly Fim)
{
    public int TotalDeDias => Fim.DayNumber - Inicio.DayNumber + 1;

    public bool DatasDoMesmoMes => Inicio.Year == Fim.Year && Inicio.Month == Fim.Month;

    /// <summary>Competência (dia 1 do mês) de uma data.</summary>
    public static DateOnly Competencia(DateOnly data) => new(data.Year, data.Month, 1);

    public static DateOnly UltimoDiaDoMes(DateOnly data) =>
        new(data.Year, data.Month, DateTime.DaysInMonth(data.Year, data.Month));

    /// <summary>
    /// Quebra o intervalo em períodos mensais: o primeiro começa em <paramref name="inicio"/>
    /// e o último termina em <paramref name="fim"/> (parciais quando não coincidem com o mês).
    /// </summary>
    public static List<PeriodoDeApuracao> QuebrarEmMeses(DateOnly inicio, DateOnly fim)
    {
        var periodos = new List<PeriodoDeApuracao>();
        var atual = inicio;
        while (atual <= fim)
        {
            var fimDoMes = UltimoDiaDoMes(atual);
            periodos.Add(new PeriodoDeApuracao(atual, fim < fimDoMes ? fim : fimDoMes));
            atual = Competencia(atual).AddMonths(1);
        }
        return periodos;
    }

    /// <summary>Como <see cref="QuebrarEmMeses(DateOnly, DateOnly)"/>, filtrando um mês (1–12).</summary>
    public static List<PeriodoDeApuracao> QuebrarEmMeses(DateOnly inicio, DateOnly fim, int mes) =>
        QuebrarEmMeses(inicio, fim).Where(p => p.Inicio.Month == mes).ToList();

    /// <summary>
    /// Anos completos entre duas datas na convenção do motor: conta enquanto
    /// <c>inicio + (n+1) anos − 1 dia ≤ fim</c>.
    /// </summary>
    public static int AnosCompletos(DateOnly inicio, DateOnly fim)
    {
        var anos = 0;
        while (inicio.AddYears(anos + 1).AddDays(-1) <= fim)
            anos++;
        return anos;
    }
}
