using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Custas;

/// <summary>Bases já liquidadas que as custas calculadas consomem.</summary>
public sealed record BasesDasCustas
{
    /// <summary>Bruto devido ao reclamante (principal corrigido + juros + multas + FGTS).</summary>
    public decimal BrutoDevidoAoReclamante { get; init; }

    /// <summary>Débitos do reclamado (INSS patronal, honorários, IRPF cobrado do reclamado...).</summary>
    public decimal OutrosDebitosReclamado { get; init; }
}

/// <summary>Custa de auto judicial (avaliação × 5%, com teto).</summary>
public sealed record ItemDeAuto(decimal Avaliacao, decimal Indice = 1m);

/// <summary>Custa de armazenamento (avaliação × 0,1% ao dia).</summary>
public sealed record ItemDeArmazenamento(decimal Avaliacao, int Dias, decimal Indice = 1m);

/// <summary>Custa já paga, deduzida do total.</summary>
public sealed record CustaPagaDeCustas(decimal Valor, decimal Indice = 1m);

/// <summary>Quantidades das nove custas fixas por tipo de ato.</summary>
public sealed record CustasFixasQuantidades
{
    public int AtosUrbanos { get; init; }
    public int AtosRurais { get; init; }
    public int AgravoInstrumento { get; init; }
    public int AgravoPeticao { get; init; }
    public int ImpugnacaoSentenca { get; init; }
    public int EmbargosArrematacao { get; init; }
    public int EmbargosExecucao { get; init; }
    public int EmbargosTerceiros { get; init; }
    public int RecursoRevista { get; init; }
    public decimal Indice { get; init; } = 1m;
}

/// <summary>Configuração das custas do reclamado.</summary>
public sealed record ParametrosDeCustas
{
    public required ParametrosDeCustasFixas Parametros { get; init; }
    public BaseParaCustasCalculadasEnum BaseCalculada { get; init; } = BaseParaCustasCalculadasEnum.BrutoDevidoAoReclamanteMaisDebitosReclamado;

    public TipoDeCustasDeConhecimentoEnum ConhecimentoReclamado { get; init; } = TipoDeCustasDeConhecimentoEnum.NaoSeAplica;
    public decimal ConhecimentoReclamadoInformado { get; init; }
    public decimal ConhecimentoReclamadoIndice { get; init; } = 1m;

    public TipoDeCustasDeLiquidacaoEnum Liquidacao { get; init; } = TipoDeCustasDeLiquidacaoEnum.NaoSeAplica;
    public decimal LiquidacaoInformada { get; init; }
    public decimal LiquidacaoIndice { get; init; } = 1m;

    /// <summary>Teto das custas de conhecimento (4× teto do RGPS); nulo antes da Reforma Trabalhista.</summary>
    public decimal? TetoConhecimento { get; init; }

    public IReadOnlyList<ItemDeAuto> Autos { get; init; } = [];
    public IReadOnlyList<ItemDeArmazenamento> Armazenamentos { get; init; } = [];
    public CustasFixasQuantidades? CustasFixas { get; init; }
    public IReadOnlyList<CustaPagaDeCustas> CustasPagasReclamado { get; init; } = [];
}

/// <summary>Resultado da apuração das custas do reclamado.</summary>
public sealed record ResultadoDasCustas(
    decimal BaseCalculada,
    decimal? TotalConhecimento,
    decimal? TotalLiquidacao,
    decimal ConsolidadoReclamado);

/// <summary>
/// Apuração das custas processuais devidas pelo reclamado (CLT art. 789 e ss.).
///
/// <para>Custas de conhecimento = base × 2%, com piso e — a partir da Reforma Trabalhista — teto
/// de 4× o teto do RGPS. Liquidação = base × 0,5% com teto tabelado. Autos = avaliação × 5% com
/// teto; armazenamento = avaliação × 0,1% ao dia. Somam-se as nove custas fixas (valor tabelado ×
/// quantidade) e subtraem-se as custas já pagas, com piso zero no consolidado.</para>
///
/// <para>Como no original, os percentuais das custas calculadas de conhecimento e liquidação usam
/// as constantes exatas "2.0" e "0.5" (sem arredondar a 2 casas); autos, armazenamento e custas
/// fixas são corrigidos pelo índice (que arredonda a 2 casas).</para>
/// </summary>
public static class ApuracaoDeCustas
{
    public static ResultadoDasCustas Calcular(ParametrosDeCustas p, BasesDasCustas bases)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(bases);

        var baseCalculada = p.BaseCalculada == BaseParaCustasCalculadasEnum.BrutoDevidoAoReclamanteMaisDebitosReclamado
            ? bases.BrutoDevidoAoReclamante + bases.OutrosDebitosReclamado
            : bases.BrutoDevidoAoReclamante;

