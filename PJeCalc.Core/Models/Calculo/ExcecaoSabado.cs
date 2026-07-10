using PJeCalc.Core.Common;

namespace PJeCalc.Core.Models.Calculo;

public class ExcecaoSabado : EntityBase
{
    public DateTime DataInicio { get; set; }
    public DateTime DataTermino { get; set; }

    public long CalculoId { get; set; }
    public Calculo Calculo { get; set; } = null!;
}
