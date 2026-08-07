namespace PJeCalc.Core.Services.Irpf;

/// <summary>Tipo de ocorrência de IRPF gerada, conforme a forma de tributação.</summary>
public enum TipoDeOcorrenciaDeIrpf
{
    /// <summary>Tributação normal (demais verbas + o que não foi separado/exclusivo).</summary>
    Normal,

    /// <summary>Tributação em separado das férias.</summary>
    TributacaoEmSeparado,

    /// <summary>Tributação exclusiva do 13º salário.</summary>
    TributacaoExclusiva,

    /// <summary>RRA — rendimentos recebidos acumuladamente de anos anteriores.</summary>
    RraAnosAnteriores,
}

/// <summary>Como uma verba com incidência de IRPF é classificada para a geração das ocorrências.</summary>
public enum CaracteristicaParaIrpf
{
    DecimoTerceiroSalario,
    Ferias,

    /// <summary>Demais verbas (comuns e aviso prévio).</summary>
    Demais,
}

/// <summary>
/// Uma verba com incidência de IRPF já liquidada, com os totais que a geração das ocorrências consome.
/// </summary>
public sealed record VerbaParaIrpf
{
    public required CaracteristicaParaIrpf Caracteristica { get; init; }

    /// <summary>Data inicial da ocorrência (define a competência e o recorte de anos anteriores).</summary>
    public required DateOnly DataInicial { get; init; }

    /// <summary>Diferença corrigida (base das verbas comuns e do 13º; arredondada 2 casas na geração).</summary>
    public decimal DiferencaCorrigida { get; init; }

    /// <summary>
    /// Base para cálculo das incidências (já arredondada): usada pelas férias (parcela gozada) e por
    /// todas as características no recorte de anos anteriores. Nula quando não há base (ex.: férias
    /// indenizadas), caso em que a ocorrência não entra na respectiva composição.
    /// </summary>
    public decimal? BaseParaIncidencias { get; init; }
}

/// <summary>
/// Contexto da geração das ocorrências de IRPF. Os agregados de juros e as deduções por balde
/// (INSS do reclamante, previdência privada, pensão e honorários já alocados) entram como
/// entrada — a apuração de cada um vive nos seus módulos; aqui apenas compõem a base.
/// </summary>
public sealed record ContextoDeGeracaoDeIrpf
{
    public required DateOnly DataDeLiquidacao { get; init; }
    public required TabelaIrpf Tabela { get; init; }

    /// <summary>Regime de caixa: força o tratamento sem anos anteriores (como antes de 28/07/2010).</summary>
    public bool RegimeDeCaixa { get; init; }

    public bool IncidirSobreJuros { get; init; }
    public bool ConsiderarTributacaoEmSeparado { get; init; } = true;
    public bool ConsiderarTributacaoExclusiva { get; init; } = true;

    // Juros de mora por balde (agregados já apurados; só entram na base quando IncidirSobreJuros).
    public decimal JurosDecimoTerceiro { get; init; }
    public decimal JurosFerias { get; init; }
    public decimal JurosDemaisVerbas { get; init; }
    public decimal JurosAnosAnteriores { get; init; }

    // Soma das deduções da base (INSS + previdência privada + pensão + honorários) já alocada por balde.
    public decimal DeducoesDecimoTerceiro { get; init; }
    public decimal DeducoesFerias { get; init; }
    public decimal DeducoesDemaisVerbas { get; init; }
    public decimal DeducoesAnosAnteriores { get; init; }

    /// <summary>Dedução por dependentes (valor por dependente × quantidade); 0 quando não há.</summary>
    public decimal DeducaoDependentes { get; init; }

    /// <summary>Dedução para aposentado maior de 65 anos; 0 quando não se aplica.</summary>
    public decimal DeducaoAposentadoMaior65 { get; init; }
}

/// <summary>Uma ocorrência de IRPF gerada, com a base, a faixa e o imposto devido.</summary>
public sealed record OcorrenciaDeIrpfGerada
{
    public required TipoDeOcorrenciaDeIrpf Tipo { get; init; }
    public required decimal Base { get; init; }
    public required decimal Aliquota { get; init; }
    public required decimal Deducao { get; init; }
    public required decimal Imposto { get; init; }

    /// <summary>Número de meses (competências) do RRA; 1 nas ocorrências normais.</summary>
    public required int NumeroDeMeses { get; init; }

    public required decimal ValorInicialFaixa { get; init; }
    public decimal? ValorFinalFaixa { get; init; }
}

/// <summary>
/// Geração das ocorrências de IRPF na liquidação (espelha <c>MaquinaDeCalculoDeIrpf.liquidar</c>):
/// classifica as verbas com incidência de IRPF em baldes (13º, férias, demais verbas e — no regime
/// de competência — anos anteriores), aloca os juros e as deduções, e emite uma ocorrência por forma
/// de tributação (em separado das férias, exclusiva do 13º, normal e RRA de anos anteriores). Cada
/// ocorrência tem base, faixa e imposto pela tabela progressiva (<see cref="ApuracaoDeIrpf"/>).
///
/// <para>O regime é escolhido pelo corte de 28/07/2010: liquidação anterior a essa data, ou marcada
/// como regime de caixa, ignora o recorte de anos anteriores; a partir dela, as verbas cuja data
/// inicial é anterior a 1º de janeiro do ano da liquidação viram uma ocorrência RRA, cujos limites de
/// faixa e parcela a deduzir escalam pelo número de competências distintas.</para>
/// </summary>
public static class GeradorDeOcorrenciasDeIrpf
{
    private static readonly DateOnly CorteRegimeCompetencia = new(2010, 7, 28);

