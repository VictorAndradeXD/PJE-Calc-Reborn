namespace PJeCalc.Core.Models.CartaoDePonto;

using PJeCalc.Core.Common;

public class CartaoDePonto : EntityBase
{
    public string? Nome { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<OcorrenciaDoCartaoDePonto> Ocorrencias { get; set; } = [];
}
