namespace PJeCalc.Core.Models.Custas;

using PJeCalc.Core.Common;

public class CustaPaga : EntityBase
{
    public string? TipoDePagante { get; set; }
    public decimal? Valor { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal? IndiceCorrecao { get; set; }
    public decimal? TaxaJuros { get; set; }
    public long CustasJudiciaisId { get; set; }
    public CustasJudiciais? CustasJudiciais { get; set; }
}
