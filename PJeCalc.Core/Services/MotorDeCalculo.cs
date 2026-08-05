using PJeCalc.Core.Services.Custas;
using PJeCalc.Core.Services.Honorarios;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services;

/// <summary>
/// Configuração do cálculo: os parâmetros de cada módulo acessório e as bases auxiliares que
/// vêm de módulos correlatos já validados (correção do valor da causa, descontos de INSS/
/// previdência privada — estes ainda em 0 no caminho mínimo).
/// </summary>
public sealed record ConfiguracaoDoCalculo
{
    public required ContextoDeApuracaoDeJuros Juros { get; init; }
    public IReadOnlyList<ParametrosDaMulta> Multas { get; init; } = [];
    public IReadOnlyList<ParametrosDoHonorario> Honorarios { get; init; } = [];
    public ParametrosDeCustas? Custas { get; init; }

    /// <summary>Valor da causa já corrigido do ajuizamento à liquidação (base de multa "valor da causa").</summary>
    public decimal ValorDaCausaCorrigido { get; init; }

    /// <summary>Soma das verbas que não compõem o principal (base VNP de honorário), já corrigida.</summary>
    public decimal SomaVerbasNaoPrincipalDoHonorario { get; init; }

    /// <summary>Cota do segurado reclamante a descontar da base de multas/honorários (2 casas).</summary>
    public decimal DescontoContribuicaoSocial { get; init; }

    /// <summary>Total do devido corrigido da previdência privada a descontar.</summary>
    public decimal DescontoPrevidenciaPrivada { get; init; }
}

/// <summary>Resultado consolidado do cálculo (caminho mínimo).</summary>
public sealed record ResultadoDoCalculo
{
    public required ResultadoDaApuracaoDeJuros ApuracaoDeJuros { get; init; }
    public required decimal BrutoDevidoAoReclamante { get; init; }
    public required IReadOnlyList<ResultadoDaMulta> Multas { get; init; }
    public required TotaisDeMultas TotaisDeMultas { get; init; }
    public required IReadOnlyList<ResultadoDoHonorario> Honorarios { get; init; }
    public required TotaisDeHonorarios TotaisDeHonorarios { get; init; }
    public ResultadoDasCustas? Custas { get; init; }
}

/// <summary>
/// Orquestra o cálculo trabalhista no caminho mínimo (sem os acessórios adiados — INSS
/// empregador, FGTS, salário-família, seguro-desemprego, pensão), sequenciando os núcleos já
/// validados na ordem do motor oficial.
///
/// <para>Verbas liquidadas → <b>apuração de juros</b> (principal corrigido + juros) → <b>multas</b>
/// (que incidem sobre o principal apurado) → <b>bruto devido ao reclamante</b> (principal + juros +
/// multas reclamante→reclamado − reclamado→reclamante) → <b>honorários</b> e <b>custas</b> (que
/// incidem sobre o bruto; as custas somam ainda os honorários e multas devidos pelo reclamado).</para>
///
/// <para>As verbas já devem estar liquidadas (via <see cref="MotorDeVerbas"/>); o orquestrador
/// consome suas ocorrências e monta os agregados que antes eram entrada dos acessórios.</para>
/// </summary>
public static class MotorDeCalculo
{
    public static ResultadoDoCalculo Calcular(IEnumerable<VerbaEmCalculo> verbas, ConfiguracaoDoCalculo config)
    {
        ArgumentNullException.ThrowIfNull(verbas);
        ArgumentNullException.ThrowIfNull(config);

        var verbasLista = verbas as IReadOnlyList<VerbaEmCalculo> ?? verbas.ToList();
        var apuracao = ApuradorDeJuros.Apurar(verbasLista, config.Juros);

        var basesDaMulta = new BasesDaMulta
        {
            PrincipalCorrigido = apuracao.TotalDeValorCorrigido,
            JurosDeMora = apuracao.TotalDeJuros,
            ValorDaCausaCorrigido = config.ValorDaCausaCorrigido,
            DescontoContribuicaoSocial = config.DescontoContribuicaoSocial,
            DescontoPrevidenciaPrivada = config.DescontoPrevidenciaPrivada,
        };
        var multas = config.Multas.Select(p => (Parametros: p, Resultado: ApuracaoDeMulta.Calcular(p, basesDaMulta))).ToList();
        var totaisDeMultas = TotalizadorDeMulta.Calcular(
            multas.Select(m => (m.Parametros.CredorDevedor, m.Resultado.ValorTotal)));

        // Bruto: cada parcela arredondada a 2 casas antes de somar, como no acumulador do original.
        var bruto = Arredondar(apuracao.TotalDeValorCorrigido)
            + Arredondar(apuracao.TotalDeJuros)
            + Arredondar(totaisDeMultas.ReclamanteReclamado)
            - Arredondar(totaisDeMultas.ReclamadoReclamante);

        var basesDoHonorario = new BasesDoHonorario
        {
            BrutoDevidoAoReclamante = bruto,
            SomaVerbasNaoPrincipal = config.SomaVerbasNaoPrincipalDoHonorario,
            DescontoContribuicaoSocial = config.DescontoContribuicaoSocial,
            DescontoPrevidenciaPrivada = config.DescontoPrevidenciaPrivada,
        };
        var honorarios = config.Honorarios.Select(p => (Parametros: p, Resultado: ApuracaoDeHonorario.Calcular(p, basesDoHonorario))).ToList();
        var totaisDeHonorarios = TotalizadorDeHonorario.Calcular(
            honorarios.Select(h => (h.Parametros.Devedor, h.Parametros.Cobranca, h.Resultado.ValorTotal)));

        ResultadoDasCustas? custas = null;
        if (config.Custas is { } configCustas)
        {
            var basesDasCustas = new BasesDasCustas
            {
                BrutoDevidoAoReclamante = bruto,
                OutrosDebitosReclamado = totaisDeHonorarios.DevidoPeloReclamado + totaisDeMultas.TerceiroReclamado,
            };
            custas = ApuracaoDeCustas.Calcular(configCustas, basesDasCustas);
        }

        return new ResultadoDoCalculo
        {
            ApuracaoDeJuros = apuracao,
            BrutoDevidoAoReclamante = bruto,
            Multas = multas.Select(m => m.Resultado).ToList(),
            TotaisDeMultas = totaisDeMultas,
            Honorarios = honorarios.Select(h => h.Resultado).ToList(),
            TotaisDeHonorarios = totaisDeHonorarios,
            Custas = custas,
        };
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
