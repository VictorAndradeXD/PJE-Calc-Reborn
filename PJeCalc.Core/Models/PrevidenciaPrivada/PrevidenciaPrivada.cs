namespace PJeCalc.Core.Models.PrevidenciaPrivada;

using PJeCalc.Core.Common;

public class PrevidenciaPrivada : EntityBase
{
    public bool ApurarPrevidenciaPrivada { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<AliquotaDePrevidenciaPrivada> Aliquotas { get; set; } = [];
    public List<OcorrenciaDePrevidenciaPrivada> Ocorrencias { get; set; } = [];
}
