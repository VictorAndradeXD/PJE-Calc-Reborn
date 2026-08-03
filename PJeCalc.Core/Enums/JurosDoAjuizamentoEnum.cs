using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Marco inicial dos juros de mora de uma verba.
/// </summary>
public enum JurosDoAjuizamentoEnum
{
    /// <summary>Cada ocorrência começa a render juros no seu próprio vencimento (nunca antes do ajuizamento).</summary>
    [Description("Ocorrências vencidas")]
    OcorrenciasVencidas,

    /// <summary>Todas as ocorrências começam a render juros no ajuizamento.</summary>
    [Description("Ocorrências vencidas e vincendas")]
    OcorrenciasVencidasEVincendas
}
