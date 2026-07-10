using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipo de documento fiscal do reclamado.
/// </summary>
public enum TipoDocumentoFiscalEnum
{
    [Description("CPF")]
    CPF,

    [Description("CNPJ")]
    CNPJ,

    [Description("CEI")]
    CEI
}
