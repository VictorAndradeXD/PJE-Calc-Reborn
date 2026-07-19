using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Como as pontas parciais de um período de juros contam meses:
/// por fração de mês (pro-rata dos dias) ou por mês inteiro (regra dos ≥15 dias).
/// </summary>
public enum TipoDeQuantidadeDeJurosBaseEnum
{
    [Description("Inteiro")]
    Inteiro,

    [Description("Fração")]
    Fracao
}
