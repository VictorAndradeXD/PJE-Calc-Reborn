namespace PJeCalc.Core.Models.Feriado;

using PJeCalc.Core.Common;

public class ExcecaoDoFeriado : EntityBase
{
    public DateTime? Data { get; set; }
    public long FeriadoId { get; set; }
    public Feriado? Feriado { get; set; }
}