        var totalConhecimento = TotalConhecimentoReclamado(p, baseCalculada);
        var totalLiquidacao = TotalLiquidacaoReclamado(p, baseCalculada);
        var consolidado = Consolidar(p, totalConhecimento, totalLiquidacao);

        return new ResultadoDasCustas(baseCalculada, totalConhecimento, totalLiquidacao, consolidado);
    }

    /// <summary>Custas de conhecimento do reclamado: percentual → piso → teto.</summary>
    public static decimal? TotalConhecimentoReclamado(ParametrosDeCustas p, decimal baseCalculada) =>
        p.ConhecimentoReclamado switch
        {
            TipoDeCustasDeConhecimentoEnum.Calculada2PorCento =>
                AplicarTeto(p.TetoConhecimento,
                    AplicarPiso(p.Parametros.PisoConhecimento, baseCalculada * (2.0m / 100m))),

            TipoDeCustasDeConhecimentoEnum.Informada =>
                AplicarPiso(p.Parametros.PisoConhecimento,
                    AplicacaoDeFator.Aplicar(p.ConhecimentoReclamadoInformado, p.ConhecimentoReclamadoIndice)),

            _ => null,
        };

    /// <summary>Custas de liquidação do reclamado: percentual/valor → teto.</summary>
    public static decimal? TotalLiquidacaoReclamado(ParametrosDeCustas p, decimal baseCalculada) =>
        p.Liquidacao switch
        {
            TipoDeCustasDeLiquidacaoEnum.CalculadaMeioPorCento =>
                AplicarTeto(p.Parametros.TetoLiquidacao, baseCalculada * (0.5m / 100m)),

            TipoDeCustasDeLiquidacaoEnum.Informada =>
                AplicarTeto(p.Parametros.TetoLiquidacao,
                    AplicacaoDeFator.Aplicar(p.LiquidacaoInformada, p.LiquidacaoIndice)),

            _ => null,
        };

    private static decimal Consolidar(ParametrosDeCustas p, decimal? conhecimento, decimal? liquidacao)
    {
        decimal? total = conhecimento;
        total = Somar(total, liquidacao);

        foreach (var fixa in CustasFixas(p))
            if (fixa != 0m)
                total = Somar(total, fixa);

        foreach (var auto in p.Autos)
            total = Somar(total, TotalDoAuto(p, auto));

        foreach (var armazenamento in p.Armazenamentos)
            total = Somar(total, TotalDoArmazenamento(armazenamento));

        if (total is not { } consolidado)
            return 0m;

        foreach (var paga in p.CustasPagasReclamado)
            consolidado -= AplicacaoDeFator.Aplicar(paga.Valor, paga.Indice);

        return consolidado < 0m ? 0m : consolidado;
    }

    private static IEnumerable<decimal> CustasFixas(ParametrosDeCustas p)
    {
        if (p.CustasFixas is not { } q)
            yield break;

        var t = p.Parametros;
        yield return Corrigir(t.AtosUrbanos * q.AtosUrbanos, q.Indice);
        yield return Corrigir(t.AtosRurais * q.AtosRurais, q.Indice);
        yield return Corrigir(t.AgravoInstrumento * q.AgravoInstrumento, q.Indice);
        yield return Corrigir(t.AgravoPeticao * q.AgravoPeticao, q.Indice);
        yield return Corrigir(t.ImpugnacaoSentenca * q.ImpugnacaoSentenca, q.Indice);
        yield return Corrigir(t.EmbargosArrematacao * q.EmbargosArrematacao, q.Indice);
        yield return Corrigir(t.EmbargosExecucao * q.EmbargosExecucao, q.Indice);
        yield return Corrigir(t.EmbargosTerceiros * q.EmbargosTerceiros, q.Indice);
        yield return Corrigir(t.RecursoRevista * q.RecursoRevista, q.Indice);
    }

    private static decimal TotalDoAuto(ParametrosDeCustas p, ItemDeAuto auto)
    {
        var custa = auto.Avaliacao * (5.0m / 100m);
        custa = AplicarTeto(p.Parametros.TetoAutos, custa);
        return Corrigir(custa, auto.Indice);
    }

    private static decimal TotalDoArmazenamento(ItemDeArmazenamento armazenamento) =>
        Corrigir(armazenamento.Avaliacao * 0.001m * armazenamento.Dias, armazenamento.Indice);

    private static decimal Corrigir(decimal valor, decimal indice) => AplicacaoDeFator.Aplicar(valor, indice);

    private static decimal AplicarPiso(decimal? piso, decimal valor) =>
        piso is { } p && valor != 0m && valor < p ? p : valor;

    private static decimal AplicarTeto(decimal? teto, decimal valor) =>
        teto is { } t && valor > t ? t : valor;

    private static decimal? Somar(decimal? acumulado, decimal? parcela) =>
        parcela is null ? acumulado : (acumulado ?? 0m) + parcela.Value;
}
