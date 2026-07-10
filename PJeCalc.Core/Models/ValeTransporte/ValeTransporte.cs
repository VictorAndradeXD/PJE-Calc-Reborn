namespace PJeCalc.Core.Models.ValeTransporte;

using PJeCalc.Core.Common;

public class ValeTransporte : EntityBase
{
    public string? DescricaoLinha { get; set; }
    public string? TipoDeLinha { get; set; }
    public DateTime? DataEncerramento { get; set; }
    public long? EstadoId { get; set; }
    public long? MunicipioId { get; set; }
    public List<ValorValeTransporte> Ocorrencias { get; set; } = [];
}
