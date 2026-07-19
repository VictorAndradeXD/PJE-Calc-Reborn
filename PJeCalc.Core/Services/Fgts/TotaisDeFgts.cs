using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Fgts;

/// <summary>
/// Totais agregados de uma apuração de FGTS. Cada parcela é arredondada a 2 casas
/// (HALF_EVEN) <b>antes</b> de entrar na soma, como o totalizador do PJe-Calc original —
/// o que difere de somar e arredondar no fim.
/// </summary>
public sealed class TotaisDeFgts
{
    private readonly IReadOnlyList<ApuracaoMensalDeFgts> _ocorrencias;

    public TotaisDeFgts(IEnumerable<ApuracaoMensalDeFgts> ocorrencias)
    {
        ArgumentNullException.ThrowIfNull(ocorrencias);
        _ocorrencias = [.. ocorrencias];
    }

    /// <summary>FGTS devido, sem correção.</summary>
    public decimal Devido => Somar(o => o.ValorDevido);

    /// <summary>FGTS devido corrigido pelo índice do tipo indicado.</summary>
    public decimal DevidoCorrigido(TipoDeCorrecaoDoFgtsEnum tipo) =>
        Somar(o => CorrecaoMonetaria.AplicacaoDeFator.Aplicar(o.ValorDevido, IndiceDe(o, tipo)));

    /// <summary>FGTS devido sem aviso prévio, corrigido.</summary>
    public decimal DevidoSemAvisoCorrigido(TipoDeCorrecaoDoFgtsEnum tipo) =>
        Somar(o => CorrecaoMonetaria.AplicacaoDeFator.Aplicar(o.ValorDevidoSemAviso, IndiceDe(o, tipo)));

    /// <summary>Diferença a recolher, sem correção.</summary>
    public decimal Diferenca => Somar(o => o.Diferenca());

    /// <summary>Diferença corrigida.</summary>
    public decimal DiferencaCorrigida(TipoDeCorrecaoDoFgtsEnum tipo) =>
        Somar(o => o.DiferencaCorrigida(tipo));

    /// <summary>Diferença sem aviso prévio, corrigida.</summary>
    public decimal DiferencaSemAvisoCorrigida(TipoDeCorrecaoDoFgtsEnum tipo) =>
        Somar(o => o.DiferencaCorrigida(tipo, excluirAvisoPrevio: true));

    /// <summary>Juros sobre as diferenças corrigidas.</summary>
    public decimal Juros(TipoDeCorrecaoDoFgtsEnum tipo) => Somar(o => o.Juros(tipo));

    /// <summary>Contribuição social de 0,5% (competências de 01/2002 a 12/2006).</summary>
    public decimal ContribuicaoSocial05 => Somar(o => o.ContribuicaoSocialDe05);

    private decimal Somar(Func<ApuracaoMensalDeFgts, decimal> parcela) =>
        _ocorrencias.Sum(o => Math.Round(parcela(o), 2, MidpointRounding.ToEven));

    private static decimal IndiceDe(ApuracaoMensalDeFgts o, TipoDeCorrecaoDoFgtsEnum tipo) =>
        tipo == TipoDeCorrecaoDoFgtsEnum.PelaDataDeDemissao ? o.IndiceAcumuladoDaMulta : o.IndiceAcumulado;
}
