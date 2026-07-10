namespace PJeCalc.Core.Models.Auditoria;

using PJeCalc.Core.Common;

public class Auditoria : EntityBase
{
    public long? IdCalculo { get; set; }
    public string? Nome { get; set; }
    public string? Cpf { get; set; }
    public DateTime? DataEvento { get; set; }
    public string? TipoAcao { get; set; }
}
