namespace PJeCalc.Core.Models.SalarioMinimo;

using PJeCalc.Core.Common;

public class SalarioMinimoNacional : EntityBase
{
    public DateTime Competencia { get; set; }
    public decimal Valor { get; set; }
}
