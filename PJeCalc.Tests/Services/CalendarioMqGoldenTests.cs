using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói os cenários do harness <c>tools/golden/GoldenGenCalendarioMq.java</c>
/// (motor Java com feriados injetados via contexto Seam mínimo): contagens de
/// calendário, carga horária, verba com divisor/quantidade de calendário e de cartão
/// de ponto, e média pela quantidade dos reflexos.
/// </summary>
public sealed class CalendarioMqCasos
{
    public IReadOnlyDictionary<string, VerbaEmCalculo> Casos { get; }

    private static readonly HashSet<DateOnly> Feriados2021 =
    [
        new(2021, 1, 1), new(2021, 2, 16), new(2021, 4, 21), new(2021, 5, 1), new(2021, 6, 3),
        new(2021, 9, 7), new(2021, 10, 12), new(2021, 11, 2), new(2021, 11, 15), new(2021, 12, 25),
    ];

    public static bool EhFeriado(DateOnly data) => Feriados2021.Contains(data);

    private static decimal Indice(DateOnly data) =>
        1m + 0.01m * ((data.Year - 2019) * 12 + data.Month);

    private static readonly (DateOnly Competencia, decimal Valor)[] Cartao2021 =
    [
        (new(2021, 1, 1), 10m), (new(2021, 2, 1), 12m), (new(2021, 3, 1), 14m),
        (new(2021, 4, 1), 16m), (new(2021, 5, 1), 18m), (new(2021, 6, 1), 20m),
        (new(2021, 7, 1), 11m), (new(2021, 8, 1), 13m), (new(2021, 9, 1), 15m),
        (new(2021, 10, 1), 17m), (new(2021, 11, 1), 19m), (new(2021, 12, 1), 21m),
    ];

