namespace PJeCalc.Core.Common;

public static class Constantes
{
    public static readonly DateTime DataReformaTrabalhista = new(2017, 11, 11);
    public static readonly DateTime DataReformaPrevidencia = new(2020, 3, 1);
    public static readonly DateTime DataLimiteAvisoPrevioCalculado = new(2011, 10, 13);
    public static readonly int DiasAvisoPrevioPadrao = 30;

    /// <summary>
    /// Historical Brazilian currency conversion factors.
    /// Each entry maps a date to the divisor used when converting from the old currency
    /// to the new one that took effect on that date.
    ///
    /// Timeline:
    ///   1967-02-13: Cruzeiro -> Cruzeiro Novo (divide by 1,000)
    ///   1986-03-01: Cruzeiro -> Cruzado (divide by 1,000)
    ///   1989-01-16: Cruzado -> Cruzado Novo (divide by 1,000)
    ///   1993-08-01: Cruzeiro -> Cruzeiro Real (divide by 1,000)
    ///   1994-07-01: Cruzeiro Real -> Real (divide by 2,750)
    /// </summary>
    public static readonly SortedDictionary<DateTime, decimal> ConversoesDeMoedaDiarias = new()
    {
        { new DateTime(1967, 2, 13), 1_000m },
        { new DateTime(1986, 3, 1), 1_000m },
        { new DateTime(1989, 1, 16), 1_000m },
        { new DateTime(1993, 8, 1), 1_000m },
        { new DateTime(1994, 7, 1), 2_750m },
    };

    public static readonly SortedDictionary<DateTime, decimal> ConversoesDeMoedaMensais = new()
    {
        { new DateTime(1967, 2, 1), 1_000m },
        { new DateTime(1986, 3, 1), 1_000m },
        { new DateTime(1989, 1, 1), 1_000m },
        { new DateTime(1993, 8, 1), 1_000m },
        { new DateTime(1994, 7, 1), 2_750m },
    };

    public static readonly DateTime DataUltimaConversaoDeMoeda = new(1994, 7, 1);

    public static decimal ObterFatorConversaoMoeda(DateTime dataInicio, DateTime dataFim)
    {
        decimal fator = 1m;

        foreach (var (dataConversao, divisor) in ConversoesDeMoedaDiarias)
        {
            if (dataConversao > dataInicio && dataConversao <= dataFim)
            {
                fator *= divisor;
            }
        }

        return fator;
    }
}
