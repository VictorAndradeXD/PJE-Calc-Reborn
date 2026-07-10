namespace PJeCalc.Core.Models.SalarioFamilia;

using PJeCalc.Core.Common;

public class SalarioFamilia : EntityBase
{
    public bool ApurarSalarioFamilia { get; set; }
    public int? QuantFilhosMenores14 { get; set; }
    public DateTime? DataInicial { get; set; }
    public DateTime? DataFinal { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<OcorrenciaDeSalarioFamilia> Ocorrencias { get; set; } = [];
}
