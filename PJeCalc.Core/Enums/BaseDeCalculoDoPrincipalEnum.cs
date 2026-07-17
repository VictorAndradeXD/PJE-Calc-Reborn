using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Base de calculo utilizada para o valor principal.
/// </summary>
public enum BaseDeCalculoDoPrincipalEnum
{
    [Description("Última Remuneração")]
    UltimaRemuneracao,

    [Description("Maior Remuneração")]
    MaiorRemuneracao,

    [Description("Histórico Salarial")]
    HistoricoSalarial,

    [Description("Piso Salarial")]
    SalarioDaCategoria,

    [Description("Salário Mínimo")]
    SalarioMinimo
}
