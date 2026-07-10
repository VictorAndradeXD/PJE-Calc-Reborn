namespace PJeCalc.Core.Models.SalarioCategoria;

using PJeCalc.Core.Common;

public class OcorrenciaDeSalarioCategoria : EntityBase
{
    public DateTime? DataOcorrencia { get; set; }
    public decimal? Valor { get; set; }
    public long SalarioCategoriaId { get; set; }
    public SalarioCategoria? SalarioCategoria { get; set; }
}
