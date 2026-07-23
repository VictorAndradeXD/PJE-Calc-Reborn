namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Falta do trabalhador em um período (início/término inclusivos). A quantidade de dias
/// é sempre apurada por interseção com o período consultado. Faltas não justificadas
/// reduzem o prazo de férias (art. 130 CLT); a marcada com
/// <see cref="ReiniciaFerias"/> zera a contagem do período aquisitivo.
/// </summary>
public sealed record FaltaDoCalculo(
    DateOnly Inicio,
    DateOnly Fim,
    bool Justificada = false,
    bool ReiniciaFerias = false)
{
    public PeriodoDeApuracao Periodo => new(Inicio, Fim);
}
