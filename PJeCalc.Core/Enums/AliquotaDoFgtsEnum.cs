using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Alíquotas do FGTS aplicáveis ao contrato de trabalho.
/// A alíquota de 2% aplica-se a contratos de aprendizagem.
/// A alíquota de 8% aplica-se aos demais contratos.
/// </summary>
public enum AliquotaDoFgtsEnum
{
    /// <summary>
    /// Alíquota de 2% (taxa: 0.02). Aplicável a contratos de aprendizagem.
    /// </summary>
    [Description("2%")]
    DoisPorCento,

    /// <summary>
    /// Alíquota de 8% (taxa: 0.08). Aplicável aos demais contratos.
    /// </summary>
    [Description("8%")]
    OitoPorCento
}
