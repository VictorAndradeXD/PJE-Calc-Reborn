namespace PJeCalc.Core.Models.Custas;

using PJeCalc.Core.Common;

public class Armazenamento : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataTermino { get; set; }
    public decimal? ValorAvaliacao { get; set; }
    public int? QtdDias { get; set; }
    public decimal? ValorCustas { get; set; }
    public decimal? IndiceCorrecao { get; set; }
    public decimal? TaxaJuros { get; set; }
    public long CustasJudiciaisId { get; set; }
    public CustasJudiciais? CustasJudiciais { get; set; }
}
