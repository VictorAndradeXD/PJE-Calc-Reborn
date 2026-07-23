using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Totais agregados de uma verba sobre as ocorrências ativas. Como no motor oficial,
/// CADA parcela é arredondada a 2 casas (HALF_EVEN) antes de somar — o total é a soma
/// dos centavos por ocorrência, não o arredondamento do total.
/// </summary>
public sealed record TotaisDaVerba(
    decimal Devido,
    decimal Pago,
    decimal Diferenca,
    decimal DiferencaCorrigida,
    decimal DiferencaCorrigidaParaCalculoDasIncidencias,
    decimal DiferencaCorrigidaDeFeriasGozadas)
{
    public static TotaisDaVerba Calcular(VerbaEmCalculo verba)
    {
        decimal devido = 0m, pago = 0m, diferenca = 0m, corrigida = 0m, incidencias = 0m, feriasGozadas = 0m;

        foreach (var ocorrencia in verba.OcorrenciasAtivas)
        {
            devido += ArredondarParcela(ocorrencia.Devido);
            pago += ArredondarParcela(ocorrencia.Pago);
            diferenca += ArredondarParcela(ocorrencia.Diferenca);
            corrigida += ArredondarParcela(ocorrencia.DiferencaCorrigida);

            var baseIncidencias = ocorrencia.DiferencaParaCalculoDasIncidencias(corrigida: true);
            if (baseIncidencias is null)
                continue; // férias indenizadas ficam fora das incidências
            incidencias += ArredondarParcela(baseIncidencias);
            if (verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias && !ocorrencia.FeriasIndenizadas)
                feriasGozadas += ArredondarParcela(baseIncidencias);
        }

        return new TotaisDaVerba(devido, pago, diferenca, corrigida, incidencias, feriasGozadas);
    }

    private static decimal ArredondarParcela(decimal? valor) =>
        valor is { } v ? Math.Round(v, 2, MidpointRounding.ToEven) : 0m;
}
