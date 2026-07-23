using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Férias de um período aquisitivo do contrato: períodos aquisitivo/concessivo, prazo
/// (art. 130 CLT), situação, até três períodos de gozo (cada um com sua dobra) e abono
/// pecuniário. Alimenta a geração de ocorrências por período aquisitivo e os provedores
/// de dias a excluir.
/// </summary>
public sealed class FeriasDoCalculo
{
    public required PeriodoDeApuracao PeriodoAquisitivo { get; init; }
    public required PeriodoDeApuracao PeriodoConcessivo { get; init; }

    /// <summary>Dias de férias devidos no período (tabela do art. 130 CLT).</summary>
    public int Prazo { get; set; } = 30;

    public SituacaoDaFeriasEnum Situacao { get; set; } = SituacaoDaFeriasEnum.Gozadas;

    public bool DobraGeral { get; set; }
    public bool Abono { get; set; }
    public int QuantidadeDiasAbono { get; set; } = 10;

    public PeriodoDeApuracao? PeriodoDeGozo1 { get; set; }
    public bool DobraDoPeriodoDeGozo1 { get; set; }
    public PeriodoDeApuracao? PeriodoDeGozo2 { get; set; }
    public bool DobraDoPeriodoDeGozo2 { get; set; }
    public PeriodoDeApuracao? PeriodoDeGozo3 { get; set; }
    public bool DobraDoPeriodoDeGozo3 { get; set; }

    public IEnumerable<PeriodoDeApuracao> PeriodosDeGozo
    {
        get
        {
            if (PeriodoDeGozo1 is { } g1) yield return g1;
            if (PeriodoDeGozo2 is { } g2) yield return g2;
            if (PeriodoDeGozo3 is { } g3) yield return g3;
        }
    }

    /// <summary>Total de dias já gozados, mais os dias vendidos como abono.</summary>
    public int TotalDeDiasDeGozo =>
        PeriodosDeGozo.Sum(g => g.TotalDeDias) + (Abono ? QuantidadeDiasAbono : 0);

    /// <summary>Dias ainda devidos: prazo menos gozos (e abono quando há).</summary>
    public int DiasDevidos => Prazo - TotalDeDiasDeGozo;

    /// <summary>
    /// Fator do abono pecuniário: <c>prazo ÷ (prazo − dias de abono)</c> — a base do mês
    /// de gozo é inflada por ele (o abono é pago junto) e as incidências o retiram.
    /// </summary>
    public decimal FatorAbono => (decimal)Prazo / (Prazo - QuantidadeDiasAbono);
}
