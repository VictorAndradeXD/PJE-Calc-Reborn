namespace PJeCalc.Core.Models.PensaoAlimenticia;

using PJeCalc.Core.Common;

public class PensaoAlimenticia : EntityBase
{
    public bool ApurarPensaoAlimenticia { get; set; }
    public decimal? Aliquota { get; set; }
    public bool IncidirSobreJuros { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
