namespace PJeCalc.Core.Models.CartaoDePonto;

using PJeCalc.Core.Common;

public class OcorrenciaDoCartaoDePonto : EntityBase
{
    public DateTime? DataOcorrencia { get; set; }
    public decimal? Valor { get; set; }
    public long CartaoDePontoId { get; set; }
    public CartaoDePonto? CartaoDePonto { get; set; }
}
