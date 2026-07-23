using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói os 17 cenários do harness <c>tools/golden/GoldenGenVerbasPipeline.java</c>
/// (motor Java oficial dirigido headless) e compara ocorrência a ocorrência: datas,
/// termos, integrais, devido/pago, diferença corrigida e totais.
/// O índice de correção estubado é <c>1 + 0,01 × ((ano − 2019) × 12 + mês)</c>.
/// </summary>
public sealed class VerbasPipelineCasos
{
    public IReadOnlyDictionary<string, VerbaEmCalculo> Casos { get; }

    private static decimal Indice(DateOnly data) =>
        1m + 0.01m * ((data.Year - 2019) * 12 + data.Month);

    public VerbasPipelineCasos()
    {
        var casos = new Dictionary<string, VerbaEmCalculo>();

        // ---- contexto padrão: admissão 2020-05-01, sem demissão ----
        var contextoPadrao = new ContextoDeVerbas
        {
            DataAdmissao = new DateOnly(2020, 5, 1),
            DataDeLiquidacao = new DateOnly(2022, 3, 31),
            ValorUltimaRemuneracao = 3300.00m,
            IndiceAcumulado = Indice,
        };
        var motorPadrao = new MotorDeVerbas(contextoPadrao);

        casos["INF_PROP"] = Executar(motorPadrao, new VerbaEmCalculo
        {
            Nome = "inf-prop",
            Tipo = TipoDaVerbaEnum.Informada,
            PeriodoInicial = new DateOnly(2021, 1, 15),
            PeriodoFinal = new DateOnly(2021, 4, 10),
            AplicarProporcionalidade = true,
            Constante = 3000.00m,
            ValorPago = { ValorInformado = 500.00m, AplicarProporcionalidade = true },
        });

        casos["INF_CHEIA"] = Executar(motorPadrao, new VerbaEmCalculo
        {
            Nome = "inf-cheia",
            Tipo = TipoDaVerbaEnum.Informada,
            PeriodoInicial = new DateOnly(2021, 1, 15),
            PeriodoFinal = new DateOnly(2021, 3, 10),
            Constante = 2500.00m,
        });

        var calcUr = new VerbaEmCalculo
        {
            Nome = "calc-ur",
            Tipo = TipoDaVerbaEnum.Calculada,
            PeriodoInicial = new DateOnly(2021, 1, 10),
            PeriodoFinal = new DateOnly(2021, 5, 20),
            BaseTabelada = new TermoBaseTabelada
            {
                Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao,
                AplicarProporcionalidade = true,
            },
            Divisor = { OutroValor = 220m },
            Multiplicador = 1.5m,
            Quantidade = { ValorInformado = 20m, AplicarProporcionalidade = true },
            ValorPago = { ValorInformado = 100.00m },
        };
        casos["CALC_UR"] = Executar(motorPadrao, calcUr);

        casos["CALC_HS"] = Executar(motorPadrao, ComHistorico(new VerbaEmCalculo
        {
            Nome = "calc-hs",
            Tipo = TipoDaVerbaEnum.Calculada,
            PeriodoInicial = new DateOnly(2021, 1, 1),
            PeriodoFinal = new DateOnly(2021, 4, 30),
            BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.HistoricoSalarial },
            Divisor = { OutroValor = 30m },
            Quantidade = { ValorInformado = 30m },
        }, new Dictionary<DateOnly, decimal>
        {
            [new DateOnly(2021, 1, 1)] = 3000.00m,
            [new DateOnly(2021, 2, 1)] = 3000.00m,
            [new DateOnly(2021, 3, 1)] = 3300.00m,
            // abril sem ocorrência de propósito -> base zero
        }));

