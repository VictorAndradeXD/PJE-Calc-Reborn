using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Dados do cálculo compartilhados por todas as verbas: datas do contrato, prescrição,
/// parâmetros do aviso prévio, remunerações de referência, o índice de correção
/// monetária e as coleções de férias e faltas — das quais derivam os dias a excluir
/// (por interseção de períodos, como no motor original).
/// </summary>
public sealed class ContextoDeVerbas
{
    public required DateOnly DataAdmissao { get; init; }
    public DateOnly? DataDemissao { get; init; }
    public DateOnly? DataAjuizamento { get; init; }
    public DateOnly? DataTerminoCalculo { get; init; }
    public DateOnly? DataDeLiquidacao { get; init; }

    public RegimeDoContratoEnum RegimeDoContrato { get; init; } = RegimeDoContratoEnum.Integral;

    public bool PrescricaoQuinquenal { get; init; }

    /// <summary>Data de prescrição quinquenal: ajuizamento − 5 anos.</summary>
    public DateOnly? DataPrescricaoQuinquenal => DataAjuizamento?.AddYears(-5);

    /// <summary>Projeta o aviso prévio indenizado sobre a demissão (padrão do motor: sim).</summary>
    public bool ProjetaAvisoIndenizado { get; init; } = true;

    /// <summary>Limita a contagem de avos ao período da própria verba, em vez do ano/admissão.</summary>
    public bool LimitarAvosAoPeriodoDoCalculo { get; init; }

    /// <summary>
    /// Prazo do aviso prévio quando informado manualmente; quando nulo, é apurado pela
    /// Lei 12.506/2011 (30 dias + 3 por ano completo de contrato, teto de 90).
    /// </summary>
    public int? PrazoDoAvisoPrevioInformado { get; init; }

    /// <summary>Override manual do prazo das férias proporcionais (art. 130 quando nulo).</summary>
    public int? PrazoFeriasProporcional { get; init; }

    public DateOnly? InicioFeriasColetivas { get; init; }

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

    public List<FeriasDoCalculo> ListaDeFerias { get; init; } = [];
    public List<FaltaDoCalculo> Faltas { get; init; } = [];

    // ------------------------------------------------------------------
    // Provedores de dias a excluir (interseção inclusiva de períodos)
    // ------------------------------------------------------------------

    /// <summary>Dias de gozo de férias dentro do período.</summary>
    public int ObterDiasFerias(PeriodoDeApuracao periodo) =>
        ListaDeFerias.SelectMany(f => f.PeriodosDeGozo).Sum(g => periodo.DiasCoincidentesCom(g));

    public int ObterFaltasJustificadas(PeriodoDeApuracao periodo) =>
        Faltas.Where(f => f.Justificada).Sum(f => periodo.DiasCoincidentesCom(f.Periodo));

    public int ObterFaltasNaoJustificadas(PeriodoDeApuracao periodo) =>
        Faltas.Where(f => !f.Justificada).Sum(f => periodo.DiasCoincidentesCom(f.Periodo));

    /// <summary>
    /// Prazo das férias proporcionais do período aquisitivo: override manual ou tabela
    /// do art. 130 com as faltas não justificadas do próprio período.
    /// </summary>
    public int PrazoDasFeriasProporcionais(PeriodoDeApuracao periodoAquisitivo) =>
        PrazoFeriasProporcional ?? PrazoDeFerias.Calcular(
            periodoAquisitivo.Fim, RegimeDoContrato, ObterFaltasNaoJustificadas(periodoAquisitivo));

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

    /// <summary>Demissão projetada com o aviso prévio indenizado (quando configurado).</summary>
    public DateOnly? DataDemissaoProjetada =>
        DataDemissao is { } demissao
            ? (ProjetaAvisoIndenizado ? demissao.AddDays(QuantidadeDeDiasDoAvisoPrevio()) : demissao)
            : null;
}