    public CalendarioMqCasos()
    {
        var casos = new Dictionary<string, VerbaEmCalculo>();

        // ---- DSR: divisor DIAS_UTEIS + quantidade REPOUSOS ----
        {
            var contexto = ContextoPadrao();
            casos["DSR"] = Executar(new MotorDeVerbas(contexto), new VerbaEmCalculo
            {
                Nome = "dsr",
                Tipo = TipoDaVerbaEnum.Calculada,
                PeriodoInicial = new DateOnly(2021, 4, 1),
                PeriodoFinal = new DateOnly(2021, 6, 30),
                BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
                Divisor = { Tipo = DivisorDeVerbaEnum.DiasUteis },
                Quantidade =
                {
                    Tipo = TipoDeQuantidadeEnum.ImportadaDoCalendario,
                    TipoImportadaDoCalendario = TipoDeQuantidadeImportadaDoCalendarioEnum.Repousos,
                },
            });
        }

        // ---- DSR com sábado não útil, falta NJ num fim de semana e férias gozadas ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 1, 10),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 2200.00m,
                IndiceAcumulado = Indice,
                EhFeriado = EhFeriado,
                SabadoDiaUtil = false,
                Faltas = { new FaltaDoCalculo(new DateOnly(2021, 4, 3), new DateOnly(2021, 4, 11)) },
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = new PeriodoDeApuracao(new DateOnly(2020, 1, 10), new DateOnly(2021, 1, 9)),
                        PeriodoConcessivo = new PeriodoDeApuracao(new DateOnly(2021, 1, 10), new DateOnly(2022, 1, 9)),
                        Situacao = SituacaoDaFeriasEnum.GozadasParcialmente,
                        PeriodoDeGozo1 = new PeriodoDeApuracao(new DateOnly(2021, 5, 10), new DateOnly(2021, 5, 23)),
                    },
                },
            };
            casos["DSR_EXCLUSOES"] = Executar(new MotorDeVerbas(contexto), new VerbaEmCalculo
            {
                Nome = "dsr-exclusoes",
                Tipo = TipoDaVerbaEnum.Calculada,
                PeriodoInicial = new DateOnly(2021, 4, 1),
                PeriodoFinal = new DateOnly(2021, 5, 31),
                ExcluirFaltaNaoJustificada = true,
                ExcluirFeriasGozadas = true,
                BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
                Divisor = { Tipo = DivisorDeVerbaEnum.DiasUteis },
                Quantidade =
                {
                    Tipo = TipoDeQuantidadeEnum.ImportadaDoCalendario,
                    TipoImportadaDoCalendario = TipoDeQuantidadeImportadaDoCalendarioEnum.RepousosFeriados,
                },
            });
        }

        // ---- Cartão de ponto no divisor e na quantidade ----
        {
            var verba = new VerbaEmCalculo
            {
                Nome = "he-cartao",
                Tipo = TipoDaVerbaEnum.Calculada,
                PeriodoInicial = new DateOnly(2021, 1, 1),
                PeriodoFinal = new DateOnly(2021, 3, 31),
                BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
                Divisor = { Tipo = DivisorDeVerbaEnum.ImportadaDoCartao },
                Multiplicador = 1.5m,
                Quantidade = { Tipo = TipoDeQuantidadeEnum.ImportadaDoCartao },
            };
            verba.CartoesDoDivisor.Add(new CartaoDePontoDaVerba("horas-mes",
                [(new(2021, 1, 1), 200m), (new(2021, 2, 1), 210m), (new(2021, 3, 1), 220m)]));
            verba.CartoesDaQuantidade.Add(new CartaoDePontoDaVerba("horas-extras",
                [(new(2021, 1, 1), 10m), (new(2021, 2, 1), 12.5m), (new(2021, 3, 1), 8m)]));
            casos["CARTAO"] = Executar(new MotorDeVerbas(ContextoPadrao()), verba);
        }

        // ---- MQ sobre devido / diferença / tratamentos / abatimento ----
        casos["MQ_DEVIDO"] = MqDecimoTerceiro(inicioOrigem: new DateOnly(2021, 1, 1),
            pago: 0m, gerar: TipoDeGeracaoEnum.Devido, TratamentoDaFracaoDeMesDoReflexoEnum.Manter,
            zeraNegativo: true, Cartao2021);
        casos["MQ_DIFERENCA"] = MqDecimoTerceiro(new DateOnly(2021, 1, 1),
            pago: 100m, TipoDeGeracaoEnum.Diferenca, TratamentoDaFracaoDeMesDoReflexoEnum.Manter,
            zeraNegativo: true, Cartao2021);
        casos["MQ_DESPREZAR"] = MqDecimoTerceiro(new DateOnly(2021, 1, 15),
            pago: 0m, TipoDeGeracaoEnum.Devido, TratamentoDaFracaoDeMesDoReflexoEnum.Desprezar,
            zeraNegativo: true, Cartao2021);
        casos["MQ_DMQ15"] = MqDecimoTerceiro(new DateOnly(2021, 1, 15),
            pago: 0m, TipoDeGeracaoEnum.Devido, TratamentoDaFracaoDeMesDoReflexoEnum.DesprezarMenorQue15Dias,
            zeraNegativo: true, Cartao2021);
        {
            var cartao = Cartao2021.ToArray();
            cartao[2] = (new DateOnly(2021, 3, 1), 0m); // março: devido zero, pago 100 -> abatimento
            casos["MQ_ABATIMENTO"] = MqDecimoTerceiro(new DateOnly(2021, 1, 1),
                pago: 100m, TipoDeGeracaoEnum.Diferenca, TratamentoDaFracaoDeMesDoReflexoEnum.Manter,
                zeraNegativo: false, cartao);
        }

        // ---- MQ em férias (janela do período aquisitivo) ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 6, 1),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 2200.00m,
                IndiceAcumulado = Indice,
                EhFeriado = EhFeriado,
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = new PeriodoDeApuracao(new DateOnly(2020, 6, 1), new DateOnly(2021, 5, 31)),
                        PeriodoConcessivo = new PeriodoDeApuracao(new DateOnly(2021, 6, 1), new DateOnly(2022, 5, 31)),
                        PeriodoDeGozo1 = new PeriodoDeApuracao(new DateOnly(2021, 8, 1), new DateOnly(2021, 8, 30)),
                    },
                },
            };
            var motor = new MotorDeVerbas(contexto);
            var origem = OrigemHeComCartao(new DateOnly(2020, 6, 1), new DateOnly(2022, 3, 10), 0m,
            [
                (new(2020, 6, 1), 10m), (new(2020, 7, 1), 12m), (new(2020, 8, 1), 14m),
                (new(2020, 9, 1), 16m), (new(2020, 10, 1), 18m), (new(2020, 11, 1), 20m),
                (new(2020, 12, 1), 11m), (new(2021, 1, 1), 13m), (new(2021, 2, 1), 15m),
                (new(2021, 3, 1), 17m), (new(2021, 4, 1), 19m), (new(2021, 5, 1), 21m),
                (new(2021, 6, 1), 9m), (new(2021, 7, 1), 8m), (new(2021, 8, 1), 7m),
            ]);
            origem.GerarReflexo = TipoDeGeracaoEnum.Devido;
            Executar(motor, origem);

            casos["MQ_FERIAS"] = Executar(motor, new VerbaEmCalculo
            {
                Nome = "reflexo-ferias-mq",
                Tipo = TipoDaVerbaEnum.Reflexo,
                Caracteristica = CaracteristicaDaVerbaEnum.Ferias,
                OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.PeriodoAquisitivo,
                PeriodoInicial = new DateOnly(2020, 6, 1),
                PeriodoFinal = new DateOnly(2022, 3, 10),
                ComportamentoDoReflexo = ComportamentoDoReflexoEnum.MediaPelaQuantidade,
                PeriodoDaMedia = PeriodoDaMediaDoReflexoEnum.PeriodoAquisitivo,
                BasesVerba = { new ItemBaseVerba(origem) },
            });
        }

        Casos = casos;
    }

    private static ContextoDeVerbas ContextoPadrao() => new()
    {
        DataAdmissao = new DateOnly(2020, 1, 10),
        DataDeLiquidacao = new DateOnly(2022, 6, 30),
        ValorUltimaRemuneracao = 2200.00m,
        IndiceAcumulado = Indice,
        EhFeriado = EhFeriado,
    };

    private static VerbaEmCalculo OrigemHeComCartao(
        DateOnly inicio, DateOnly fim, decimal pago, (DateOnly, decimal)[] cartao)
    {
        var origem = new VerbaEmCalculo
        {
            Nome = "he-mq",
            Tipo = TipoDaVerbaEnum.Calculada,
            PeriodoInicial = inicio,
            PeriodoFinal = fim,
            BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
            Divisor = { OutroValor = 220m },
            Multiplicador = 1.5m,
            Quantidade = { Tipo = TipoDeQuantidadeEnum.ImportadaDoCartao },
            ValorPago = { ValorInformado = pago },
        };
        origem.CartoesDaQuantidade.Add(new CartaoDePontoDaVerba("cartao-mq", cartao));
        return origem;
    }

    private VerbaEmCalculo MqDecimoTerceiro(
        DateOnly inicioOrigem, decimal pago, TipoDeGeracaoEnum gerar,
        TratamentoDaFracaoDeMesDoReflexoEnum tratamento, bool zeraNegativo,
        (DateOnly, decimal)[] cartao)
    {
        var motor = new MotorDeVerbas(ContextoPadrao());
        var origem = OrigemHeComCartao(inicioOrigem, new DateOnly(2021, 12, 31), pago, cartao);
        origem.GerarReflexo = gerar;
        origem.ZeraValorNegativo = zeraNegativo;
        Executar(motor, origem);

        return Executar(motor, new VerbaEmCalculo
        {
            Nome = "reflexo-13-mq",
            Tipo = TipoDaVerbaEnum.Reflexo,
            Caracteristica = CaracteristicaDaVerbaEnum.DecimoTerceiroSalario,
            OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.Dezembro,
            PeriodoInicial = new DateOnly(2021, 1, 1),
            PeriodoFinal = new DateOnly(2021, 12, 31),
            ComportamentoDoReflexo = ComportamentoDoReflexoEnum.MediaPelaQuantidade,
            PeriodoDaMedia = PeriodoDaMediaDoReflexoEnum.AnoCivil,
            TratamentoDaFracaoDeMes = tratamento,
            Divisor = { OutroValor = 12m },
            Quantidade = { ValorInformado = 12m },
            BasesVerba = { new ItemBaseVerba(origem) },
        });
    }

    private static VerbaEmCalculo Executar(MotorDeVerbas motor, VerbaEmCalculo verba)
    {
        motor.GerarOcorrencias(verba);
        motor.Liquidar(verba);
        return verba;
    }
}