    public static IReadOnlyList<OcorrenciaDeIrpfGerada> Gerar(
        IReadOnlyList<VerbaParaIrpf> verbas, ContextoDeGeracaoDeIrpf contexto)
    {
        ArgumentNullException.ThrowIfNull(verbas);
        ArgumentNullException.ThrowIfNull(contexto);

        var semAnosAnteriores = contexto.RegimeDeCaixa || contexto.DataDeLiquidacao < CorteRegimeCompetencia;
        return semAnosAnteriores ? GerarRegimeCaixa(verbas, contexto) : GerarRegimeCompetencia(verbas, contexto);
    }

    /// <summary>Regime de caixa / antes de 28/07/2010: tudo é corrente, sem RRA.</summary>
    private static List<OcorrenciaDeIrpfGerada> GerarRegimeCaixa(
        IReadOnlyList<VerbaParaIrpf> verbas, ContextoDeGeracaoDeIrpf ctx)
    {
        var baldes = new BaldesCorrentes();
        foreach (var verba in verbas)
            baldes.Acumular(verba);

        return MontarCorrentes(baldes, ctx, []);
    }

    /// <summary>Regime de competência (a partir de 28/07/2010): separa anos anteriores (RRA) do corrente.</summary>
    private static List<OcorrenciaDeIrpfGerada> GerarRegimeCompetencia(
        IReadOnlyList<VerbaParaIrpf> verbas, ContextoDeGeracaoDeIrpf ctx)
    {
        var dataLimite = new DateOnly(ctx.DataDeLiquidacao.Year, 1, 1);

        var baldes = new BaldesCorrentes();
        var verbaAnosAnteriores = 0m;
        var existeAnosAnteriores = false;
        var mesesAnosAnteriores = new HashSet<DateOnly>();
        var mesesAnosAnterioresDecimoTerceiro = new HashSet<DateOnly>();

        foreach (var verba in verbas)
        {
            if (verba.DataInicial < dataLimite)
            {
                if (Arredondar(verba.DiferencaCorrigida) == 0m)
                    continue;

                var competencia = new DateOnly(verba.DataInicial.Year, verba.DataInicial.Month, 1);
                if (verba.BaseParaIncidencias is { } baseInc)
                {
                    verbaAnosAnteriores += baseInc;
                    existeAnosAnteriores = true;
                }

                if (verba.Caracteristica == CaracteristicaParaIrpf.DecimoTerceiroSalario)
                    mesesAnosAnterioresDecimoTerceiro.Add(competencia);
                else if (verba.BaseParaIncidencias is not null)
                    mesesAnosAnteriores.Add(competencia);

                continue;
            }

            baldes.Acumular(verba);
        }

        var ocorrencias = new List<OcorrenciaDeIrpfGerada>();

        var numeroDeMeses = mesesAnosAnteriores.Count + mesesAnosAnterioresDecimoTerceiro.Count;
        if (numeroDeMeses > 0 && existeAnosAnteriores)
        {
            ocorrencias.Add(CriarRra(
                verbaAnosAnteriores,
                ctx.IncidirSobreJuros ? ctx.JurosAnosAnteriores : 0m,
                ctx.DeducoesAnosAnteriores,
                numeroDeMeses, ctx));
        }

        return MontarCorrentes(baldes, ctx, ocorrencias);
    }

    /// <summary>Emite as ocorrências correntes (em separado, exclusiva e normal) a partir dos baldes.</summary>
    private static List<OcorrenciaDeIrpfGerada> MontarCorrentes(
        BaldesCorrentes b, ContextoDeGeracaoDeIrpf ctx, List<OcorrenciaDeIrpfGerada> ocorrencias)
    {
        var verbaNormal = b.Decimo + b.Ferias + b.Demais;
        var jurosNormal = ctx.JurosDecimoTerceiro + ctx.JurosFerias + ctx.JurosDemaisVerbas;
        var deducoesNormal = ctx.DeducoesDecimoTerceiro + ctx.DeducoesFerias + ctx.DeducoesDemaisVerbas;
        var existeDemais = b.ExisteDemais;

        if (ctx.ConsiderarTributacaoEmSeparado && b.ExisteFerias)
        {
            ocorrencias.Add(CriarCorrente(
                TipoDeOcorrenciaDeIrpf.TributacaoEmSeparado, b.Ferias, ctx.JurosFerias, ctx.DeducoesFerias, ctx));
            verbaNormal -= b.Ferias;
            jurosNormal -= ctx.JurosFerias;
            deducoesNormal -= ctx.DeducoesFerias;
        }
        else if (b.ExisteFerias)
        {
            existeDemais = true;
        }

        if (ctx.ConsiderarTributacaoExclusiva && b.ExisteDecimo)
        {
            ocorrencias.Add(CriarCorrente(
                TipoDeOcorrenciaDeIrpf.TributacaoExclusiva, b.Decimo, ctx.JurosDecimoTerceiro, ctx.DeducoesDecimoTerceiro, ctx));
            verbaNormal -= b.Decimo;
            jurosNormal -= ctx.JurosDecimoTerceiro;
            deducoesNormal -= ctx.DeducoesDecimoTerceiro;
        }
        else if (b.ExisteDecimo)
        {
            existeDemais = true;
        }

        if (existeDemais)
        {
            ocorrencias.Add(CriarCorrente(
                TipoDeOcorrenciaDeIrpf.Normal, verbaNormal, jurosNormal, deducoesNormal, ctx));
        }

        return ocorrencias;
    }

