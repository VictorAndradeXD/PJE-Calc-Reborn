namespace PJeCalc.Core.Models.Irpf;

using PJeCalc.Core.Common;

public class Irpf : EntityBase
{
    public bool ApurarImpostoRenda { get; set; }
    public bool IncidirSobreJurosDeMora { get; set; }
    public bool CobrarDoReclamado { get; set; }
    public bool ConsiderarTributacaoExclusiva { get; set; }
    public bool RegimeDeCaixa { get; set; }
    public bool DeduzirContribuicaoSocial { get; set; }
    public bool AposentadoMaior65 { get; set; }
    public bool PossuiDependentes { get; set; }
    public int? QuantidadeDependentes { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<OcorrenciaDeIrpf> Ocorrencias { get; set; } = [];
}
