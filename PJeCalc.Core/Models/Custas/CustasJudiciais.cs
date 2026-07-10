namespace PJeCalc.Core.Models.Custas;

using PJeCalc.Core.Common;

public class CustasJudiciais : EntityBase
{
    public decimal? ValorConhecimentoReclamante { get; set; }
    public decimal? ValorConhecimentoReclamado { get; set; }
    public DateTime? DataVencimentoReclamante { get; set; }
    public DateTime? DataVencimentoReclamado { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<Armazenamento> Armazenamentos { get; set; } = [];
    public List<AutoJudicial> AutosJudiciais { get; set; } = [];
    public List<CustaPaga> CustasPagas { get; set; } = [];
}