        // ---- 13º com avos ----
        casos["DEZ_AVOS"] = Executar(
            new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2019, 3, 20),
                DataDemissao = new DateOnly(2021, 12, 28),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
            }),
            DecimoTerceiroComAvos("dez-avos", new DateOnly(2019, 3, 20), new DateOnly(2021, 12, 28)));

        casos["DEZ_DEMISSAO_JUL"] = Executar(
            new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2019, 3, 20),
                DataDemissao = new DateOnly(2021, 7, 15),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
            }),
            DecimoTerceiroComAvos("dez-jul", new DateOnly(2019, 3, 20), new DateOnly(2021, 7, 15)));

        // ---- saldo de salário no desligamento ----
        casos["DESLIG"] = Executar(
            new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 5, 1),
                DataDemissao = new DateOnly(2021, 8, 17),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
            }),
            new VerbaEmCalculo
            {
                Nome = "deslig",
                Tipo = TipoDaVerbaEnum.Calculada,
                OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.Desligamento,
                PeriodoInicial = new DateOnly(2020, 5, 1),
                PeriodoFinal = new DateOnly(2021, 8, 17),
                BaseTabelada = new TermoBaseTabelada
                {
                    Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao,
                    AplicarProporcionalidade = true,
                },
                Divisor = { OutroValor = 30m },
                Quantidade = { ValorInformado = 30m, AplicarProporcionalidade = true },
            });

        // ---- reflexo valor mensal sobre a diferença de CALC_UR ----
        casos["REFLEXO_VM"] = Executar(motorPadrao, new VerbaEmCalculo
        {
            Nome = "reflexo-vm",
            Tipo = TipoDaVerbaEnum.Reflexo,
            PeriodoInicial = new DateOnly(2021, 1, 10),
            PeriodoFinal = new DateOnly(2021, 5, 20),
            Divisor = { OutroValor = 6m },
            Quantidade = { ValorInformado = 1m },
            BasesVerba = { new ItemBaseVerba(calcUr) },
        });

        casos["REFLEXO_VM_DOBRA"] = Executar(motorPadrao, new VerbaEmCalculo
        {
            Nome = "reflexo-dobra",
            Tipo = TipoDaVerbaEnum.Reflexo,
            PeriodoInicial = new DateOnly(2021, 2, 1),
            PeriodoFinal = new DateOnly(2021, 3, 31),
            Divisor = { OutroValor = 6m },
            Quantidade = { ValorInformado = 1m },
            Dobra = true,
            BasesVerba = { new ItemBaseVerba(calcUr) },
        });

        // ---- médias pelo valor (ano civil), um subcaso por tratamento da fração ----
        foreach (var (tratamento, nome) in new[]
        {
            (TratamentoDaFracaoDeMesDoReflexoEnum.Manter, "REFLEXO_MV_MANTER"),
            (TratamentoDaFracaoDeMesDoReflexoEnum.Desprezar, "REFLEXO_MV_DESPREZAR"),
            (TratamentoDaFracaoDeMesDoReflexoEnum.DesprezarMenorQue15Dias, "REFLEXO_MV_DMQ15"),
            (TratamentoDaFracaoDeMesDoReflexoEnum.Integralizar, "REFLEXO_MV_INTEGRALIZAR"),
        })
        {
            var motor = new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 1, 10),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                IndiceAcumulado = Indice,
            });
            var origem = Executar(motor, new VerbaEmCalculo
            {
                Nome = "origem-he",
                Tipo = TipoDaVerbaEnum.Informada,
                PeriodoInicial = new DateOnly(2021, 1, 15),
                PeriodoFinal = new DateOnly(2021, 12, 31),
                AplicarProporcionalidade = true,
                Constante = 1200.00m,
            });
            casos[nome] = Executar(motor, new VerbaEmCalculo
            {
                Nome = "reflexo-13-media",
                Tipo = TipoDaVerbaEnum.Reflexo,
                Caracteristica = CaracteristicaDaVerbaEnum.DecimoTerceiroSalario,
                OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.Dezembro,
                PeriodoInicial = new DateOnly(2021, 1, 1),
                PeriodoFinal = new DateOnly(2021, 12, 31),
                ComportamentoDoReflexo = ComportamentoDoReflexoEnum.MediaPeloValor,
                PeriodoDaMedia = PeriodoDaMediaDoReflexoEnum.AnoCivil,
                TratamentoDaFracaoDeMes = tratamento,
                Divisor = { OutroValor = 12m },
                Quantidade = { ValorInformado = 12m },
                BasesVerba = { new ItemBaseVerba(origem) },
            });
        }

        // ---- pago maior que o devido, com e sem zerar a diferença negativa ----
        foreach (var (zerar, nome) in new[] { (true, "CALC_ZERA"), (false, "CALC_NOZERA") })
        {
            casos[nome] = Executar(motorPadrao, new VerbaEmCalculo
            {
                Nome = nome,
                Tipo = TipoDaVerbaEnum.Calculada,
                PeriodoInicial = new DateOnly(2021, 1, 10),
                PeriodoFinal = new DateOnly(2021, 3, 31),
                ZeraValorNegativo = zerar,
                BaseTabelada = new TermoBaseTabelada
                {
                    Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao,
                    AplicarProporcionalidade = true,
                },
                Divisor = { OutroValor = 220m },
                Multiplicador = 1.5m,
                Quantidade = { ValorInformado = 20m, AplicarProporcionalidade = true },
                ValorPago = { ValorInformado = 300.00m },
            });
        }

        // ---- divisor zero desativa a ocorrência ----
        casos["DIV0"] = Executar(
            new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 5, 1),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
            }),
            new VerbaEmCalculo
            {
                Nome = "div0",
                Tipo = TipoDaVerbaEnum.Calculada,
                PeriodoInicial = new DateOnly(2021, 1, 1),
                PeriodoFinal = new DateOnly(2021, 2, 28),
                BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
                Divisor = { OutroValor = 0m },
                Quantidade = { ValorInformado = 1m },
            });

        // ---- média pelos últimos 12 meses do contrato (aviso prévio típico) ----
        {
            var motor = new MotorDeVerbas(new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 1, 10),
                DataDemissao = new DateOnly(2021, 10, 20),
                DataDeLiquidacao = new DateOnly(2022, 3, 31),
                IndiceAcumulado = Indice,
            });
            var origem = Executar(motor, new VerbaEmCalculo
            {
                Nome = "origem-he-dm",
                Tipo = TipoDaVerbaEnum.Informada,
                PeriodoInicial = new DateOnly(2020, 6, 1),
                PeriodoFinal = new DateOnly(2021, 10, 20),
                AplicarProporcionalidade = true,
                Constante = 900.00m,
            });
            casos["ORIGEM_DM"] = origem;
            casos["REFLEXO_MV_DM"] = Executar(motor, new VerbaEmCalculo
            {
                Nome = "reflexo-aviso-media",
                Tipo = TipoDaVerbaEnum.Reflexo,
                OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.Desligamento,
                PeriodoInicial = new DateOnly(2020, 6, 1),
                PeriodoFinal = new DateOnly(2021, 10, 20),
                ComportamentoDoReflexo = ComportamentoDoReflexoEnum.MediaPeloValor,
                PeriodoDaMedia = PeriodoDaMediaDoReflexoEnum.UltimosDozeMesesDoContrato,
                Divisor = { OutroValor = 30m },
                Quantidade = { ValorInformado = 30m },
                BasesVerba = { new ItemBaseVerba(origem) },
            });
        }

        Casos = casos;
    }

    private static VerbaEmCalculo Executar(MotorDeVerbas motor, VerbaEmCalculo verba)
    {
        motor.GerarOcorrencias(verba);
        motor.Liquidar(verba);
        return verba;
    }

    private static VerbaEmCalculo ComHistorico(VerbaEmCalculo verba, Dictionary<DateOnly, decimal> salarios)
    {
        verba.HistoricosDaBase.Add(new VinculoDeHistoricoSalarial(salarios));
        return verba;
    }

    private static VerbaEmCalculo DecimoTerceiroComAvos(string nome, DateOnly inicio, DateOnly fim) => new()
    {
        Nome = nome,
        Tipo = TipoDaVerbaEnum.Calculada,
        Caracteristica = CaracteristicaDaVerbaEnum.DecimoTerceiroSalario,
        OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.Dezembro,
        PeriodoInicial = inicio,
        PeriodoFinal = fim,
        BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
        Divisor = { OutroValor = 12m },
        Quantidade = { Tipo = TipoDeQuantidadeEnum.Avos },
    };
}

