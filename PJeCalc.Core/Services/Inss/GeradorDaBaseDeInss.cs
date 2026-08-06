using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services.Inss;

/// <summary>Base do INSS de uma competência, separando verbas comuns das de 13º salário.</summary>
public sealed record BaseDeInssPorCompetencia(DateOnly Competencia, decimal BaseComum, decimal BaseDecimoTerceiro);

/// <summary>
/// Geração da base do INSS sobre as verbas, por competência: soma as diferenças para cálculo das
/// incidências (não corrigidas, zeradas quando negativas) das verbas com incidência de INSS,
/// separando as comuns das de 13º salário — como <c>calcularValorBaseVerbas</c> do original.
/// </summary>
public static class GeradorDaBaseDeInss
{
    /// <summary>
    /// Base das verbas numa competência: comuns quando <paramref name="decimoTerceiro"/> é falso,
    /// verbas de 13º quando verdadeiro.
    /// </summary>
    public static decimal BaseVerbasNoMes(
        IEnumerable<VerbaEmCalculo> verbasComIncidenciaDeInss, DateOnly competencia, bool decimoTerceiro)
    {
        ArgumentNullException.ThrowIfNull(verbasComIncidenciaDeInss);

        decimal soma = 0m;
        foreach (var verba in verbasComIncidenciaDeInss)
        {
            var ehDecimoTerceiro = verba.Caracteristica == CaracteristicaDaVerbaEnum.DecimoTerceiroSalario;
            if (ehDecimoTerceiro != decimoTerceiro)
                continue;

            foreach (var ocorrencia in verba.OcorrenciasAtivas)
            {
                if (!MesmoMes(ocorrencia.DataInicial, competencia))
                    continue;
                if (ocorrencia.DiferencaParaCalculoDasIncidencias() is { } diferenca)
                    soma += Math.Max(0m, diferenca);
            }
        }

        return soma;
    }

    /// <summary>Bases (comum e 13º) de cada competência da janela.</summary>
    public static IReadOnlyList<BaseDeInssPorCompetencia> Gerar(
        IEnumerable<VerbaEmCalculo> verbasComIncidenciaDeInss, DateOnly inicio, DateOnly fim)
    {
        var verbas = verbasComIncidenciaDeInss as IReadOnlyList<VerbaEmCalculo> ?? verbasComIncidenciaDeInss.ToList();
        return PeriodoDeApuracao.QuebrarEmMeses(inicio, fim)
            .Select(mes => PeriodoDeApuracao.Competencia(mes.Inicio))
            .Select(comp => new BaseDeInssPorCompetencia(
                comp,
                BaseVerbasNoMes(verbas, comp, decimoTerceiro: false),
                BaseVerbasNoMes(verbas, comp, decimoTerceiro: true)))
            .ToList();
    }

    private static bool MesmoMes(DateOnly a, DateOnly b) => a.Year == b.Year && a.Month == b.Month;
}
