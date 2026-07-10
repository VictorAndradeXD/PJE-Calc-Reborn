namespace PJeCalc.Core.Models.HistoricoSalarial;

using PJeCalc.Core.Common;

public class OcorrenciaDoHistoricoSalarial : EntityBase
{
    public DateTime? DataOcorrencia { get; set; }
    public decimal? Valor { get; set; }
    public bool RecolhidoFGTS { get; set; }
    public bool RecolhidoINSS { get; set; }
    public bool IncidenciaFGTS { get; set; }
    public bool IncidenciaINSS { get; set; }
    public long HistoricoSalarialId { get; set; }
    public HistoricoSalarial? HistoricoSalarial { get; set; }
}
