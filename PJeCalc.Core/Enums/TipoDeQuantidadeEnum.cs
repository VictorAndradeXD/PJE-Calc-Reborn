using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Origem da quantidade da fórmula da verba.
/// </summary>
public enum TipoDeQuantidadeEnum
{
    [Description("Informada")]
    Informada,

    [Description("Importada do Calendário")]
    ImportadaDoCalendario,

    [Description("Importada do Cartão de Ponto")]
    ImportadaDoCartao,

    [Description("Avos")]
    Avos,

    [Description("Apurada")]
    Apurada
}