public class CalendarioMqGoldenTests(CalendarioMqCasos casos) : IClassFixture<CalendarioMqCasos>
{
    private readonly CalendarioMqCasos _casos = casos;

    private static readonly string[] Linhas =
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_calendario_mq.csv"));

    public static IEnumerable<object[]> LinhasDeCalendario() =>
        Linhas.Where(l => l.StartsWith("CAL;")).Select(l => (object[])[l]);

    public static IEnumerable<object[]> LinhasDeCargaHoraria() =>
        Linhas.Where(l => l.StartsWith("CARGA;")).Select(l => (object[])[l]);

    public static IEnumerable<object[]> LinhasDeOcorrencia()
    {
        var indicePorCaso = new Dictionary<string, int>();
        foreach (var linha in Linhas.Where(l => l.StartsWith("OCC;")))
        {
            var caso = linha.Split(';')[1];
            indicePorCaso.TryGetValue(caso, out var indice);
            indicePorCaso[caso] = indice + 1;
            yield return [caso, indice, linha];
        }
    }

    public static IEnumerable<object[]> LinhasDeTotais() =>
        Linhas.Where(l => l.StartsWith("TOTC;")).Select(l => (object[])[l.Split(';')[1], l]);

    [Theory]
    [MemberData(nameof(LinhasDeCalendario))]
    public void Contagens_de_calendario_batem_com_o_motor_oficial(string linha)
    {
        var campos = linha.Split(';');
        var periodo = new PeriodoDeApuracao(DateOnly.Parse(campos[1]), DateOnly.Parse(campos[2]));
        var sabado = campos[3] switch
        {
            "SAB_UTIL" => SabadoUtil.Sim,
            "SAB_NAO_UTIL" => SabadoUtil.Nao,
            _ => new SabadoUtil(true, [new PeriodoDeApuracao(new DateOnly(2021, 4, 10), new DateOnly(2021, 4, 30))]),
        };
        Assert.Equal(int.Parse(campos[4]),
            CalendarioTrabalhista.TotalDeDiasUteis(periodo, sabado, CalendarioMqCasos.EhFeriado));
        Assert.Equal(int.Parse(campos[5]), CalendarioTrabalhista.TotalDeRepousos(periodo, sabado));
        Assert.Equal(int.Parse(campos[6]),
            CalendarioTrabalhista.TotalDeFeriados(periodo, CalendarioMqCasos.EhFeriado));
        Assert.Equal(int.Parse(campos[7]),
            CalendarioTrabalhista.TotalDeRepousosEFeriados(periodo, sabado, CalendarioMqCasos.EhFeriado));
    }

