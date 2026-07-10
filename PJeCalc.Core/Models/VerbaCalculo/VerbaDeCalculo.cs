using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Models.VerbaCalculo;

public class VerbaDeCalculo : EntityBase
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public TipoDeVerbaEnum? Tipo { get; set; }
    public CaracteristicaDaVerbaEnum? Caracteristica { get; set; }
    public string? Discriminador { get; set; }
    public bool IncidenciaFGTS { get; set; }
    public bool IncidenciaINSS { get; set; }

    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
