using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Fgts;

/// <summary>
/// Apuração do FGTS de uma competência: bases, alíquota, o que já foi depositado e os
/// fatores de correção/juros daquele mês. Expõe a matemática por competência do PJe-Calc.
///
/// <para>Os dois índices vêm da correção monetária: <see cref="IndiceAcumulado"/> acumula
/// até a data de liquidação (valor a pagar) e <see cref="IndiceAcumuladoDaMulta"/> até a
/// data de demissão (base da multa).</para>
/// </summary>
public sealed record ApuracaoMensalDeFgts
{
    /// <summary>Competência apurada (1º dia do mês).</summary>
    public required DateOnly Competencia { get; init; }

    /// <summary>Base vinda do histórico salarial.</summary>
    public decimal BaseHistorico { get; init; }

    /// <summary>Base vinda das verbas com incidência de FGTS.</summary>
    public decimal BaseVerba { get; init; }

    /// <summary>Base das verbas excluindo o aviso prévio (usada na multa, quando aplicável).</summary>
    public decimal BaseVerbaSemAvisoPrevio { get; init; }

    public required AliquotaDoFgtsEnum Aliquota { get; init; }

    /// <summary>FGTS já recolhido na competência (valor, não base).</summary>
    public decimal Depositado { get; init; }

    /// <summary>Fator acumulado até a data de liquidação.</summary>
    public decimal IndiceAcumulado { get; init; }

    /// <summary>Fator acumulado até a data de demissão (base da multa).</summary>
    public decimal IndiceAcumuladoDaMulta { get; init; }

    /// <summary>Taxa de juros da competência, em pontos percentuais.</summary>
    public decimal TaxaDeJuros { get; init; }

    /// <summary>Soma das bases de incidência do mês.</summary>
    public decimal SomaDasBases(bool excluirAvisoPrevio = false) =>
        BaseHistorico + (excluirAvisoPrevio ? BaseVerbaSemAvisoPrevio : BaseVerba);

    /// <summary>FGTS devido no mês: alíquota sobre a soma das bases.</summary>
    public decimal ValorDevido => FatorDaAliquota * SomaDasBases();

    /// <summary>FGTS devido desconsiderando o aviso prévio.</summary>
    public decimal ValorDevidoSemAviso => FatorDaAliquota * SomaDasBases(excluirAvisoPrevio: true);

    /// <summary>Diferença a recolher: devido menos depositado, nunca negativa.</summary>
    public decimal Diferenca(bool excluirAvisoPrevio = false)
    {
        var devido = excluirAvisoPrevio ? ValorDevidoSemAviso : ValorDevido;
        var diferenca = devido - Depositado;
        return diferenca < 0m ? 0m : diferenca;
    }

    /// <summary>Diferença corrigida pelo índice do tipo indicado (arredondada a 2 casas).</summary>
    public decimal DiferencaCorrigida(TipoDeCorrecaoDoFgtsEnum tipo, bool excluirAvisoPrevio = false) =>
        AplicacaoDeFator.Aplicar(Diferenca(excluirAvisoPrevio), IndiceDe(tipo));

    /// <summary>Juros sobre a diferença corrigida.</summary>
    public decimal Juros(TipoDeCorrecaoDoFgtsEnum tipo) =>
        DiferencaCorrigida(tipo) * TaxaDeJuros / 100m;

    /// <summary>Diferença corrigida acrescida dos juros.</summary>
    public decimal Total(TipoDeCorrecaoDoFgtsEnum tipo) =>
        DiferencaCorrigida(tipo) + Juros(tipo);

    /// <summary>
    /// Contribuição social de 0,5% (LC 110/2001), devida apenas nas competências de
    /// 01/2002 a 12/2006.
    /// </summary>
    public decimal ContribuicaoSocialDe05 =>
        Competencia >= InicioContribuicaoSocial && Competencia <= FimContribuicaoSocial
            ? SomaDasBases() * TaxaContribuicaoSocial / 100m
            : 0m;

    private decimal IndiceDe(TipoDeCorrecaoDoFgtsEnum tipo) => tipo switch
    {
        TipoDeCorrecaoDoFgtsEnum.PelaDataDeDemissao => IndiceAcumuladoDaMulta,
        _ => IndiceAcumulado,
    };

    private decimal FatorDaAliquota =>
        Aliquota == AliquotaDoFgtsEnum.DoisPorCento ? 0.02m : 0.08m;

    private const decimal TaxaContribuicaoSocial = 0.5m;
    private static readonly DateOnly InicioContribuicaoSocial = new(2002, 1, 1);
    private static readonly DateOnly FimContribuicaoSocial = new(2006, 12, 31);
}
