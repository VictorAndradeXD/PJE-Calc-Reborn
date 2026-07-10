namespace PJeCalc.Core.Models.ValeTransporte;

using PJeCalc.Core.Common;

public class ValorValeTransporte : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataTermino { get; set; }
    public decimal? Valor { get; set; }
    public long ValeTransporteId { get; set; }
    public ValeTransporte? ValeTransporte { get; set; }
}
