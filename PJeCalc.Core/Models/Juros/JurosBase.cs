namespace PJeCalc.Core.Models.Juros;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public abstract class JurosBase : EntityBase
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal Aliquota { get; set; }
    public TipoDeJurosEnum? TipoDeJuros { get; set; }
}
