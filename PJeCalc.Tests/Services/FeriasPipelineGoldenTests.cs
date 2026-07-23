using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói os cenários do harness <c>tools/golden/GoldenGenFeriasPipeline.java</c>
/// (motor Java oficial headless): tabela do art. 130, quebra em períodos aquisitivos,
/// salário em férias, pipeline PERIODO_AQUISITIVO (gozos/dobra/saldo/indenizadas/
/// fracionário/abono/prescrição), faltas nos provedores e reflexos com destino férias.
/// Índice estubado: <c>1 + 0,01 × ((ano − 2019) × 12 + mês)</c>.
/// </summary>
public sealed class FeriasPipelineCasos
{
    public IReadOnlyDictionary<string, VerbaEmCalculo> Casos { get; }

    private static decimal Indice(DateOnly data) =>
        1m + 0.01m * ((data.Year - 2019) * 12 + data.Month);

    public FeriasPipelineCasos()
    {
        var casos = new Dictionary<string, VerbaEmCalculo>();

        // ---- FER_COMPLETAS: PA1 gozado cruzando o concessivo, PA2 parcial c/ saldo,
        // PA3 indenizado e fracionário até a demissão projetada ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2019, 1, 10),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2019, 1, 10, 2020, 1, 9),
                        PeriodoConcessivo = Periodo(2020, 1, 10, 2021, 1, 9),
                        PeriodoDeGozo1 = Periodo(2020, 12, 26, 2021, 1, 24),
                        DobraDoPeriodoDeGozo1 = true,
                    },
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2020, 1, 10, 2021, 1, 9),
                        PeriodoConcessivo = Periodo(2021, 1, 10, 2022, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.GozadasParcialmente,
                        PeriodoDeGozo1 = Periodo(2021, 5, 1, 2021, 5, 15),
                    },
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2021, 1, 10, 2022, 1, 9),
                        PeriodoConcessivo = Periodo(2022, 1, 10, 2023, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.Indenizadas,
                    },
                },
            };
            casos["FER_COMPLETAS"] = Executar(new MotorDeVerbas(contexto),
                VerbaDeFerias(new DateOnly(2019, 1, 10), new DateOnly(2022, 3, 10)));
        }

        // ---- FER_ABONO: gozo de 20 dias com abono de 10 (fator 1,5) ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2019, 1, 10),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2019, 1, 10, 2020, 1, 9),
                        PeriodoConcessivo = Periodo(2020, 1, 10, 2021, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.GozadasParcialmente,
                        Abono = true,
                        PeriodoDeGozo1 = Periodo(2020, 6, 1, 2020, 6, 20),
                    },
                },
            };
            casos["FER_ABONO"] = Executar(new MotorDeVerbas(contexto),
                VerbaDeFerias(new DateOnly(2019, 1, 10), new DateOnly(2022, 3, 10)));
        }

        // ---- FER_PRESCRITAS: concessivo encerrado antes da prescrição não gera ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2014, 1, 10),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                PrescricaoQuinquenal = true,
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2014, 1, 10, 2015, 1, 9),
                        PeriodoConcessivo = Periodo(2015, 1, 10, 2016, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.Indenizadas,
                    },
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2020, 1, 10, 2021, 1, 9),
                        PeriodoConcessivo = Periodo(2021, 1, 10, 2022, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.Indenizadas,
                    },
                },
            };
            casos["FER_PRESCRITAS"] = Executar(new MotorDeVerbas(contexto),
                VerbaDeFerias(new DateOnly(2017, 4, 15), new DateOnly(2022, 3, 10)));
        }

        // ---- FER_PROPORCIONAIS: só o fracionário, avos do PA e art. 130 com faltas ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2021, 10, 1),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
                Faltas = { new FaltaDoCalculo(new DateOnly(2021, 11, 8), new DateOnly(2021, 11, 13)) },
            };
            var verba = VerbaDeFerias(new DateOnly(2021, 10, 1), new DateOnly(2022, 3, 10));
            verba.Divisor.OutroValor = 12m;
            verba.Quantidade.Tipo = TipoDeQuantidadeEnum.Avos;
            casos["FER_PROPORCIONAIS"] = Executar(new MotorDeVerbas(contexto), verba);
        }

        // ---- MENSAL_FALTAS: proporcionalização com férias gozadas e faltas reais ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 1, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                IndiceAcumulado = Indice,
                Faltas =
                {
                    new FaltaDoCalculo(new DateOnly(2021, 11, 8), new DateOnly(2021, 11, 13)),
                    new FaltaDoCalculo(new DateOnly(2021, 12, 6), new DateOnly(2021, 12, 8), Justificada: true),
                },
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2020, 1, 10, 2021, 1, 9),
                        PeriodoConcessivo = Periodo(2021, 1, 10, 2022, 1, 9),
                        Situacao = SituacaoDaFeriasEnum.GozadasParcialmente,
                        PeriodoDeGozo1 = Periodo(2021, 10, 11, 2021, 10, 20),
                    },
                },
            };
            var verba = new VerbaEmCalculo
            {
                Nome = "mensal-faltas",
                Tipo = TipoDaVerbaEnum.Informada,
                PeriodoInicial = new DateOnly(2021, 10, 1),
                PeriodoFinal = new DateOnly(2021, 12, 31),
                AplicarProporcionalidade = true,
                ExcluirFeriasGozadas = true,
                ExcluirFaltaJustificada = true,
                ExcluirFaltaNaoJustificada = true,
                Constante = 3000.00m,
            };
            casos["MENSAL_FALTAS"] = Executar(new MotorDeVerbas(contexto), verba);
        }

        // ---- Reflexos de HE em férias: média do PA e valor mensal ----
        {
            var contexto = new ContextoDeVerbas
            {
                DataAdmissao = new DateOnly(2020, 6, 1),
                DataDemissao = new DateOnly(2022, 3, 10),
                DataAjuizamento = new DateOnly(2022, 4, 15),
                DataDeLiquidacao = new DateOnly(2022, 6, 30),
                ValorUltimaRemuneracao = 3000.00m,
                IndiceAcumulado = Indice,
                ListaDeFerias =
                {
                    new FeriasDoCalculo
                    {
                        PeriodoAquisitivo = Periodo(2020, 6, 1, 2021, 5, 31),
                        PeriodoConcessivo = Periodo(2021, 6, 1, 2022, 5, 31),
                        PeriodoDeGozo1 = Periodo(2021, 8, 1, 2021, 8, 30),
                    },
                },
            };
            var motor = new MotorDeVerbas(contexto);
            var origem = Executar(motor, new VerbaEmCalculo
            {
                Nome = "he",
                Tipo = TipoDaVerbaEnum.Informada,
                PeriodoInicial = new DateOnly(2020, 6, 1),
                PeriodoFinal = new DateOnly(2022, 3, 10),
                AplicarProporcionalidade = true,
                Constante = 600.00m,
            });

            casos["REFLEXO_MV_PA_FERIAS"] = Executar(motor, ReflexoEmFerias(origem,
                ComportamentoDoReflexoEnum.MediaPeloValor));
            casos["REFLEXO_VM_FERIAS"] = Executar(motor, ReflexoEmFerias(origem,
                ComportamentoDoReflexoEnum.ValorMensal));
        }

        Casos = casos;
    }

    private static PeriodoDeApuracao Periodo(int a1, int m1, int d1, int a2, int m2, int d2) =>
        new(new DateOnly(a1, m1, d1), new DateOnly(a2, m2, d2));

    private static VerbaEmCalculo VerbaDeFerias(DateOnly inicio, DateOnly fim) => new()
    {
        Nome = "ferias",
        Tipo = TipoDaVerbaEnum.Calculada,
        Caracteristica = CaracteristicaDaVerbaEnum.Ferias,
        OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.PeriodoAquisitivo,
        PeriodoInicial = inicio,
        PeriodoFinal = fim,
        BaseTabelada = new TermoBaseTabelada { Tipo = BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao },
    };

    private static VerbaEmCalculo ReflexoEmFerias(VerbaEmCalculo origem, ComportamentoDoReflexoEnum comportamento) => new()
    {
        Nome = $"reflexo-he-ferias-{comportamento}",
        Tipo = TipoDaVerbaEnum.Reflexo,
        Caracteristica = CaracteristicaDaVerbaEnum.Ferias,
        OcorrenciaDePagamento = OcorrenciaDePagamentoEnum.PeriodoAquisitivo,
        PeriodoInicial = new DateOnly(2020, 6, 1),
        PeriodoFinal = new DateOnly(2022, 3, 10),
        ComportamentoDoReflexo = comportamento,
        PeriodoDaMedia = PeriodoDaMediaDoReflexoEnum.PeriodoAquisitivo,
        BasesVerba = { new ItemBaseVerba(origem) },
    };

    private static VerbaEmCalculo Executar(MotorDeVerbas motor, VerbaEmCalculo verba)
    {
        motor.GerarOcorrencias(verba);
        motor.Liquidar(verba);
        return verba;
    }
}

