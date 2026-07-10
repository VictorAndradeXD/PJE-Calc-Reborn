namespace PJeCalc.Core.Models.PrevidenciaPrivada;

using PJeCalc.Core.Common;

public class AliquotaDePrevidenciaPrivada : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataTermino { get; set; }
    public decimal? Aliquota { get; set; }
    public long PrevidenciaPrivadaId { get; set; }
    public PrevidenciaPrivada? PrevidenciaPrivada { get; set; }
}
