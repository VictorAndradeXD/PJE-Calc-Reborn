namespace PJeCalc.Core.Models.Processo;

using PJeCalc.Core.Common;

public class Advogado : EntityBase
{
    public string? Nome { get; set; }
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? NumeroOAB { get; set; }
    public string? Tipo { get; set; }
    public long ProcessoId { get; set; }
    public Processo? Processo { get; set; }
}
