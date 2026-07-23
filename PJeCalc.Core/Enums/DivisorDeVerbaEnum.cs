using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Origem do divisor da fórmula da verba.
/// </summary>
public enum DivisorDeVerbaEnum
{
    [Description("Carga Horária")]
    CargaHoraria,

    [Description("Dias Úteis")]
    DiasUteis,

    [Description("Outro Valor")]
    OutroValor,

    [Description("Importada do Cartão de Ponto")]
    ImportadaDoCartao
}
