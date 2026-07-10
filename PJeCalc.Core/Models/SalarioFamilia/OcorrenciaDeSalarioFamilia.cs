namespace PJeCalc.Core.Models.SalarioFamilia;

using PJeCalc.Core.Common;

public class OcorrenciaDeSalarioFamilia : EntityBase
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int? QuantidadeFilhos { get; set; }
    public decimal? ValorRemuneracaoMensal { get; set; }
    public decimal? ValorSalarioFamilia { get; set; }
    public decimal? IndiceAcumulado { get; set; }
    public decimal? TaxaDeJuros { get; set; }
    public long SalarioFamiliaId { get; set; }
    public SalarioFamilia? SalarioFamilia { get; set; }
}
