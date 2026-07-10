namespace PJeCalc.Core.Models.HistoricoSalarial;

using PJeCalc.Core.Common;

public class HistoricoSalarial : EntityBase
{
    public string? Nome { get; set; }
    public bool IncidenciaFGTS { get; set; }
    public bool AplicarProporcionalidadeFGTS { get; set; }
    public bool IncidenciaINSS { get; set; }
    public bool AplicarProporcionalidadeINSS { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<OcorrenciaDoHistoricoSalarial> Ocorrencias { get; set; } = [];
}
