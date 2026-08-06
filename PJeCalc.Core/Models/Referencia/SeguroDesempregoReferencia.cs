namespace PJeCalc.Core.Models.Referencia;

/// <summary>
/// Tabela do seguro-desemprego no banco de referência (espelho de TBTABELASEGURODESEMPREGO):
/// por competência, o teto da primeira faixa e seu percentual, o percentual e a parcela fixa da
/// segunda faixa, e o piso/teto do benefício.
/// </summary>
public class SeguroDesempregoReferencia
{
    public long Id { get; set; }
    public DateTime Competencia { get; set; }

    public decimal? FinalFaixa1 { get; set; }
    public decimal PercentualFaixa1 { get; set; }
    public decimal? PercentualFaixa2 { get; set; }
    public decimal? SomaFaixa2 { get; set; }
    public decimal Piso { get; set; }
    public decimal Teto { get; set; }
}
