namespace PJeCalc.Core.Models.Pagamento;

using PJeCalc.Core.Common;

public class Pagamento : EntityBase
{
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataPagamento { get; set; }
    public bool PagarPrecatorio { get; set; }
    public decimal? ValorPagamento { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
