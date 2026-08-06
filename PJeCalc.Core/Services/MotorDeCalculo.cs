using PJeCalc.Core.Services.Custas;
using PJeCalc.Core.Services.Honorarios;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services;

/// <summary>
/// Acessórios que compõem o principal (bruto devido ao reclamante), já apurados nos seus módulos.
/// Cada parcela entra arredondada a 2 casas, como no acumulador do original.
/// </summary>
public sealed record ComponentesDoPrincipal
{
    /// <summary>FGTS devido corrigido à liquidação (quando compõe o principal).</summary>
    public decimal FgtsCorrigidoNaLiquidacao { get; init; }

    /// <summary>Multa do FGTS (40%/20%).</summary>
    public decimal MultaDoFgts { get; init; }

    /// <summary>Multa do art. 467 da CLT sobre o FGTS.</summary>
    public decimal MultaDoArtigo467 { get; init; }

    /// <summary>Depósitos/saques a deduzir do principal (quando "deduzir do FGTS").</summary>
    public decimal DepositadoOuSacadoDeduzido { get; init; }

    /// <summary>Salário-família (quando apurado e compõe o principal).</summary>
    public decimal SalarioFamilia { get; init; }

    /// <summary>Seguro-desemprego (quando apurado e compõe o principal).</summary>
    public decimal SeguroDesemprego { get; init; }
}

/// <summary>
/// Configuração do cálculo: os parâmetros de cada módulo acessório e as bases auxiliares que
/// vêm de módulos correlatos já validados (correção do valor da causa, descontos de INSS/
/// previdência privada).
/// </summary>
public sealed record ConfiguracaoDoCalculo
{
    public required ContextoDeApuracaoDeJuros Juros { get; init; }
    public IReadOnlyList<ParametrosDaMulta> Multas { get; init; } = [];
    public IReadOnlyList<ParametrosDoHonorario> Honorarios { get; init; } = [];
    public ParametrosDeCustas? Custas { get; init; }

    /// <summary>Acessórios (FGTS/salário-família/seguro-desemprego) que compõem o principal.</summary>
    public ComponentesDoPrincipal? PrincipalAdicional { get; init; }

    /// <summary>
    /// Débitos do reclamado que entram na base das custas (BROR) além dos honorários e multas de
    /// terceiro já somados aqui: INSS patronal/segurado, contribuição social do FGTS, IRPF cobrado
    /// do reclamado etc. — apurados nos seus módulos.
    /// </summary>
    public decimal OutrosDebitosDoReclamado { get; init; }

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
        var bruto = Arredondar(apuracao.TotalDeValorCorrigido) + Arredondar(apuracao.TotalDeJuros);

        if (config.PrincipalAdicional is { } componentes)
        {
            bruto += Arredondar(componentes.FgtsCorrigidoNaLiquidacao)
                + Arredondar(componentes.MultaDoFgts)
                + Arredondar(componentes.MultaDoArtigo467)
                - Arredondar(componentes.DepositadoOuSacadoDeduzido)
                + Arredondar(componentes.SalarioFamilia)
                + Arredondar(componentes.SeguroDesemprego);
        }

        bruto += Arredondar(totaisDeMultas.ReclamanteReclamado) - Arredondar(totaisDeMultas.ReclamadoReclamante);

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
                OutrosDebitosReclamado = totaisDeHonorarios.DevidoPeloReclamado
                    + totaisDeMultas.TerceiroReclamado
                    + config.OutrosDebitosDoReclamado,
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