public class VerbasPipelineGoldenTests(VerbasPipelineCasos casos) : IClassFixture<VerbasPipelineCasos>
{
    private readonly VerbasPipelineCasos _casos = casos;

    private static readonly string[] Linhas =
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_verbas_pipeline.csv"));

    public static IEnumerable<object[]> LinhasDeOcorrencia()
    {
        var indicePorCaso = new Dictionary<string, int>();
        foreach (var linha in Linhas.Where(l => l.StartsWith("OC;")))
        {
            var caso = linha.Split(';')[1];
            indicePorCaso.TryGetValue(caso, out var indice);
            indicePorCaso[caso] = indice + 1;
            yield return [caso, indice, linha];
        }
    }

    public static IEnumerable<object[]> LinhasDeTotais() =>
        Linhas.Where(l => l.StartsWith("TOT;")).Select(l => (object[])[l.Split(';')[1], l]);

    [Theory]
    [MemberData(nameof(LinhasDeOcorrencia))]
    public void Ocorrencia_bate_com_o_motor_oficial(string caso, int indice, string linha)
    {
        var campos = linha.Split(';');
        var verba = _casos.Casos[caso];
        Assert.True(indice < verba.Ocorrencias.Count, $"{caso}: esperava ao menos {indice + 1} ocorrências.");
        var o = verba.Ocorrencias[indice];

        Assert.Equal(DateOnly.Parse(campos[2]), o.DataInicial);
        Assert.Equal(DateOnly.Parse(campos[3]), o.DataFinal);
        Assert.Equal(campos[4] == "1", o.Ativo);
        Comparar(caso, indice, "base", campos[5], o.Base);
        Comparar(caso, indice, "divisor", campos[6], o.Divisor);
        Comparar(caso, indice, "multiplicador", campos[7], o.Multiplicador);
        Comparar(caso, indice, "quantidade", campos[8], o.Quantidade);
        Comparar(caso, indice, "quantidadeIntegral", campos[9], o.QuantidadeIntegral);
        Comparar(caso, indice, "devido", campos[10], o.Devido);
        Comparar(caso, indice, "devidoIntegral", campos[11], o.DevidoIntegral);
        Comparar(caso, indice, "pago", campos[12], o.Pago);
        Comparar(caso, indice, "pagoIntegral", campos[13], o.PagoIntegral);
        Assert.Equal(campos[14] == "1", o.Dobra);
        Comparar(caso, indice, "indiceAcumulado", campos[15], o.IndiceAcumulado);
        Comparar(caso, indice, "diferenca", campos[16], o.Diferenca);
        Comparar(caso, indice, "diferencaCorrigida", campos[17], o.DiferencaCorrigida);
    }

