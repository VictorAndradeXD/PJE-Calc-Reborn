namespace PJeCalc.Core.Models.Fgts;

using PJeCalc.Core.Common;

public class OperacaoDeFgts : EntityBase
{
    public DateTime? Competencia { get; set; }
    public string? TipoDeOperacao { get; set; }
    public decimal? Valor { get; set; }
    public decimal? IndiceAcumulado { get; set; }
    public decimal? IndiceAcumuladoDaMulta { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public long FgtsId { get; set; }
    public Fgts? Fgts { get; set; }
}
