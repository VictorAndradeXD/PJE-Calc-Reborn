namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Dados do cálculo compartilhados por todas as verbas: datas do contrato, parâmetros de
/// projeção do aviso prévio, remunerações de referência, o índice de correção monetária e
/// os provedores de dias a excluir (férias gozadas e faltas), que nesta fase do backend
/// retornam zero por padrão.
/// </summary>
public sealed class ContextoDeVerbas
{
    public required DateOnly DataAdmissao { get; init; }
    public DateOnly? DataDemissao { get; init; }
    public DateOnly? DataDeLiquidacao { get; init; }

    /// <summary>Projeta o aviso prévio indenizado sobre a demissão (padrão do motor: sim).</summary>
    public bool ProjetaAvisoIndenizado { get; init; } = true;

    /// <summary>Limita a contagem de avos ao período da própria verba, em vez do ano/admissão.</summary>
    public bool LimitarAvosAoPeriodoDoCalculo { get; init; }

    /// <summary>
    /// Prazo do aviso prévio quando informado manualmente; quando nulo, é apurado pela
    /// Lei 12.506/2011 (30 dias + 3 por ano completo de contrato, teto de 90).
    /// </summary>
    public int? PrazoDoAvisoPrevioInformado { get; init; }

    public decimal? ValorUltimaRemuneracao { get; init; }
    public decimal? ValorMaiorRemuneracao { get; init; }

    /// <summary>Salário mínimo vigente na competência, quando alguma verba usa essa base.</summary>
    public Func<DateOnly, decimal>? SalarioMinimoNaCompetencia { get; init; }

    /// <summary>
    /// Índice acumulado de correção trabalhista da competência (data inicial da ocorrência)
    /// até a liquidação. Quando ausente, as ocorrências ficam com índice 1.
    /// </summary>
    public Func<DateOnly, decimal>? IndiceAcumulado { get; init; }

    /// <summary>
    /// Índice usado pela média de reflexo "pelo valor corrigido"; por padrão, o mesmo
    /// <see cref="IndiceAcumulado"/>.
    /// </summary>
    public Func<DateOnly, decimal>? IndiceParaMediaCorrigida { get; init; }

    public Func<PeriodoDeApuracao, int> DiasDeFeriasGozadas { get; init; } = _ => 0;
    public Func<PeriodoDeApuracao, int> FaltasJustificadas { get; init; } = _ => 0;
    public Func<PeriodoDeApuracao, int> FaltasNaoJustificadas { get; init; } = _ => 0;

    internal decimal ObterIndiceAcumulado(DateOnly dataInicial) =>
        IndiceAcumulado?.Invoke(dataInicial) ?? 1m;

    internal decimal ObterIndiceParaMediaCorrigida(DateOnly competencia) =>
        (IndiceParaMediaCorrigida ?? IndiceAcumulado)?.Invoke(competencia) ?? 1m;

    /// <summary>
    /// Dias do aviso prévio: informado, ou apurado por 30 + 3 × anos completos do contrato,
    /// com teto de 90 (Lei 12.506/2011).
    /// </summary>
    public int QuantidadeDeDiasDoAvisoPrevio()
    {
        if (PrazoDoAvisoPrevioInformado is { } informado)
            return informado;
        if (DataDemissao is not { } demissao)
            return 30;
        var dias = 30 + 3 * PeriodoDeApuracao.AnosCompletos(DataAdmissao, demissao);
        return Math.Min(dias, 90);
    }
}
