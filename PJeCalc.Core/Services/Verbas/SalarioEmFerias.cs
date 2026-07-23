namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Rateio do salário durante um gozo de férias que pode cruzar dois meses:
/// <c>valorMes1 ÷ 30 × dias no 1º mês + valorMes2 ÷ 30 × dia final no 2º mês</c>.
/// Peculiaridades preservadas do motor: o 1º mês conta os dias civis até o fim do mês,
/// o 2º usa o dia-do-mês da data final, e o divisor é 30 fixo.
/// </summary>
public static class SalarioEmFerias
{
    public static decimal Calcular(PeriodoDeApuracao periodo, decimal valorMes1, decimal? valorMes2)
    {
        if (periodo.DatasDoMesmoMes)
            return valorMes1 / 30m * periodo.TotalDeDias;

        if (valorMes1 < 0m)
            return 0m;

        var fimDoPrimeiroMes = PeriodoDeApuracao.UltimoDiaDoMes(periodo.Inicio);
        var resultado = valorMes1 / 30m * (fimDoPrimeiroMes.DayNumber - periodo.Inicio.DayNumber + 1);
        if (valorMes2 is { } v2 && v2 >= 0m)
            resultado += v2 / 30m * periodo.Fim.Day;
        return resultado;
    }
}
