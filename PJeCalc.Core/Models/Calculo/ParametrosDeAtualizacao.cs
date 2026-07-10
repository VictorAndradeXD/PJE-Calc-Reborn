using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Models.Calculo;

public class ParametrosDeAtualizacao : EntityBase
{
    public IndiceMonetarioEnum? IndiceTrabalhista { get; set; }
    public IndiceMonetarioEnum? OutroIndiceTrabalhista { get; set; }
    public bool CombinarOutroIndice { get; set; }
    public DateTime? ApartirDeOutroIndice { get; set; }
    public bool IgnorarTaxaNegativa { get; set; }

    public JurosEnum? Juros { get; set; }
    public bool AplicarJurosFasePreJudicial { get; set; }
    public bool CombinarOutroJuros { get; set; }
    public bool EntePublico { get; set; }

    public IndiceMonetarioEnum? IndiceDeCorrecaoDoFGTS { get; set; }
    public bool JurosDeFgtsComJam { get; set; }
    public IndiceMonetarioEnum? IndiceDeCorrecaoDePrevidenciaPrivada { get; set; }
    public JurosEnum? JurosDePrevidenciaPrivada { get; set; }
    public IndiceMonetarioEnum? IndiceDeCorrecaoDasCustas { get; set; }
    public JurosEnum? JurosDeCustas { get; set; }

    public long CalculoId { get; set; }
    public Calculo? Calculo { get; set; }
}
