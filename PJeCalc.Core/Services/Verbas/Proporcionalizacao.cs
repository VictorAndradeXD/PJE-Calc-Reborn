namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Proporcionalização e integralização de valores mensais por dias, na convenção do
/// PJe-Calc: o mês vale <b>30 dias</b> (28/29 em fevereiro), os dias contados têm piso 0
/// e teto 30, e vários chamadores aplicam antes a "regra do 31" (mês de 31 dias conta
/// como se tivesse 1 dia a excluir).
/// </summary>
public static class Proporcionalizacao
{
    /// <summary>Valor proporcional aos dias do período: valor × dias ÷ D.</summary>
    public static decimal Proporcionalizar(DateOnly inicio, DateOnly fim, decimal valor, int exclusoes = 0)
    {
        var dias = DiasComputaveis(inicio, fim, exclusoes);
        return valor * dias / DivisorDoMes(inicio);
    }

    /// <summary>Valor "mensalizado" a partir de um período parcial: valor × D ÷ dias.</summary>
    public static decimal Integralizar(DateOnly inicio, DateOnly fim, decimal valor, int exclusoes = 0)
    {
        var dias = DiasComputaveis(inicio, fim, exclusoes);
        return dias == 0 ? 0m : valor * DivisorDoMes(inicio) / dias;
    }

    /// <summary>
    /// Regra do 31, aplicada pelos chamadores antes de proporcionalizar: quando os dias
    /// líquidos do período são exatamente 31, o mês conta com 1 dia a excluir.
    /// </summary>
    public static int AjustarExclusoesParaMesDe31(int totalDeDias, int exclusoes) =>
        totalDeDias - exclusoes == 31 ? 1 : exclusoes;

    /// <summary>Dias corridos do período (inclusivo), menos exclusões, com piso 0 e teto 30.</summary>
    public static int DiasComputaveis(DateOnly inicio, DateOnly fim, int exclusoes)
    {
        var dias = fim.DayNumber - inicio.DayNumber + 1 - exclusoes;
        return Math.Clamp(dias, 0, 30);
    }

    /// <summary>28/29 em fevereiro; 30 nos demais meses (mesmo os de 31 dias).</summary>
    public static int DivisorDoMes(DateOnly data) =>
        data.Month == 2 ? DateTime.DaysInMonth(data.Year, 2) : 30;
}
