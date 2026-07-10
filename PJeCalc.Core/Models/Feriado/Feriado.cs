namespace PJeCalc.Core.Models.Feriado;

using PJeCalc.Core.Common;

public class Feriado : EntityBase
{
    public string? Tipo { get; set; }
    public string? Abrangencia { get; set; }
    public string? NomeFeriado { get; set; }
    public DateTime? Data { get; set; }
    public string? DescricaoLegislacao { get; set; }
    public DateTime? InicioVigencia { get; set; }
    public DateTime? FimVigencia { get; set; }
    public bool Movel { get; set; }
    public string? Uid { get; set; }
    public long? EstadoId { get; set; }
    public long? MunicipioId { get; set; }
    public List<ExcecaoDoFeriado> Excecoes { get; set; } = [];
}
