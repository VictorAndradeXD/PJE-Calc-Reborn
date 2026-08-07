using System.Globalization;
using PJeCalc.Core.Services.Irpf;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da GERAÇÃO das ocorrências de IRPF na liquidação
/// (<see cref="GeradorDeOcorrenciasDeIrpf"/>) contra o motor oficial (harness
/// <c>tools/golden/GoldenGenIrpfGeracao.java</c>, que dirige
/// <c>MaquinaDeCalculoDeIrpf.liquidar</c> e lê <c>irpf.getOcorrencias()</c>).
///
/// <para>Exercita a lógica nova: escolha do regime pelo corte 28/07/2010, classificação das
/// verbas em baldes (13º/férias/demais/anos anteriores), tipos de tributação (em separado,
/// exclusiva, normal), contagem das competências do RRA e a faixa × NM, além da incidência
/// sobre juros e das deduções fixas (dependentes/aposentado). Casos em
/// Fixtures/golden_irpf_geracao.csv.</para>
/// </summary>
public class IrpfGeracaoGoldenTests
{
    private static readonly TabelaIrpf Tabela = CarregarTabela(new DateOnly(2024, 2, 1));
    private static readonly Dictionary<string, Dictionary<string, string>> GoldenPorCenario = CarregarGolden();

    private static readonly DateOnly Liquidacao = new(2024, 2, 10);

