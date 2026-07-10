namespace PJeCalc.Core.Models.Inss;

using PJeCalc.Core.Common;

public class AliquotaEmpregadorPorPeriodo : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataTermino { get; set; }
    public decimal? AliquotaEmpresa { get; set; }
    public decimal? AliquotaRAT { get; set; }
    public decimal? AliquotaTerceiros { get; set; }
    public long InssId { get; set; }
    public Inss? Inss { get; set; }
}
