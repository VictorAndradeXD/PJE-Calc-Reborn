namespace PJeCalc.Core.Models.Ferias;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class Ferias : EntityBase
{
    public string? Relativa { get; set; }
    public DateTime? DataInicialPeriodoAquisitivo { get; set; }
    public DateTime? DataFinalPeriodoAquisitivo { get; set; }
    public DateTime? DataInicialPeriodoConcessivo { get; set; }
    public DateTime? DataFinalPeriodoConcessivo { get; set; }
    public int Prazo { get; set; } = 30;
    public SituacaoDaFeriasEnum? Situacao { get; set; }
    public bool Dobra { get; set; }
    public bool Abono { get; set; }
    public int? DiasDeAbono { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
