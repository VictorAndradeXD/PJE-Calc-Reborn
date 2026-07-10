namespace PJeCalc.Core.Models.Processo;

using PJeCalc.Core.Enums;

public class Reclamado
{
    public string? Nome { get; set; }
    public TipoDocumentoFiscalEnum? TipoDocumentoFiscal { get; set; }
    public string? NumeroDocumentoFiscal { get; set; }
}
