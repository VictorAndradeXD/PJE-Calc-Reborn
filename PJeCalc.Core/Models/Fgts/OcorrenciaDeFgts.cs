namespace PJeCalc.Core.Models.Fgts;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class OcorrenciaDeFgts : EntityBase
{
    public DateTime? Ocorrencia { get; set; }
    public decimal? BaseHistorico { get; set; }
    public decimal? BaseVerba { get; set; }
    public decimal? BaseVerbaSemAvisoPrevio { get; set; }
    public AliquotaDoFgtsEnum? AliquotaDoFgts { get; set; }
    public decimal? Depositado { get; set; }
    public decimal? IndiceAcumulado { get; set; }
    public decimal? IndiceAcumuladoDaMulta { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public long FgtsId { get; set; }
    public Fgts? Fgts { get; set; }
}
