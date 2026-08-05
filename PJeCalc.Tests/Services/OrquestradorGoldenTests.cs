using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services;
using PJeCalc.Core.Services.Custas;
using PJeCalc.Core.Services.Honorarios;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Core.Services.Verbas;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói o cenário do harness <c>tools/golden/GoldenGenOrquestrador.java</c> (caminho mínimo
/// do motor: verbas → apuração de juros → multas → bruto → honorários → custas) e confere o
/// <see cref="MotorDeCalculo"/> ponta a ponta.
/// </summary>
public sealed class OrquestradorGoldenTests
{
    private const decimal Tolerancia = 1e-10m;
    private static readonly DateOnly Ajuizamento = new(2019, 6, 1);
    private static readonly DateOnly Liquidacao = new(2021, 6, 1);

    private sealed class SemFaixas : IJurosFaixaProvider
    {
        public IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim) => [];
    }

    [Fact]
    public void Golden_do_orquestrador_bate_com_o_motor_oficial()
    {
        var resultado = MotorDeCalculo.Calcular(Verbas(), Configuracao());

        var hon = resultado.Honorarios.Single();
        var calculado = new Dictionary<string, decimal>
        {
            ["APURACAO;totalCorrigido"] = resultado.ApuracaoDeJuros.TotalDeValorCorrigido,
            ["APURACAO;totalJuros"] = resultado.ApuracaoDeJuros.TotalDeJuros,
            ["MULTAS;reclamanteReclamado"] = resultado.TotaisDeMultas.ReclamanteReclamado,
            ["MULTAS;reclamadoReclamante"] = resultado.TotaisDeMultas.ReclamadoReclamante,
            ["BRUTO;brutoDevidoAoReclamante"] = resultado.BrutoDevidoAoReclamante,
            ["HONORARIO;valorTotal"] = hon.ValorTotal,
            ["HONORARIO;devidoPeloReclamado"] = resultado.TotaisDeHonorarios.DevidoPeloReclamado,
            ["CUSTAS;base"] = resultado.Custas!.BaseCalculada,
            ["CUSTAS;consolidado"] = resultado.Custas!.ConsolidadoReclamado,
        };

        var golden = LerGolden();
        Assert.NotEmpty(golden);
        foreach (var (chave, esperado) in golden)
        {
            Assert.True(calculado.ContainsKey(chave), $"chave não reproduzida: {chave}");
            var limite = Tolerancia * Math.Max(1m, Math.Abs(esperado));
            Assert.True(Math.Abs(calculado[chave] - esperado) <= limite,
                $"{chave}: esperado {esperado}, obtido {calculado[chave]}");
        }
    }

    private static ConfiguracaoDoCalculo Configuracao()
    {
        var tabela = new TabelaDeJurosService(new SemFaixas());
        using var contexto = ReferenciaDbContextFactory.Criar();
        var parametrosCustas = new EfParametroDeCustasProvider(contexto).ObterPorData(Liquidacao);

        return new ConfiguracaoDoCalculo
        {
            Juros = new ContextoDeApuracaoDeJuros
            {
                DataAjuizamento = Ajuizamento,
                TaxaAcumuladaAPartirDe = dia =>
                    tabela.CalcularTaxaAcumulada(JurosEnum.JurosUmPorcento, dia, Liquidacao),
            },
            Multas =
            [
                new ParametrosDaMulta
                {
                    CredorDevedor = CredorDevedorMultaEnum.ReclamanteReclamado,
                    Base = BaseParaApuracaoDeMultaEnum.Principal,
                    Aliquota = 10m,
                },
                new ParametrosDaMulta
                {
                    CredorDevedor = CredorDevedorMultaEnum.ReclamadoReclamante,
                    TipoValor = TipoValorEnum.Informado,
                    ValorInformado = 500.00m,
                },
            ],
            Honorarios =
            [
                new ParametrosDoHonorario
                {
                    Devedor = TipoDeDevedorDoHonorarioEnum.Reclamado,
                    Base = BaseParaApuracaoDeHonorarioEnum.Bruto,
                    Aliquota = 10m,
                },
            ],
            Custas = new ParametrosDeCustas
            {
                Parametros = parametrosCustas,
                BaseCalculada = BaseParaCustasCalculadasEnum.BrutoDevidoAoReclamante,
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                TetoConhecimento = 4m * 6433.57m, // 4× teto RGPS 01/2021 (ajuizamento pós-Reforma)
            },
        };
    }

    private static List<VerbaEmCalculo> Verbas()
    {
        var verbas = new List<VerbaEmCalculo>();

        var a = Nova("A", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(a, new(2019, 3, 1), new(2019, 3, 31), 1000.00m, 0m, 1.2m);
        Add(a, new(2019, 8, 1), new(2019, 8, 31), 500.00m, 0m, 1.1m);
        verbas.Add(a);

        var b = Nova("B", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(b, new(2019, 3, 1), new(2019, 3, 31), 200.00m, 0m, 1.0m);
        verbas.Add(b);

        var c = Nova("C", CaracteristicaDaVerbaEnum.Ferias, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(c, new(2020, 1, 1), new(2020, 1, 31), 3000.00m, 500.00m, 1.05m);
        verbas.Add(c);

        var d = Nova("D", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidasEVincendas);
        Add(d, new(2020, 5, 1), new(2020, 5, 31), 800.00m, 0m, 1.0m);
        verbas.Add(d);

        return verbas;
    }

    private static VerbaEmCalculo Nova(
        string nome, CaracteristicaDaVerbaEnum caracteristica, JurosDoAjuizamentoEnum juros) =>
        new() { Nome = nome, Tipo = TipoDaVerbaEnum.Informada, Caracteristica = caracteristica, JurosDoAjuizamento = juros };

    private static void Add(
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
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_orquestrador.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => $"{c[0]};{c[1]}", c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
