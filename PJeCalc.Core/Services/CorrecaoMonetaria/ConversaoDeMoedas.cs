namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Divisores de conversão entre os padrões monetários brasileiros. Como um cálculo
/// pode retroagir décadas, ao acumular índices atravessando uma data de troca de
/// moeda o fator acumulado é dividido pelo respectivo divisor
/// (Cruzeiro → Cruzado → Cruzado Novo → Cruzeiro Real → Real).
///
/// Referência: ConversaoDeMoedas.java do PJe-Calc original. Para períodos
/// integralmente posteriores a julho/1994 nenhum corte se aplica (divisor 1).
/// </summary>
public static class ConversaoDeMoedas
{
    private static readonly (DateOnly Corte, decimal Divisor)[] Cortes =
    [
        (new DateOnly(1967, 1, 1), 1000m),  // Cruzeiro Novo
        (new DateOnly(1986, 3, 1), 1000m),  // Cruzado
        (new DateOnly(1989, 1, 1), 1000m),  // Cruzado Novo
        (new DateOnly(1993, 7, 1), 1000m),  // Cruzeiro Real
        (new DateOnly(1994, 7, 1), 2750m),  // Real (URV)
    ];

    /// <summary>
    /// Divisor acumulado a aplicar quando o intervalo (inicio, fim] atravessa
    /// datas de troca de moeda. Retorna 1 quando não há corte no intervalo.
    /// </summary>
    public static decimal DivisorNoIntervalo(DateOnly inicio, DateOnly fim)
    {
        var divisor = 1m;
        foreach (var (corte, valor) in Cortes)
        {
            if (corte > inicio && corte <= fim)
                divisor *= valor;
        }
        return divisor;
    }
}
