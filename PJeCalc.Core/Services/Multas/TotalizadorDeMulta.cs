using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Multas;

/// <summary>Totais das multas por direção credor→devedor.</summary>
public sealed record TotaisDeMultas(
    decimal ReclamanteReclamado,
    decimal ReclamadoReclamante,
    decimal TerceiroReclamado);

/// <summary>
/// Soma as multas do cálculo em três baldes conforme o credor/devedor. Como no original
/// (<c>Total</c> com arredondamento), <b>cada parcela é arredondada a 2 casas (HALF_EVEN)
/// antes de somar</b>. Multas terceiro→reclamante não entram (são débito do reclamante em
/// outro fluxo).
/// </summary>
public static class TotalizadorDeMulta
{
    public static TotaisDeMultas Calcular(IEnumerable<(CredorDevedorMultaEnum Credor, decimal ValorTotal)> multas)
    {
        ArgumentNullException.ThrowIfNull(multas);

        decimal reclamanteReclamado = 0m, reclamadoReclamante = 0m, terceiroReclamado = 0m;
        foreach (var (credor, valorTotal) in multas)
        {
            var parcela = Math.Round(valorTotal, 2, MidpointRounding.ToEven);
            switch (credor)
            {
                case CredorDevedorMultaEnum.ReclamanteReclamado:
                    reclamanteReclamado += parcela;
                    break;
                case CredorDevedorMultaEnum.ReclamadoReclamante:
                    reclamadoReclamante += parcela;
                    break;
                case CredorDevedorMultaEnum.TerceiroReclamado:
                    terceiroReclamado += parcela;
                    break;
            }
        }

        return new TotaisDeMultas(reclamanteReclamado, reclamadoReclamante, terceiroReclamado);
    }
}
