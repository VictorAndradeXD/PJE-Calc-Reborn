using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de origem do registro (calculo ou atualizacao).
/// </summary>
public enum TipoOrigemRegistroEnum
{
    [Description("Cálculo")]
    Calculo,

    [Description("Atualização")]
    Atualizacao
}
