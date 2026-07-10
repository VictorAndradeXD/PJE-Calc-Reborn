using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Indices acumulados para correcao monetaria.
/// </summary>
public enum IndicesAcumuladosEnum
{
    [Description("A partir do mês subsequente ao vencimento das verbas")]
    MesSubsequenteAoVencimento,

    [Description("A partir do mês de vencimento das verbas")]
    MesDoVencimento,

    [Description("A partir do mês subsequente ao vencimento das verbas mensais e a partir do mês de vencimento das verbas anuais e rescisórias")]
    MesSubsequenteEMesDoVencimento,

    [Description("Para uso nas atualizações de cálculo")]
    AtualizacaoCalculo
}
