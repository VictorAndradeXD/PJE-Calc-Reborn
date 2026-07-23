using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Prazo de férias em dias pela tabela do art. 130 da CLT, conforme as faltas NÃO
/// justificadas do período aquisitivo. Contratos em regime parcial com período
/// aquisitivo encerrado antes da reforma trabalhista (11/11/2017) usam a tabela do
/// art. 130-A, revogado.
/// </summary>
public static class PrazoDeFerias
{
    public static readonly DateOnly DataDaReformaTrabalhista = new(2017, 11, 11);

    public static int Calcular(DateOnly fimDoPeriodoAquisitivo, RegimeDoContratoEnum regime, int faltasNaoJustificadas)
    {
        if (regime == RegimeDoContratoEnum.Parcial && fimDoPeriodoAquisitivo < DataDaReformaTrabalhista)
            return faltasNaoJustificadas <= 7 ? 18 : 9;

        return faltasNaoJustificadas switch
        {
            <= 5 => 30,
            <= 14 => 24,
            <= 23 => 18,
            <= 32 => 12,
            _ => 0,
        };
    }
}
