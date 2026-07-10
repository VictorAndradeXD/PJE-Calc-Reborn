using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Fase do calculo judicial.
/// </summary>
public enum FaseDoCalculoEnum
{
    [Description("Calculando Valor Devido")]
    CalculandoValorDevido,

    [Description("Calculando Valor Pago")]
    CalculandoValorPago
}