public class FeriasPipelineGoldenTests(FeriasPipelineCasos casos) : IClassFixture<FeriasPipelineCasos>
{
    private readonly FeriasPipelineCasos _casos = casos;

    private static readonly string[] Linhas =
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_ferias_pipeline.csv"));

    public static IEnumerable<object[]> LinhasSimples(string prefixo) =>
        Linhas.Where(l => l.StartsWith(prefixo + ";")).Select(l => (object[])[l]);

    public static IEnumerable<object[]> LinhasDePrazo() => LinhasSimples("PRAZO");
    public static IEnumerable<object[]> LinhasDeQuebraEmAnos() => LinhasSimples("BRKY");
    public static IEnumerable<object[]> LinhasDeSalarioEmFerias() => LinhasSimples("SALFER");

    public static IEnumerable<object[]> LinhasDeOcorrencia()
    {
        var indicePorCaso = new Dictionary<string, int>();
        foreach (var linha in Linhas.Where(l => l.StartsWith("OCF;")))
        {
            var caso = linha.Split(';')[1];
            indicePorCaso.TryGetValue(caso, out var indice);
            indicePorCaso[caso] = indice + 1;
            yield return [caso, indice, linha];
        }
    }

    public static IEnumerable<object[]> LinhasDeTotais() =>
        Linhas.Where(l => l.StartsWith("TOTF;")).Select(l => (object[])[l.Split(';')[1], l]);