    [Fact]
    public void A1_regime_de_caixa_gera_os_tres_tipos()
    {
        var ctx = ContextoBase() with { RegimeDeCaixa = true };
        var verbas = new[]
        {
            Verba(CaracteristicaParaIrpf.Ferias, "2024-01-05", 5000m),
            Verba(CaracteristicaParaIrpf.DecimoTerceiroSalario, "2024-01-05", 3000m),
            Verba(CaracteristicaParaIrpf.Demais, "2024-01-05", 4000m),
        };

        Conferir("A1_CAIXA_3TIPOS", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    [Fact]
    public void A2_sem_separado_nem_exclusiva_cai_tudo_no_normal()
    {
        var ctx = ContextoBase() with
        {
            RegimeDeCaixa = true,
            ConsiderarTributacaoEmSeparado = false,
            ConsiderarTributacaoExclusiva = false,
        };
        var verbas = new[]
        {
            Verba(CaracteristicaParaIrpf.Ferias, "2024-01-05", 5000m),
            Verba(CaracteristicaParaIrpf.DecimoTerceiroSalario, "2024-01-05", 3000m),
            Verba(CaracteristicaParaIrpf.Demais, "2024-01-05", 4000m),
        };

        Conferir("A2_CAIXA_SO_NORMAL", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    [Fact]
    public void A3_incidencia_sobre_juros_e_dependentes_reduzem_a_base()
    {
        var ctx = ContextoBase() with
        {
            RegimeDeCaixa = true,
            IncidirSobreJuros = true,
            JurosDemaisVerbas = 500m,
            DeducaoDependentes = Tabela.DeducaoPorDependente * 2,
        };
        var verbas = new[] { Verba(CaracteristicaParaIrpf.Demais, "2024-01-05", 4000m) };

        Conferir("A3_CAIXA_JUROS_DEP", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    [Fact]
    public void B1_anos_anteriores_geram_rra_com_nm_igual_as_competencias()
    {
        var ctx = ContextoBase();
        var verbas = new[]
        {
            Verba(CaracteristicaParaIrpf.Demais, "2022-05-01", 20000m),
            Verba(CaracteristicaParaIrpf.Demais, "2023-03-01", 20000m),
            Verba(CaracteristicaParaIrpf.Demais, "2023-07-01", 20000m),
            Verba(CaracteristicaParaIrpf.Demais, "2024-02-01", 4000m),
        };

        Conferir("B1_RRA_NM3", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    [Fact]
    public void B2_rra_conta_o_13_e_aplica_aposentado_vezes_nm()
    {
        var ctx = ContextoBase() with { DeducaoAposentadoMaior65 = Tabela.DeducaoAposentadoMaior65 };
        var verbas = new[]
        {
            Verba(CaracteristicaParaIrpf.Demais, "2022-05-01", 15000m),
            Verba(CaracteristicaParaIrpf.DecimoTerceiroSalario, "2022-12-01", 10000m),
        };

        Conferir("B2_RRA_13_APOS", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    [Fact]
    public void B3_regime_de_competencia_sem_anos_anteriores_nao_gera_rra()
    {
        var ctx = ContextoBase();
        var verbas = new[]
        {
            Verba(CaracteristicaParaIrpf.Ferias, "2024-01-05", 5000m),
            Verba(CaracteristicaParaIrpf.Demais, "2024-02-01", 4000m),
        };

        Conferir("B3_COMPETENCIA_CORRENTE", GeradorDeOcorrenciasDeIrpf.Gerar(verbas, ctx));
    }

    // ---- Infraestrutura ----

    private static ContextoDeGeracaoDeIrpf ContextoBase() => new()
    {
        DataDeLiquidacao = Liquidacao,
        Tabela = Tabela,
    };

    private static VerbaParaIrpf Verba(CaracteristicaParaIrpf carac, string dataInicial, decimal diferencaCorrigida) => new()
    {
        Caracteristica = carac,
        DataInicial = DateOnly.ParseExact(dataInicial, "yyyy-MM-dd", CultureInfo.InvariantCulture),
        DiferencaCorrigida = diferencaCorrigida,
        BaseParaIncidencias = Math.Round(diferencaCorrigida, 2, MidpointRounding.ToEven),
    };

    private static void Conferir(string cenario, IReadOnlyList<OcorrenciaDeIrpfGerada> ocorrencias)
    {
        var golden = GoldenPorCenario[cenario];
        Assert.Equal(int.Parse(golden["numOcorrencias"], CultureInfo.InvariantCulture), ocorrencias.Count);

        foreach (var o in ocorrencias)
        {
            var t = Nome(o.Tipo);
            Assert.Equal(Num(golden[$"{t}.base"]), o.Base);
            Assert.Equal(Num(golden[$"{t}.aliquota"]), o.Aliquota);
            Assert.Equal(Num(golden[$"{t}.deducao"]), o.Deducao);
            Assert.Equal(Num(golden[$"{t}.devido"]), o.Imposto);
            Assert.Equal(int.Parse(golden[$"{t}.nm"], CultureInfo.InvariantCulture), o.NumeroDeMeses);
            Assert.Equal(Num(golden[$"{t}.faixaInicial"]), o.ValorInicialFaixa);
            Assert.Equal(Opcional(golden[$"{t}.faixaFinal"]), o.ValorFinalFaixa);
        }
    }

    private static string Nome(TipoDeOcorrenciaDeIrpf tipo) => tipo switch
    {
        TipoDeOcorrenciaDeIrpf.Normal => "NORMAL",
        TipoDeOcorrenciaDeIrpf.TributacaoEmSeparado => "TRIBUTACAO_EM_SEPARADO",
        TipoDeOcorrenciaDeIrpf.TributacaoExclusiva => "TRIBUTACAO_EXCLUSIVA",
        TipoDeOcorrenciaDeIrpf.RraAnosAnteriores => "RRA_ANOS_ANTERIORES",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    private static Dictionary<string, Dictionary<string, string>> CarregarGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_irpf_geracao.csv");
        var porCenario = new Dictionary<string, Dictionary<string, string>>();
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            if (!porCenario.TryGetValue(c[0], out var mapa))
                porCenario[c[0]] = mapa = new Dictionary<string, string>();
            mapa[c[1]] = c.Length > 2 ? c[2] : "";
        }
        return porCenario;
    }

    private static TabelaIrpf CarregarTabela(DateOnly competencia)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Irpf", "irpf_tabela.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(',').Select(campo => campo.Trim().Trim('"')).ToArray();
            if (Data(c[0]) != competencia)
                continue;

            return new TabelaIrpf
            {
                Competencia = competencia,
                Faixa1 = Faixa(c[1], c[2], c[3], c[4])!,
                Faixa2 = Faixa(c[5], c[6], c[7], c[8]),
                Faixa3 = Faixa(c[9], c[10], c[11], c[12]),
                Faixa4 = Faixa(c[13], c[14], c[15], c[16]),
                Faixa5 = Faixa(c[17], c[18], c[19], c[20]),
                DeducaoPorDependente = Num(c[21]),
                DeducaoAposentadoMaior65 = Num(c[22]),
            };
        }
        throw new InvalidOperationException($"Sem tabela IRPF para {competencia}.");
    }

    private static FaixaFiscal? Faixa(string inicial, string final, string aliquota, string deducao) =>
        string.IsNullOrWhiteSpace(aliquota)
            ? null
            : new FaixaFiscal(Num(inicial), Opcional(final), Num(aliquota), Num(deducao));

    private static DateOnly Data(string s) =>
        DateOnly.ParseExact(s.Trim().Trim('"'), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static decimal Num(string s) =>
        string.IsNullOrWhiteSpace(s) ? 0m : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static decimal? Opcional(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
