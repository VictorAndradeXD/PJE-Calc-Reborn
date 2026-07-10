namespace PJeCalc.Core.Models.SalarioCategoria;

using PJeCalc.Core.Common;

public class SalarioCategoria : EntityBase
{
    public string? Nome { get; set; }
    public long? EstadoId { get; set; }
    public List<OcorrenciaDeSalarioCategoria> Ocorrencias { get; set; } = [];
}
