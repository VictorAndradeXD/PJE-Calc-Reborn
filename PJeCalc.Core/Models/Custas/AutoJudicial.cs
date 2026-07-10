namespace PJeCalc.Core.Models.Custas;

using PJeCalc.Core.Common;

public class AutoJudicial : EntityBase
{
    public string? TipoDeAuto { get; set; }
    public decimal? ValorAvaliacao { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal? ValorTeto { get; set; }
    public decimal? ValorCustas { get; set; }
    public decimal? IndiceCorrecao { get; set; }
    public decimal? TaxaJuros { get; set; }
    public long CustasJudiciaisId { get; set; }
    public CustasJudiciais? CustasJudiciais { get; set; }
}
