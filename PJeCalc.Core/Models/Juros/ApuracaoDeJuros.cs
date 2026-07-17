namespace PJeCalc.Core.Models.Juros;

using PJeCalc.Core.Common;

public class ApuracaoDeJuros : EntityBase
{
    public DateTime? Competencia { get; set; }
    public DateTime? DataInicial { get; set; }
    public decimal? ValorCorrigido { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public decimal? ValorVerbaParaContribuicaoSocial { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
