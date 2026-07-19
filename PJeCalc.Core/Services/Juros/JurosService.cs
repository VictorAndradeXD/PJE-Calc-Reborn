namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Cálculo de juros de mora.
///
/// <para>Escopo atual (walking skeleton): matemática por período (<see cref="PeriodoDeJuros"/>)
/// — contagem de meses e taxa acumulada de um trecho — e aplicação dos juros sobre o
/// capital (valor corrigido líquido de contribuições).</para>
///
/// <para>Ainda NÃO cobre — próximos incrementos, validados por golden do motor Java:
/// montagem da cadeia de períodos a partir das faixas do banco (JurosPadrão, Fazenda,
/// SELIC, TRD, Taxa Legal), regra de início dos juros (mês seguinte ao vencimento, nunca
/// antes do ajuizamento salvo fase pré-judicial), combinações de tabela e corte na
/// liquidação.</para>
/// </summary>
public sealed class JurosService
{
    /// <summary>
    /// Base líquida sobre a qual os juros incidem: valor corrigido menos a contribuição
    /// social (INSS) e a previdência privada, que não rendem juros de mora.
    /// </summary>
    public static decimal CalcularCapital(
        decimal valorCorrigido, decimal contribuicaoSocial = 0m, decimal previdenciaPrivada = 0m) =>
        valorCorrigido - contribuicaoSocial - previdenciaPrivada;

    /// <summary>
    /// Juros de mora sobre o capital, dada a taxa acumulada em pontos percentuais.
    /// Arredonda a 2 casas (HALF_EVEN), como o valor monetário do PJe-Calc.
    /// </summary>
    public static decimal CalcularJuros(decimal capital, decimal taxaPercentual) =>
        Math.Round(capital * taxaPercentual / 100m, 2, MidpointRounding.ToEven);

    /// <summary>Taxa acumulada (em pontos percentuais) de um único período.</summary>
    public static decimal CalcularTaxa(PeriodoDeJuros periodo)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        return periodo.Taxa;
    }
}
