namespace PJeCalc.Core.Models.Processo;

using PJeCalc.Core.Enums;

public class Reclamante
{
    public string? Nome { get; set; }
    public TipoDocumentoFiscalEnum? TipoDocumentoFiscal { get; set; }
    public string? NumeroDocumentoFiscal { get; set; }
    public string? TipoDocumentoPrevidenciario { get; set; }
    public string? NumeroDocumentoPrevidenciario { get; set; }
}
