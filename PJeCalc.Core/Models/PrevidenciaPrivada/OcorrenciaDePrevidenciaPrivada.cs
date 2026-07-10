namespace PJeCalc.Core.Models.PrevidenciaPrivada;

using PJeCalc.Core.Common;

public class OcorrenciaDePrevidenciaPrivada : EntityBase
{
    public DateTime? Competencia { get; set; }
    public decimal? ValorBase { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? IndiceAcumulado { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public long PrevidenciaPrivadaId { get; set; }
    public PrevidenciaPrivada? PrevidenciaPrivada { get; set; }
}
