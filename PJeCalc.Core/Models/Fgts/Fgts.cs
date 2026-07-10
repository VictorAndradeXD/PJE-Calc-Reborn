namespace PJeCalc.Core.Models.Fgts;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class Fgts : EntityBase
{
    public DateTime? PeriodoInicial { get; set; }
    public DateTime? PeriodoFinal { get; set; }
    public DestinoDoFgtsEnum? Destino { get; set; }
    public AliquotaDoFgtsEnum? Aliquota { get; set; }
    public bool Multa { get; set; }
    public bool ExcluirAvisoDaMulta { get; set; }
    public TipoDeBaseDoFgtsEnum? TipoDoValorDaMulta { get; set; }
    public decimal? ValorInformadoDaMulta { get; set; }
    public ValorDaMultaDoFgtsEnum? MultaDoFgts { get; set; }
    public bool MultaDoArtigo467 { get; set; }
    public bool Multa10 { get; set; }
    public bool ContribuicaoSocial05 { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<OcorrenciaDeFgts> Ocorrencias { get; set; } = [];
    public List<OperacaoDeFgts> Operacoes { get; set; } = [];
}
