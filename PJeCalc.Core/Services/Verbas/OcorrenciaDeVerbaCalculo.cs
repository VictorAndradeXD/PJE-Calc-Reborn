namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Matemática de uma ocorrência (competência) de verba:
/// <c>devido = base ÷ divisor × multiplicador × quantidade</c>, dobrado quando é o caso e
/// arredondado a 2 casas; a diferença abate o pago (com piso zero opcional) e a corrigida
/// aplica o índice acumulado <b>sem arredondar</b> — como no motor oficial.
/// </summary>
public sealed record OcorrenciaDeVerbaCalculo
{
    public required decimal Base { get; init; }
    public decimal Divisor { get; init; } = 1m;
    public decimal Multiplicador { get; init; } = 1m;
    public decimal Quantidade { get; init; } = 1m;
    public bool Dobra { get; init; }
    public decimal Pago { get; init; }

    /// <summary>Fator de correção da competência até a liquidação.</summary>
    public decimal IndiceAcumulado { get; init; } = 1m;

    /// <summary>Quando verdadeiro, diferença negativa (pago maior que devido) vira zero.</summary>
    public bool ZeraValorNegativo { get; init; } = true;

    /// <summary>Valor devido da competência, arredondado a 2 casas (HALF_EVEN).</summary>
    public decimal Devido
    {
        get
        {
            var valor = Base / Divisor * Multiplicador * Quantidade;
            if (Dobra)
                valor *= 2m;
            return Math.Round(valor, 2, MidpointRounding.ToEven);
        }
    }

    /// <summary>Devido menos pago; zerada quando negativa e <see cref="ZeraValorNegativo"/>.</summary>
    public decimal Diferenca
    {
        get
        {
            var diferenca = Devido - Pago;
            return ZeraValorNegativo && diferenca < 0m ? 0m : diferenca;
        }
    }

    /// <summary>Diferença corrigida pelo índice acumulado (sem arredondamento).</summary>
    public decimal DiferencaCorrigida => IndiceAcumulado * Diferenca;
}
