using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Fonte das séries mensais de índices de correção monetária.
/// Abstrai a origem dos dados (banco de referência, memória, fixtures de teste),
/// permitindo exercitar o cálculo sem infraestrutura de persistência.
/// </summary>
public interface IIndiceProvider
{
    /// <summary>
    /// Série mensal do índice, ordenada por competência ascendente.
    /// Cada competência é o primeiro dia do respectivo mês.
    /// </summary>
    IReadOnlyList<IndiceMensal> ObterSerieMensal(IndiceMonetarioEnum indice);
}
