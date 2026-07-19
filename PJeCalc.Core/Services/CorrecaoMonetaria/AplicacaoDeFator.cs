namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Aplicação de um fator acumulado a um valor monetário, na convenção do PJe-Calc.
/// Compartilhada pelos módulos que corrigem valores (correção monetária, FGTS...).
/// </summary>
public static class AplicacaoDeFator
{
    /// <summary>
    /// Aplica o fator ao valor, arredondando a 2 casas (HALF_EVEN, como
    /// <c>Utils.arredondarValorMonetario</c> do original). Fator negativo divide por
    /// -fator — convenção interna para deflacionar/tratar ausência de correção.
    /// </summary>
    public static decimal Aplicar(decimal valor, decimal fator)
    {
        var bruto = fator >= 0m ? valor * fator : valor / -fator;
        return Math.Round(bruto, 2, MidpointRounding.ToEven);
    }
}
