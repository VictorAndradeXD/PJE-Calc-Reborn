namespace PJeCalc.Core.Models.Honorarios;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class Honorario : EntityBase
{
    public string? Descricao { get; set; }
    public TipoHonorarioEnum? TipoHonorario { get; set; }
    public string? NomeCredor { get; set; }
    public TipoDocumentoFiscalEnum? TipoDocumentoCredor { get; set; }
    public string? NumeroDocumentoCredor { get; set; }
    public bool ApurarIRRF { get; set; }
    public TipoValorEnum? TipoValor { get; set; }
    public decimal? Valor { get; set; }
    public decimal? ValorJuros { get; set; }
    public DateTime? DataVencimento { get; set; }
    public IndiceMonetarioEnum? IndiceDeCorrecao { get; set; }
    public bool AplicarJuros { get; set; }
    public DateTime? DataApartirJuros { get; set; }
    public decimal? Aliquota { get; set; }
    public BaseParaApuracaoDeHonorarioEnum? BaseHonorario { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
