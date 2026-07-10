using PJeCalc.Core.Common;

namespace PJeCalc.Core.Models.Calculo;

public class ExcecaoCargaHoraria : EntityBase
{
    public DateTime DataInicio { get; set; }
    public DateTime DataTermino { get; set; }
    public decimal ValorCargaHoraria { get; set; }

    public long CalculoId { get; set; }
    public Calculo Calculo { get; set; } = null!;
}
