namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Divisores de conversão entre os padrões monetários brasileiros. Como um cálculo
/// pode retroagir décadas, ao acumular índices a contribuição do mês que marca uma
/// troca de moeda é dividida pelo respectivo divisor
/// (Cruzeiro → Cruzado → Cruzado Novo → Cruzeiro Real → Real).
///
/// Datas e divisores idênticos a
/// <c>ConversaoDeMoedas.COMPETENCIAS_MENSAIS_PARA_CONVERSAO_DE_MOEDAS</c> do PJe-Calc
/// original. A divisão é aplicada por competência (não no fator total) para reproduzir
/// exatamente o motor oficial e evitar estouro do <see cref="decimal"/> em períodos de
/// hiperinflação.
/// </summary>
public static class ConversaoDeMoedas
{
    private static readonly Dictionary<DateOnly, decimal> DivisoresPorCompetencia = new()
    {
        [new DateOnly(1967, 2, 1)] = 1000m,  // Cruzeiro Novo
        [new DateOnly(1986, 3, 1)] = 1000m,  // Cruzado
        [new DateOnly(1989, 1, 1)] = 1000m,  // Cruzado Novo
        [new DateOnly(1993, 8, 1)] = 1000m,  // Cruzeiro Real
        [new DateOnly(1994, 7, 1)] = 2750m,  // Real (URV)
    };

    /// <summary>
    /// Divisor de conversão da competência informada, ou 1 quando ela não marca troca
    /// de moeda.
    /// </summary>
    public static decimal DivisorNaCompetencia(DateOnly competencia) =>
        DivisoresPorCompetencia.TryGetValue(competencia, out var divisor) ? divisor : 1m;
}