    [Theory]
    [MemberData(nameof(LinhasDeTotais))]
    public void Totais_batem_com_o_motor_oficial(string caso, string linha)
    {
        var campos = linha.Split(';');
        var verba = _casos.Casos[caso];
        var quantidadeEsperada = Linhas.Count(l => l.StartsWith($"OC;{caso};"));
        Assert.Equal(quantidadeEsperada, verba.Ocorrencias.Count);

        var totais = TotaisDaVerba.Calcular(verba);
        Comparar(caso, -1, "totalDevido", campos[2], totais.Devido);
        Comparar(caso, -1, "totalPago", campos[3], totais.Pago);
        Comparar(caso, -1, "totalDiferenca", campos[4], totais.Diferenca);
        Comparar(caso, -1, "totalDiferencaCorrigida", campos[5], totais.DiferencaCorrigida);
        Comparar(caso, -1, "totalIncidencias", campos[6], totais.DiferencaCorrigidaParaCalculoDasIncidencias);
    }

    /// <summary>Compara com tolerância relativa 1e-10 (campos não arredondados têm dízimas).</summary>
    private static void Comparar(string caso, int indice, string campo, string esperadoTexto, decimal? obtido)
    {
        if (esperadoTexto.Length == 0)
        {
            Assert.True(obtido is null, $"{caso}[{indice}].{campo}: esperava nulo, obteve {obtido}.");
            return;
        }
        Assert.True(obtido is not null, $"{caso}[{indice}].{campo}: esperava {esperadoTexto}, obteve nulo.");

        var esperado = decimal.Parse(esperadoTexto, System.Globalization.CultureInfo.InvariantCulture);
        var escala = Math.Max(Math.Abs(esperado), 1m);
        Assert.True(Math.Abs(obtido.Value - esperado) <= escala * 0.0000000001m,
            $"{caso}[{indice}].{campo}: esperava {esperado}, obteve {obtido}.");
    }
}