    [Theory]
    [MemberData(nameof(LinhasDePrazo))]
    public void Prazo_do_art130_bate_com_o_motor_oficial(string linha)
    {
        var campos = linha.Split(';');
        var regime = campos[2] == "PARCIAL" ? RegimeDoContratoEnum.Parcial : RegimeDoContratoEnum.Integral;
        var obtido = PrazoDeFerias.Calcular(DateOnly.Parse(campos[1]), regime, int.Parse(campos[3]));
        Assert.Equal(int.Parse(campos[4]), obtido);
    }

    [Theory]
    [MemberData(nameof(LinhasDeQuebraEmAnos))]
    public void Quebra_em_anos_bate_com_o_motor_oficial(string linha)
    {
        var campos = linha.Split(';');
        var periodos = PeriodoDeApuracao.QuebrarEmAnos(
            DateOnly.Parse(campos[1]), DateOnly.Parse(campos[2]), incluirResto: false);
        var esperados = campos.Skip(3).ToArray();
        Assert.Equal(esperados.Length, periodos.Count);
        for (var i = 0; i < esperados.Length; i++)
        {
            Assert.Equal(esperados[i],
                $"{periodos[i].Inicio:yyyy-MM-dd}..{periodos[i].Fim:yyyy-MM-dd}");
        }
    }

    [Theory]
    [MemberData(nameof(LinhasDeSalarioEmFerias))]
    public void Salario_em_ferias_bate_com_o_motor_oficial(string linha)
    {
        var campos = linha.Split(';');
        var periodo = new PeriodoDeApuracao(DateOnly.Parse(campos[1]), DateOnly.Parse(campos[2]));
        var valorMes2 = campos[4].Length == 0 ? (decimal?)null : decimal.Parse(campos[4], System.Globalization.CultureInfo.InvariantCulture);
        var obtido = SalarioEmFerias.Calcular(periodo, decimal.Parse(campos[3], System.Globalization.CultureInfo.InvariantCulture), valorMes2);
        Comparar("SALFER", 0, "resultado", campos[5], obtido);
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
        Comparar(caso, indice, "quantidadeIntegral", campos[9], o.QuantidadeIntegral);
        Comparar(caso, indice, "devido", campos[10], o.Devido);
        Comparar(caso, indice, "devidoIntegral", campos[11], o.DevidoIntegral);
        Comparar(caso, indice, "pago", campos[12], o.Pago);
        Comparar(caso, indice, "pagoIntegral", campos[13], o.PagoIntegral);
        Assert.Equal(campos[14] == "1", o.Dobra);
        Comparar(caso, indice, "indiceAcumulado", campos[15], o.IndiceAcumulado);
        Comparar(caso, indice, "diferenca", campos[16], o.Diferenca);
        Comparar(caso, indice, "diferencaCorrigida", campos[17], o.DiferencaCorrigida);

        Assert.Equal(campos[18], o.InicioDoPeriodoAquisitivo?.ToString("yyyy-MM-dd") ?? "");
        Assert.Equal(campos[19], o.FimDoPeriodoAquisitivo?.ToString("yyyy-MM-dd") ?? "");
        Assert.Equal(campos[20] == "1", o.FeriasIndenizadas);
        Assert.Equal(campos[21] == "1", o.FeriasComAbono);
        Comparar(caso, indice, "difIncidencias", campos[22], o.DiferencaParaCalculoDasIncidencias(corrigida: true));
    }

    [Theory]
    [MemberData(nameof(LinhasDeTotais))]
    public void Totais_batem_com_o_motor_oficial(string caso, string linha)
    {
        var campos = linha.Split(';');
        var verba = _casos.Casos[caso];
        var quantidadeEsperada = Linhas.Count(l => l.StartsWith($"OCF;{caso};"));
        Assert.Equal(quantidadeEsperada, verba.Ocorrencias.Count);

        var totais = TotaisDaVerba.Calcular(verba);
        Comparar(caso, -1, "totalDevido", campos[2], totais.Devido);
        Comparar(caso, -1, "totalPago", campos[3], totais.Pago);
        Comparar(caso, -1, "totalDiferenca", campos[4], totais.Diferenca);
        Comparar(caso, -1, "totalDiferencaCorrigida", campos[5], totais.DiferencaCorrigida);
        Comparar(caso, -1, "totalIncidencias", campos[6], totais.DiferencaCorrigidaParaCalculoDasIncidencias);
        Comparar(caso, -1, "totalFeriasGozadas", campos[7], totais.DiferencaCorrigidaDeFeriasGozadas);
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
