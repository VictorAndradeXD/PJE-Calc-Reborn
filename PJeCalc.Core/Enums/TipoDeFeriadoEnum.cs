using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Tipo do feriado cadastrado (bancários nunca contam no calendário trabalhista).</summary>
public enum TipoDeFeriadoEnum
{
    [Description("Feriado")]
    Feriado,

    [Description("Ponto Facultativo")]
    PontoFacultativo,

    [Description("Bancário")]
    Bancario
}
