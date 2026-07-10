namespace PJeCalc.Core.Models.Irpf;

using PJeCalc.Core.Common;

public class OcorrenciaDeIrpf : EntityBase
{
    public DateTime? DataOcorrencia { get; set; }
    public decimal? ValorVerbas { get; set; }
    public decimal? ValorJuros { get; set; }
    public decimal? ValorContribuicaoSocial { get; set; }
    public decimal? ValorPrevidenciaPrivada { get; set; }
    public decimal? ValorPensaoAlimenticia { get; set; }
    public decimal? ValorHonorarios { get; set; }
    public decimal? ValorDependentes { get; set; }
    public decimal? ValorBase { get; set; }
    public int? QuantidadeCompetencias { get; set; }
    public long IrpfId { get; set; }
    public Irpf? Irpf { get; set; }
}
