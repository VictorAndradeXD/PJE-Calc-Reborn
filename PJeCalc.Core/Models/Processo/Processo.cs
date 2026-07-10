namespace PJeCalc.Core.Models.Processo;

using PJeCalc.Core.Common;

public class Processo : EntityBase
{
    public decimal? ValorDaCausa { get; set; }
    public DateTime? DataAutuacao { get; set; }
    public IdentificadorDoProcesso Identificador { get; set; } = new();
    public Reclamante Reclamante { get; set; } = new();
    public Reclamado Reclamado { get; set; } = new();
    public List<Advogado> AdvogadosReclamante { get; set; } = [];
    public List<Advogado> AdvogadosReclamado { get; set; } = [];
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
