namespace PJeCalc.Core.Models.Referencia;

/// <summary>
/// Tabela do salário-família no banco de referência (espelho de TBTABELASALARIOFAMILIA): duas
/// faixas de remuneração por competência, cada uma com o valor da cota por filho. Acima da
/// segunda faixa não há benefício.
/// </summary>
public class SalarioFamiliaReferencia
{
    public long Id { get; set; }
    public DateTime Competencia { get; set; }

    public decimal? FinalFaixa1 { get; set; }
    public decimal CotaFaixa1 { get; set; }
    public decimal? FinalFaixa2 { get; set; }
    public decimal CotaFaixa2 { get; set; }
}
