using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Fonte das faixas históricas de juros por regime (JurosPadrão, Fazenda Pública, ...).
/// Abstrai a origem (banco de referência, fixtures de teste), permitindo exercitar o
/// cálculo sem infraestrutura.
/// </summary>
public interface IJurosFaixaProvider
{
    /// <summary>
    /// Faixas do regime que se sobrepõem ao intervalo [inicio, fim], ordenadas por
    /// data de início ascendente.
    /// </summary>
    IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim);
}
