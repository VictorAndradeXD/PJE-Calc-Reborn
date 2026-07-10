namespace PJeCalc.Core.Models.SeguroDesemprego;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class SeguroDesemprego : EntityBase
{
    public bool ApurarSeguroDesemprego { get; set; }
    public bool EmpregadoDomestico { get; set; }
    public int? NumeroDeParcelas { get; set; }
    public decimal? RemuneracaoMensal { get; set; }
    public decimal? ValorSeguroDesemprego { get; set; }
    public decimal? IndiceDeCorrecao { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public LogicoEnum? ComporPrincipal { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
