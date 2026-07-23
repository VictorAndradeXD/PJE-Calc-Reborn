using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Honorarios;

/// <summary>Totais dos honorários devidos por cada parte.</summary>
public sealed record TotaisDeHonorarios(decimal DevidoPeloReclamante, decimal DevidoPeloReclamado);

/// <summary>
/// Soma os honorários do cálculo. Como no original, cada parcela é arredondada a 2 casas
/// (HALF_EVEN) antes de somar. O honorário devido pelo reclamante marcado para <b>cobrar</b>
/// não reduz o crédito (acumula zero); marcado para <b>descontar</b>, acumula o valor total.
/// </summary>
public static class TotalizadorDeHonorario
{
    public static TotaisDeHonorarios Calcular(
        IEnumerable<(TipoDeDevedorDoHonorarioEnum Devedor, TipoCobrancaReclamanteEnum Cobranca, decimal ValorTotal)> honorarios)
    {
        ArgumentNullException.ThrowIfNull(honorarios);

        decimal devidoPeloReclamante = 0m, devidoPeloReclamado = 0m;
        foreach (var (devedor, cobranca, valorTotal) in honorarios)
        {
            var parcela = Math.Round(valorTotal, 2, MidpointRounding.ToEven);
            if (devedor == TipoDeDevedorDoHonorarioEnum.Reclamante)
                devidoPeloReclamante += cobranca == TipoCobrancaReclamanteEnum.Cobrar ? 0m : parcela;
            else
                devidoPeloReclamado += parcela;
        }

        return new TotaisDeHonorarios(devidoPeloReclamante, devidoPeloReclamado);
    }
}
