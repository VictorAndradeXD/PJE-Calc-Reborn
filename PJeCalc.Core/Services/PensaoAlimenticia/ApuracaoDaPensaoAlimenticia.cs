namespace PJeCalc.Core.Services.PensaoAlimenticia;

/// <summary>
/// Uma verba com incidência de pensão alimentícia, com os totais já liquidados que a base da
/// pensão consome.
/// </summary>
public sealed record VerbaParaPensao
{
    /// <summary>Diferença corrigida da verba (base geral).</summary>
    public decimal DiferencaCorrigida { get; init; }

    /// <summary>Diferença corrigida apenas das férias gozadas (base quando a verba é de férias).</summary>
    public decimal DiferencaCorrigidaDeFeriasGozadas { get; init; }

    /// <summary>Juros de mora da verba (entram na base quando a pensão incide sobre juros).</summary>
    public decimal Juros { get; init; }

    public bool Ferias { get; init; }
    public bool IncidenciaIrpf { get; init; }
}

/// <summary>Contexto da apuração da pensão alimentícia.</summary>
public sealed record ContextoDaPensao
{
    /// <summary>Alíquota da pensão, em pontos percentuais.</summary>
    public required decimal Aliquota { get; init; }

    /// <summary>Se a pensão incide também sobre os juros de mora.</summary>
    public bool IncidirSobreJuros { get; init; }

    /// <summary>Verbas com incidência de pensão (já filtradas).</summary>
    public IReadOnlyList<VerbaParaPensao> Verbas { get; init; } = [];

    /// <summary>Base do FGTS (já resolvida pelo módulo do FGTS conforme incidência/juros/dedução).</summary>
    public decimal BaseFgts { get; init; }

    /// <summary>Base da multa do FGTS (já resolvida pelo módulo do FGTS).</summary>
    public decimal BaseMultaFgts { get; init; }
}

/// <summary>Resultado da apuração da pensão alimentícia.</summary>
public sealed record ResultadoDaPensao(
    decimal BaseVerbas,
    decimal BaseVerbasTributaveis,
    decimal BaseFgts,
    decimal BaseMultaFgts,
    decimal TotalDasBases,
    decimal ValorDevido);

/// <summary>
/// Apuração da pensão alimentícia: monta a base sobre as verbas com incidência de pensão (a
/// diferença corrigida — as férias entram só pela parcela gozada; os juros, quando configurado,
/// proporcionalmente às férias gozadas), soma a base do FGTS e da sua multa, e aplica a alíquota
/// sobre o total. O valor devido desconta do crédito do reclamante.
/// </summary>
public static class ApuracaoDaPensaoAlimenticia
{
    public static ResultadoDaPensao Apurar(ContextoDaPensao contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        decimal baseVerbas = 0m, baseVerbasTributaveis = 0m;
        foreach (var verba in contexto.Verbas)
        {
            var valorBase = verba.Ferias ? verba.DiferencaCorrigidaDeFeriasGozadas : verba.DiferencaCorrigida;

            if (contexto.IncidirSobreJuros)
            {
                if (verba.Ferias)
                {
                    var proporcao = verba.DiferencaCorrigida == 0m
                        ? 0m
                        : verba.DiferencaCorrigidaDeFeriasGozadas / verba.DiferencaCorrigida;
                    valorBase += Arredondar(verba.Juros * proporcao);
                }
                else
                {
                    valorBase += verba.Juros;
                }
            }

            baseVerbas += valorBase;
            if (verba.IncidenciaIrpf)
                baseVerbasTributaveis += valorBase;
        }

        var totalDasBases = baseVerbas + contexto.BaseFgts + contexto.BaseMultaFgts;
        var valorDevido = totalDasBases * (contexto.Aliquota / 100m);

        return new ResultadoDaPensao(
            baseVerbas, baseVerbasTributaveis, contexto.BaseFgts, contexto.BaseMultaFgts, totalDasBases, valorDevido);
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
