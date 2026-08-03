using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói o cenário do harness <c>tools/golden/GoldenGenApuracaoJuros.java</c> (motor Java
/// dirigido via reflection sobre <c>apurarJurosDasVerbas</c>, regime fixo 1% a.m.): agrupamento
/// das ocorrências por competência + início dos juros, capital corrigido, taxa acumulada, juros
/// por balde e os totais que alimentam o bruto do reclamante.
/// </summary>
public sealed class ApuracaoDeJurosGoldenTests
{
    private const decimal Tolerancia = 1e-10m;

    private static readonly DateOnly Ajuizamento = new(2019, 6, 1);
    private static readonly DateOnly Liquidacao = new(2021, 6, 1);

    /// <summary>Provider vazio: o regime fixo de 1% não consulta faixas.</summary>
    private sealed class SemFaixas : IJurosFaixaProvider
    {
        public IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim) => [];
    }

    [Fact]
    public void Golden_da_apuracao_de_juros_bate_com_o_motor_oficial()
    {
        var tabela = new TabelaDeJurosService(new SemFaixas());
        var contexto = new ContextoDeApuracaoDeJuros
        {
            DataAjuizamento = Ajuizamento,
            FasePreJudicial = false,
            TaxaAcumuladaAPartirDe = dia =>
                tabela.CalcularTaxaAcumulada(JurosEnum.JurosUmPorcento, dia, Liquidacao),
        };

        var resultado = ApuradorDeJuros.Apurar(Verbas(), contexto);

        var calculado = new Dictionary<string, decimal>();
        foreach (var ap in resultado.Apuracoes)
        {
            var balde = $"balde_{ap.Competencia:yyyyMM}_{ap.DataInicial:yyyyMMdd}";
            calculado[$"{balde};taxa"] = ap.TaxaDeJuros;
            calculado[$"{balde};corrigido"] = ap.ValorCorrigido;
            calculado[$"{balde};juros"] = ap.Juros;
        }
        calculado["TOTAIS;corrigido"] = resultado.TotalDeValorCorrigido;
        calculado["TOTAIS;juros"] = resultado.TotalDeJuros;

        var golden = LerGolden();
        Assert.NotEmpty(golden);
        foreach (var (chave, esperado) in golden)
        {
            Assert.True(calculado.ContainsKey(chave), $"balde/chave não reproduzido: {chave}");
            var obtido = calculado[chave];
            var limite = Tolerancia * Math.Max(1m, Math.Abs(esperado));
            Assert.True(Math.Abs(obtido - esperado) <= limite,
                $"{chave}: esperado {esperado}, obtido {obtido}");
        }
    }

    private static List<VerbaEmCalculo> Verbas()
    {
        var verbas = new List<VerbaEmCalculo>();

        var a = NovaVerba("A", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        AddOcorrencia(a, new(2019, 3, 1), new(2019, 3, 31), 1000.00m, 0m, 1.2m);
        AddOcorrencia(a, new(2019, 8, 1), new(2019, 8, 31), 500.00m, 0m, 1.1m);
        verbas.Add(a);

        var b = NovaVerba("B", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        AddOcorrencia(b, new(2019, 3, 1), new(2019, 3, 31), 200.00m, 0m, 1.0m);
        verbas.Add(b);

        var c = NovaVerba("C", CaracteristicaDaVerbaEnum.Ferias, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        AddOcorrencia(c, new(2020, 1, 1), new(2020, 1, 31), 3000.00m, 500.00m, 1.05m);
        verbas.Add(c);

        var d = NovaVerba("D", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidasEVincendas);
        AddOcorrencia(d, new(2020, 5, 1), new(2020, 5, 31), 800.00m, 0m, 1.0m);
        verbas.Add(d);

        var e = NovaVerba("E", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        e.ComporPrincipal = false;
        AddOcorrencia(e, new(2019, 3, 1), new(2019, 3, 31), 9999.00m, 0m, 1.0m);
        verbas.Add(e);

        return verbas;
    }

    private static VerbaEmCalculo NovaVerba(
        string nome, CaracteristicaDaVerbaEnum caracteristica, JurosDoAjuizamentoEnum juros) =>
        new()
        {
            Nome = nome,
            Tipo = TipoDaVerbaEnum.Informada,
            Caracteristica = caracteristica,
            ComporPrincipal = true,
            JurosDoAjuizamento = juros,
        };

    private static void AddOcorrencia(
        VerbaEmCalculo verba, DateOnly inicio, DateOnly fim, decimal devido, decimal pago, decimal indice) =>
        verba.Ocorrencias.Add(new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = inicio,
            DataFinal = fim,
            Devido = devido,
            Pago = pago,
            IndiceAcumulado = indice,
            Ativo = true,
        });

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_apuracao_juros.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(
                c => $"{c[0]};{c[1]}",
                c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
