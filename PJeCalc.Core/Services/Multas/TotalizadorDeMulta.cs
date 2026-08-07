using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Multas;

/// <summary>
/// Totais das multas. Os três primeiros baldes (por credor→devedor) formam o bruto; os dois
/// últimos são os que <b>descontam do crédito do reclamante</b> no líquido (reclamado→reclamante e
/// terceiro→reclamante marcados para descontar), como nas seções de crédito/débito do resumo.
/// </summary>
public sealed record TotaisDeMultas(
    decimal ReclamanteReclamado,
    decimal ReclamadoReclamante,
    decimal TerceiroReclamado,
    decimal ReclamadoReclamanteDescontar = 0m,
    decimal TerceiroReclamanteDescontar = 0m);

/// <summary>
/// Soma as multas do cálculo em três baldes conforme o credor/devedor (para o bruto) e apura, à
/// parte, o que desconta do crédito do reclamante. Como no original (<c>Total</c> com
/// arredondamento), <b>cada parcela é arredondada a 2 casas (HALF_EVEN) antes de somar</b>.
///
/// <para>No líquido, o crédito do reclamante soma as multas reclamante→reclamado e subtrai as
/// reclamado→reclamante marcadas para descontar; o débito subtrai as terceiro→reclamante marcadas
/// para descontar (resumo do cálculo).</para>
/// </summary>
public static class TotalizadorDeMulta
{
    public static TotaisDeMultas Calcular(
        IEnumerable<(CredorDevedorMultaEnum Credor, TipoCobrancaReclamanteEnum Cobranca, decimal ValorTotal)> multas)
    {
        ArgumentNullException.ThrowIfNull(multas);

        decimal reclamanteReclamado = 0m, reclamadoReclamante = 0m, terceiroReclamado = 0m;
        decimal reclamadoReclamanteDescontar = 0m, terceiroReclamanteDescontar = 0m;
        foreach (var (credor, cobranca, valorTotal) in multas)
        {
            var parcela = Math.Round(valorTotal, 2, MidpointRounding.ToEven);
            var descontar = cobranca == TipoCobrancaReclamanteEnum.DescontarCredito;
            switch (credor)
            {
                case CredorDevedorMultaEnum.ReclamanteReclamado:
                    reclamanteReclamado += parcela;
                    break;
                case CredorDevedorMultaEnum.ReclamadoReclamante:
                    reclamadoReclamante += parcela;
                    if (descontar)
                        reclamadoReclamanteDescontar += parcela;
                    break;
                case CredorDevedorMultaEnum.TerceiroReclamado:
                    terceiroReclamado += parcela;
                    break;
                case CredorDevedorMultaEnum.TerceiroReclamante:
                    if (descontar)
                        terceiroReclamanteDescontar += parcela;
                    break;
            }
        }

        return new TotaisDeMultas(
            reclamanteReclamado, reclamadoReclamante, terceiroReclamado,
            reclamadoReclamanteDescontar, terceiroReclamanteDescontar);
    }
}
