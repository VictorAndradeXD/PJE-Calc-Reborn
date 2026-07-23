using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Uma competência gerada de uma verba, com os termos resolvidos e os valores integrais
/// ("mensalizados"). A diferença abate o pago (zerada quando negativa e a verba zera
/// valor negativo) e a corrigida aplica o índice acumulado sem arredondar, como no motor
/// oficial.
/// </summary>
public sealed class OcorrenciaDaVerba
{
    public required VerbaEmCalculo Verba { get; init; }
    public required DateOnly DataInicial { get; init; }
    public required DateOnly DataFinal { get; init; }

    public bool Ativo { get; set; } = true;
    public TipoValorEnum Valor { get; set; }

    public decimal? Base { get; set; }
    public decimal? BaseIntegral { get; set; }
    public decimal? Divisor { get; set; }
    public decimal? Multiplicador { get; set; }
    public decimal? Quantidade { get; set; }
    public decimal? Devido { get; set; }
    public decimal? Pago { get; set; }
    public bool Dobra { get; set; }

    /// <summary>Fator de correção da competência até a liquidação (definido ao liquidar).</summary>
    public decimal? IndiceAcumulado { get; set; }

    public bool FeriasIndenizadas { get; set; }
    public bool FeriasComAbono { get; set; }

    public DateOnly? InicioDoPeriodoAquisitivo { get; set; }
    public DateOnly? FimDoPeriodoAquisitivo { get; set; }

    public PeriodoDeApuracao? PeriodoAquisitivo =>
        InicioDoPeriodoAquisitivo is { } inicio && FimDoPeriodoAquisitivo is { } fim
            ? new PeriodoDeApuracao(inicio, fim)
            : null;

    /// <summary>
    /// Fator do abono pecuniário aplicado à base na liquidação (prazo ÷ (prazo − dias de
    /// abono); 1,5 quando o período aquisitivo não casa com férias cadastradas).
    /// As incidências dividem por ele para retirar o abono.
    /// </summary>
    public decimal FatorAbono { get; internal set; } = 1.5m;

    private decimal? _quantidadeIntegral;
    private decimal? _devidoIntegral;
    private decimal? _pagoIntegral;

    /// <summary>
    /// Integralizador da competência (valor × D ÷ dias, com as exclusões da verba),
    /// usado pelo recálculo tardio dos valores integrais — mesma semântica do motor
    /// original, que reintegraliza quando o integral está nulo ou zerado com valor ≠ 0.
    /// </summary>
    internal Func<decimal, decimal>? Integralizador { get; set; }

    public decimal? QuantidadeIntegral
    {
        get => IntegralComRecalculo(ref _quantidadeIntegral, Quantidade);
        set => _quantidadeIntegral = value;
    }

    public decimal? DevidoIntegral
    {
        get => IntegralComRecalculo(ref _devidoIntegral, Devido);
        set => _devidoIntegral = value;
    }

    public decimal? PagoIntegral
    {
        get => IntegralComRecalculo(ref _pagoIntegral, Pago);
        set => _pagoIntegral = value;
    }

    private decimal? IntegralComRecalculo(ref decimal? integral, decimal? origem)
    {
        if (Integralizador is not null && origem is { } valor &&
            (integral is null || (integral == 0m && valor != 0m)))
        {
            integral = Integralizador(valor);
        }
        return integral;
    }

    public decimal Diferenca => CalcularDiferenca(Devido, Pago);

    public decimal DiferencaIntegral => CalcularDiferenca(DevidoIntegral, PagoIntegral);

    private decimal CalcularDiferenca(decimal? devido, decimal? pago)
    {
        if (!Ativo)
            return 0m;
        var diferenca = devido is not null && pago is not null ? devido.Value - pago.Value : 0m;
        return diferenca < 0m && Verba.ZeraValorNegativo ? 0m : diferenca;
    }

    /// <summary>Diferença × índice acumulado, sem arredondar; nula antes da liquidação.</summary>
    public decimal? DiferencaCorrigida => IndiceAcumulado is { } indice ? indice * Diferenca : null;

    /// <summary>
    /// Diferença (corrigida ou não) que alimenta as incidências (FGTS/INSS/IRPF):
    /// nula para férias indenizadas; férias em dobro contam metade; o abono pecuniário é
    /// retirado (÷ <see cref="FatorAbono"/>). Arredondada a 2 casas.
    /// </summary>
    public decimal? DiferencaParaCalculoDasIncidencias(bool corrigida = false)
    {
        if (FeriasIndenizadas)
            return null;
        var valor = corrigida ? DiferencaCorrigida ?? 0m : Diferenca;
        if (Verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias && Dobra)
            valor *= 0.5m;
        if (FeriasComAbono && Verba.TipoValor == TipoValorEnum.Calculado)
            valor /= FatorAbono;
        return Math.Round(valor, 2, MidpointRounding.ToEven);
    }
}
