namespace PJeCalc.Core.Models.Faltas;

using PJeCalc.Core.Common;

public class Falta : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataTermino { get; set; }
    public bool FaltaJustificada { get; set; }
    public string? Justificativa { get; set; }
    public bool ReiniciarFerias { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
