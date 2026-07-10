namespace PJeCalc.Core.Models.Multas;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public class Multa : EntityBase
{
    public string? Descricao { get; set; }
    public CredorDevedorMultaEnum? TipoCredorDevedor { get; set; }
    public string? NomeTerceiro { get; set; }
    public TipoValorEnum? TipoValor { get; set; }
    public decimal? Valor { get; set; }
    public decimal? ValorJuros { get; set; }
    public DateTime? DataVencimento { get; set; }
    public IndiceMonetarioEnum? IndiceDeCorrecao { get; set; }
    public bool AplicarJuros { get; set; }
    public DateTime? DataApartirJuros { get; set; }
    public decimal? Aliquota { get; set; }
    public BaseParaApuracaoDeMultaEnum? TipoBase { get; set; }
    public TipoOrigemRegistroEnum? OrigemRegistro { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
}