    [Theory]
    [MemberData(nameof(LinhasDeCargaHoraria))]
    public void Carga_horaria_bate_com_o_motor_oficial(string linha)
    {
        var campos = linha.Split(';');
        var contexto = new ContextoDeVerbas
        {
            DataAdmissao = new DateOnly(2020, 1, 10),
            ValorCargaHorariaPadrao = 220.0m,
            ExcecoesDaCargaHoraria =
            {
                (new PeriodoDeApuracao(new DateOnly(2021, 2, 1), new DateOnly(2021, 4, 15)), 180.0m),
            },
        };
        var periodo = new PeriodoDeApuracao(DateOnly.Parse(campos[1]), DateOnly.Parse(campos[2]));
        Comparar("CARGA", 0, "valor", campos[3], contexto.ObterValorCargaHoraria(periodo));
    }

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
        Comparar(caso, indice, "devido", campos[9], o.Devido);
        Comparar(caso, indice, "pago", campos[10], o.Pago);
        Comparar(caso, indice, "indiceAcumulado", campos[11], o.IndiceAcumulado);
        Comparar(caso, indice, "diferenca", campos[12], o.Diferenca);
        Comparar(caso, indice, "diferencaCorrigida", campos[13], o.DiferencaCorrigida);
    }

    [Theory]
    [MemberData(nameof(LinhasDeTotais))]
    public void Totais_batem_com_o_motor_oficial(string caso, string linha)
    {
        var campos = linha.Split(';');
        var verba = _casos.Casos[caso];
        Assert.Equal(Linhas.Count(l => l.StartsWith($"OCC;{caso};")), verba.Ocorrencias.Count);

        var totais = TotaisDaVerba.Calcular(verba);
        Comparar(caso, -1, "totalDevido", campos[2], totais.Devido);
        Comparar(caso, -1, "totalPago", campos[3], totais.Pago);
        Comparar(caso, -1, "totalDiferenca", campos[4], totais.Diferenca);
        Comparar(caso, -1, "totalDiferencaCorrigida", campos[5], totais.DiferencaCorrigida);
    }

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
