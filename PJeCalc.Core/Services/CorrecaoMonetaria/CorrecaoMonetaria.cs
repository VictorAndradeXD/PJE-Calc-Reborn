using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.CorrecaoMonetaria;

/// <summary>
/// Parâmetros para corrigir um valor monetário entre o vencimento e a liquidação.
/// </summary>
public sealed record PedidoDeCorrecao
{
    /// <summary>Valor original a ser corrigido.</summary>
    public required decimal Valor { get; init; }

    /// <summary>Data em que o valor se tornou devido (vencimento).</summary>
    public required DateOnly DataVencimento { get; init; }

    /// <summary>Data até a qual o valor será atualizado (liquidação).</summary>
    public required DateOnly DataLiquidacao { get; init; }

    /// <summary>Índice de correção a aplicar.</summary>
    public required IndiceMonetarioEnum Indice { get; init; }

    /// <summary>Regime que define a competência inicial da correção a partir do vencimento.</summary>
    public IndicesAcumuladosEnum Regime { get; init; } = IndicesAcumuladosEnum.MesSubsequenteAoVencimento;

    /// <summary>
    /// Quando verdadeiro, meses com taxa negativa não deflacionam o valor
    /// (contribuem com fator 1). Não se aplica à família SELIC.
    /// </summary>
    public bool IgnorarTaxaNegativa { get; init; }
}

/// <summary>
/// Resultado da correção monetária de um valor.
/// </summary>
public sealed record ResultadoDaCorrecao
{
    /// <summary>Valor informado, antes da correção.</summary>
    public required decimal ValorOriginal { get; init; }

    /// <summary>Valor corrigido, arredondado a 2 casas.</summary>
    public required decimal ValorCorrigido { get; init; }

    /// <summary>Fator acumulado aplicado ao valor original.</summary>
    public required decimal FatorAcumulado { get; init; }

    /// <summary>Primeira competência considerada no fator.</summary>
    public required DateOnly CompetenciaInicial { get; init; }

    /// <summary>Última competência considerada no fator.</summary>
    public required DateOnly CompetenciaFinal { get; init; }

    /// <summary>Quantidade de competências efetivamente encontradas na série e acumuladas.</summary>
    public required int MesesConsiderados { get; init; }
}
