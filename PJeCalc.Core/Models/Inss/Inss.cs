namespace PJeCalc.Core.Models.Inss;

using PJeCalc.Core.Common;

public class Inss : EntityBase
{
    public string? TipoAliquotaSegurado { get; set; }
    public decimal? AliquotaSeguradoFixa { get; set; }
    public bool LimitarTeto { get; set; }
    public string? TipoAliquotaEmpregador { get; set; }
    public decimal? AliquotaEmpresaFixa { get; set; }
    public decimal? AliquotaRATFixa { get; set; }
    public decimal? AliquotaTerceirosFixa { get; set; }
    public bool ApurarInssSobreSalariosPagos { get; set; }
    public long CalculoId { get; set; }
    public Calculo.Calculo? Calculo { get; set; }
    public List<AliquotaEmpregadorPorPeriodo> AliquotasPorPeriodos { get; set; } = [];
}
