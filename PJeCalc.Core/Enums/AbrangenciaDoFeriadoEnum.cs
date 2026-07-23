using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Abrangência territorial do feriado.</summary>
public enum AbrangenciaDoFeriadoEnum
{
    [Description("Nacional")]
    Federal,

    [Description("Estadual")]
    Estadual,

    [Description("Municipal")]
    Municipal
}
