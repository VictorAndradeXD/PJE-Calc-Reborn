namespace PJeCalc.Core.Models.Indices;

using PJeCalc.Core.Common;

public abstract class IndiceBase : EntityBase
{
    public DateTime Competencia { get; set; }
    public decimal Taxa { get; set; }
    public DateTime? DataCriacao { get; set; }
}