    /// <summary>Ocorrência corrente (NM = 1): dependentes e aposentado entram por inteiro.</summary>
    private static OcorrenciaDeIrpfGerada CriarCorrente(
        TipoDeOcorrenciaDeIrpf tipo, decimal verba, decimal juros, decimal deducoes, ContextoDeGeracaoDeIrpf ctx)
    {
        var entrada = new OcorrenciaDeIrpfEntrada
        {
            Verbas = verba,
            Juros = ctx.IncidirSobreJuros ? juros : 0m,
            ContribuicaoSocial = deducoes,
            Dependentes = ctx.DeducaoDependentes,
            AposentadoMaior65 = ctx.DeducaoAposentadoMaior65,
            NumeroDeMeses = 1,
        };

        var r = ApuracaoDeIrpf.Calcular(ctx.Tabela, entrada);
        var faixa = ctx.Tabela.ObterFaixaParaValor(r.Base);
        return new OcorrenciaDeIrpfGerada
        {
            Tipo = tipo,
            Base = r.Base,
            Aliquota = r.Aliquota,
            Deducao = r.Deducao,
            Imposto = r.Imposto,
            NumeroDeMeses = 1,
            ValorInicialFaixa = faixa.ValorInicial,
            ValorFinalFaixa = faixa.ValorFinal,
        };
    }

    /// <summary>Ocorrência RRA: limites de faixa, parcela a deduzir e deduções fixas escalam pelo NM.</summary>
    private static OcorrenciaDeIrpfGerada CriarRra(
        decimal verba, decimal juros, decimal deducoes, int numeroDeMeses, ContextoDeGeracaoDeIrpf ctx)
    {
        var entrada = new OcorrenciaDeIrpfEntrada
        {
            Verbas = verba,
            Juros = juros,
            ContribuicaoSocial = deducoes,
            Dependentes = ctx.DeducaoDependentes * numeroDeMeses,
            AposentadoMaior65 = ctx.DeducaoAposentadoMaior65 * numeroDeMeses,
            NumeroDeMeses = numeroDeMeses,
        };

        var r = ApuracaoDeIrpf.Calcular(ctx.Tabela, entrada);
        var faixa = ctx.Tabela.ObterFaixaParaValor(r.Base, numeroDeMeses);

        // O início da faixa desconta 1 centavo antes de escalar e o recoloca depois (piso zero).
        var valorInicial = (faixa.ValorInicial - 0.01m) * numeroDeMeses + 0.01m;
        if (valorInicial < 0m)
            valorInicial = 0m;

        return new OcorrenciaDeIrpfGerada
        {
            Tipo = TipoDeOcorrenciaDeIrpf.RraAnosAnteriores,
            Base = r.Base,
            Aliquota = r.Aliquota,
            Deducao = r.Deducao,
            Imposto = r.Imposto,
            NumeroDeMeses = numeroDeMeses,
            ValorInicialFaixa = valorInicial,
            ValorFinalFaixa = faixa.ValorFinal is { } fim ? fim * numeroDeMeses : null,
        };
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);

    /// <summary>Acumuladores dos baldes correntes (13º, férias gozadas e demais verbas).</summary>
    private sealed class BaldesCorrentes
    {
        public decimal Decimo { get; private set; }
        public decimal Ferias { get; private set; }
        public decimal Demais { get; private set; }
        public bool ExisteDecimo { get; private set; }
        public bool ExisteFerias { get; private set; }
        public bool ExisteDemais { get; private set; }

        public void Acumular(VerbaParaIrpf verba)
        {
            switch (verba.Caracteristica)
            {
                case CaracteristicaParaIrpf.DecimoTerceiroSalario:
                    Decimo += Arredondar(verba.DiferencaCorrigida);
                    ExisteDecimo = true;
                    break;

                case CaracteristicaParaIrpf.Ferias:
                    if (verba.BaseParaIncidencias is { } baseFerias)
                        Ferias += baseFerias;
                    ExisteFerias = true;
                    break;

                case CaracteristicaParaIrpf.Demais:
                    Demais += Arredondar(verba.DiferencaCorrigida);
                    ExisteDemais = true;
                    break;
            }
        }
    }
}
