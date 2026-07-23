using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Custas;
using PJeCalc.Core.Services.Honorarios;
using PJeCalc.Core.Services.Irpf;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Reconstrói os cenários do harness <c>tools/golden/GoldenGenPenalidades.java</c> (motor Java
/// oficial com o agregado "bruto/principal" injetado) e confere multas, honorários (com IRRF) e
/// custas (piso/teto/consolidação com dados reais) contra os valores-verdade.
/// </summary>
public sealed class PenalidadesGoldenTests
{
    private const decimal Tolerancia = 1e-10m;

    private static readonly ParametrosDeCustasFixas ParametrosCustas =
        CarregarParametrosDeCustas();

    private static ParametrosDeCustasFixas CarregarParametrosDeCustas()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        return new EfParametroDeCustasProvider(contexto).ObterPorData(new DateOnly(2021, 6, 10));
    }

    [Fact]
    public void Golden_de_penalidades_bate_com_o_motor_oficial()
    {
        var calculado = Calcular();
        var golden = LerGolden();

        Assert.NotEmpty(golden);
        foreach (var (chave, esperado) in golden)
        {
            Assert.True(calculado.ContainsKey(chave), $"cenário não reproduzido: {chave}");
            var obtido = calculado[chave];
            var diferenca = Math.Abs(obtido - esperado);
            var limite = Tolerancia * Math.Max(1m, Math.Abs(esperado));
            Assert.True(diferenca <= limite,
                $"{chave}: esperado {esperado}, obtido {obtido} (Δ {diferenca})");
        }
    }

    private static Dictionary<string, decimal> Calcular()
    {
        var r = new Dictionary<string, decimal>();

        void Add(string cenario, string chave, decimal valor) => r[$"{cenario};{chave}"] = valor;

        // ---------------- MULTAS ----------------
        var m1 = ApuracaoDeMulta.Calcular(
            new ParametrosDaMulta { Base = BaseParaApuracaoDeMultaEnum.Principal, Aliquota = 7.5m },
            new BasesDaMulta { PrincipalCorrigido = 1234.56m });
        Add("M1_PRINCIPAL", "base", m1.Base);
        Add("M1_PRINCIPAL", "valorCorrigido", m1.ValorCorrigido);
        Add("M1_PRINCIPAL", "valorTotal", m1.ValorTotal);

        var m2 = ApuracaoDeMulta.Calcular(
            new ParametrosDaMulta { Base = BaseParaApuracaoDeMultaEnum.Principal, Aliquota = 40m },
            new BasesDaMulta { PrincipalCorrigido = 10000.00m, JurosDeMora = 1500.00m });
        Add("M2_PRINCIPAL", "base", m2.Base);
        Add("M2_PRINCIPAL", "valorCorrigido", m2.ValorCorrigido);
        Add("M2_PRINCIPAL", "valorTotal", m2.ValorTotal);

        var m3 = ApuracaoDeMulta.Calcular(
            new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 2500.00m, TaxaDeJuros = 1.0m },
            new BasesDaMulta());
        Add("M3_INFORMADO", "indice", 1m);
        Add("M3_INFORMADO", "valorCorrigido", m3.ValorCorrigido);
        Add("M3_INFORMADO", "juros", m3.Juros);
        Add("M3_INFORMADO", "valorTotal", m3.ValorTotal);

        var m4 = ApuracaoDeMulta.Calcular(
            new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 333.33m, TaxaDeJuros = 8.33m },
            new BasesDaMulta());
        Add("M4_INFORMADO", "valorCorrigido", m4.ValorCorrigido);
        Add("M4_INFORMADO", "juros", m4.Juros);
        Add("M4_INFORMADO", "valorTotal", m4.ValorTotal);

        var mA = ApuracaoDeMulta.Calcular(new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 92.59m }, new BasesDaMulta());
        var mB = ApuracaoDeMulta.Calcular(new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 10.125m }, new BasesDaMulta());
        var mC = ApuracaoDeMulta.Calcular(new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 50.00m }, new BasesDaMulta());
        var mD = ApuracaoDeMulta.Calcular(new ParametrosDaMulta { TipoValor = TipoValorEnum.Informado, ValorInformado = 333.33m, TaxaDeJuros = 8.33m }, new BasesDaMulta());
        var totMultas = TotalizadorDeMulta.Calcular(
        [
            (CredorDevedorMultaEnum.ReclamanteReclamado, mA.ValorTotal),
            (CredorDevedorMultaEnum.ReclamanteReclamado, mB.ValorTotal),
            (CredorDevedorMultaEnum.ReclamadoReclamante, mC.ValorTotal),
            (CredorDevedorMultaEnum.TerceiroReclamado, mD.ValorTotal),
        ]);
        Add("MT_TOTALIZADOR", "reclamanteReclamado", totMultas.ReclamanteReclamado);
        Add("MT_TOTALIZADOR", "reclamadoReclamante", totMultas.ReclamadoReclamante);
        Add("MT_TOTALIZADOR", "terceiroReclamado", totMultas.TerceiroReclamado);

        // ---------------- HONORÁRIOS ----------------
        var h1 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario { Base = BaseParaApuracaoDeHonorarioEnum.Bruto, Aliquota = 10m },
            new BasesDoHonorario { BrutoDevidoAoReclamante = 10000.00m });
        Add("H1_BRUTO", "base", h1.Base);
        Add("H1_BRUTO", "valorCorrigido", h1.ValorCorrigido);
        Add("H1_BRUTO", "valorTotal", h1.ValorTotal);
        Add("H1_BRUTO", "imposto", h1.Imposto);

        var h2 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario
            {
                Base = BaseParaApuracaoDeHonorarioEnum.Bruto,
                Aliquota = 10m,
                ApurarIRRF = true,
                TipoImposto = TipoDeImpostoDeRendaEnum.PessoaJuridica,
            },
            new BasesDoHonorario { BrutoDevidoAoReclamante = 50000.00m });
        Add("H2_BRUTO_PJ", "valorCorrigido", h2.ValorCorrigido);
        Add("H2_BRUTO_PJ", "imposto", h2.Imposto);

        var h3 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario
            {
                Base = BaseParaApuracaoDeHonorarioEnum.Bruto,
                Aliquota = 7.5m,
                ApurarIRRF = true,
                TipoImposto = TipoDeImpostoDeRendaEnum.PessoaJuridica,
            },
            new BasesDoHonorario { BrutoDevidoAoReclamante = 1234.56m });
        Add("H3_BRUTO_PJ", "valorCorrigido", h3.ValorCorrigido);
        Add("H3_BRUTO_PJ", "imposto", h3.Imposto);

        var h4 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario
            {
                TipoValor = TipoValorEnum.Informado,
                ValorInformado = 8000.00m,
                TaxaDeJuros = 1.0m,
                ApurarIRRF = true,
                ApurarIRPFSobreJuros = true,
                TipoImposto = TipoDeImpostoDeRendaEnum.PessoaJuridica,
            },
            new BasesDoHonorario());
        Add("H4_INFORMADO", "valorCorrigido", h4.ValorCorrigido);
        Add("H4_INFORMADO", "juros", h4.Juros ?? 0m);
        Add("H4_INFORMADO", "valorTotal", h4.ValorTotal);
        Add("H4_INFORMADO", "imposto", h4.Imposto);

        var tabelaIrpf = CarregarTabelaIrpf(new DateOnly(2021, 6, 1));
        var h5 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario
            {
                Base = BaseParaApuracaoDeHonorarioEnum.Bruto,
                Aliquota = 10m,
                ApurarIRRF = true,
                TipoImposto = TipoDeImpostoDeRendaEnum.PessoaFisica,
                TabelaIrpf = tabelaIrpf,
            },
            new BasesDoHonorario { BrutoDevidoAoReclamante = 30000.00m });
        Add("H5_BRUTO_PF", "valorCorrigido", h5.ValorCorrigido);
        Add("H5_BRUTO_PF", "aliquotaIrpf", tabelaIrpf.ObterFaixaParaValor(h5.ValorCorrigido).Aliquota);
        Add("H5_BRUTO_PF", "imposto", h5.Imposto);

        var h6 = ApuracaoDeHonorario.Calcular(
            new ParametrosDoHonorario
            {
                TipoValor = TipoValorEnum.Informado,
                ValorInformado = 6000.00m,
                ApurarIRRF = true,
                TipoImposto = TipoDeImpostoDeRendaEnum.PessoaFisica,
                TabelaIrpf = tabelaIrpf,
            },
            new BasesDoHonorario());
        Add("H6_INFORMADO_PF", "valorCorrigido", h6.ValorCorrigido);
        Add("H6_INFORMADO_PF", "imposto", h6.Imposto);

        var hA = ApuracaoDeHonorario.Calcular(new ParametrosDoHonorario { TipoValor = TipoValorEnum.Informado, ValorInformado = 1000.00m, Devedor = TipoDeDevedorDoHonorarioEnum.Reclamado }, new BasesDoHonorario());
        var hB = ApuracaoDeHonorario.Calcular(new ParametrosDoHonorario { TipoValor = TipoValorEnum.Informado, ValorInformado = 500.00m, Devedor = TipoDeDevedorDoHonorarioEnum.Reclamante, Cobranca = TipoCobrancaReclamanteEnum.Cobrar }, new BasesDoHonorario());
        var hC = ApuracaoDeHonorario.Calcular(new ParametrosDoHonorario { TipoValor = TipoValorEnum.Informado, ValorInformado = 300.00m, Devedor = TipoDeDevedorDoHonorarioEnum.Reclamante, Cobranca = TipoCobrancaReclamanteEnum.DescontarCredito }, new BasesDoHonorario());
        var totHon = TotalizadorDeHonorario.Calcular(
        [
            (TipoDeDevedorDoHonorarioEnum.Reclamado, TipoCobrancaReclamanteEnum.DescontarCredito, hA.ValorTotal),
            (TipoDeDevedorDoHonorarioEnum.Reclamante, TipoCobrancaReclamanteEnum.Cobrar, hB.ValorTotal),
            (TipoDeDevedorDoHonorarioEnum.Reclamante, TipoCobrancaReclamanteEnum.DescontarCredito, hC.ValorTotal),
        ]);
        Add("HT_TOTALIZADOR", "devidoPeloReclamante", totHon.DevidoPeloReclamante);
        Add("HT_TOTALIZADOR", "devidoPeloReclamado", totHon.DevidoPeloReclamado);

        // ---------------- CUSTAS ----------------
        ParametrosDeCustas CustasBR() => new()
        {
            Parametros = ParametrosCustas,
            BaseCalculada = BaseParaCustasCalculadasEnum.BrutoDevidoAoReclamante,
        };

        var c1 = ApuracaoDeCustas.Calcular(
            CustasBR() with { ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento },
            new BasesDasCustas { BrutoDevidoAoReclamante = 100000.00m });
        Add("C1_CONHEC_2PC", "base", c1.BaseCalculada);
        Add("C1_CONHEC_2PC", "totalConhecimento", c1.TotalConhecimento ?? 0m);
        Add("C1_CONHEC_2PC", "consolidado", c1.ConsolidadoReclamado);

        var c2 = ApuracaoDeCustas.Calcular(
            CustasBR() with { ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento },
            new BasesDasCustas { BrutoDevidoAoReclamante = 100.00m });
        Add("C2_CONHEC_PISO", "totalConhecimento", c2.TotalConhecimento ?? 0m);
        Add("C2_CONHEC_PISO", "consolidado", c2.ConsolidadoReclamado);

        var c3 = ApuracaoDeCustas.Calcular(
            CustasBR() with
            {
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                TetoConhecimento = 4m * 6433.57m, // 4x teto RGPS 01/2021
            },
            new BasesDasCustas { BrutoDevidoAoReclamante = 2000000.00m });
        Add("C3_CONHEC_TETO", "teto", 4m * 6433.57m);
        Add("C3_CONHEC_TETO", "totalConhecimento", c3.TotalConhecimento ?? 0m);

        var c4 = ApuracaoDeCustas.Calcular(
            CustasBR() with { Liquidacao = TipoDeCustasDeLiquidacaoEnum.CalculadaMeioPorCento },
            new BasesDasCustas { BrutoDevidoAoReclamante = 200000.00m });
        Add("C4_LIQ_TETO", "totalLiquidacao", c4.TotalLiquidacao ?? 0m);
        Add("C4_LIQ_TETO", "consolidado", c4.ConsolidadoReclamado);

        var c4b = ApuracaoDeCustas.Calcular(
            CustasBR() with { Liquidacao = TipoDeCustasDeLiquidacaoEnum.CalculadaMeioPorCento },
            new BasesDasCustas { BrutoDevidoAoReclamante = 50000.00m });
        Add("C4b_LIQ", "totalLiquidacao", c4b.TotalLiquidacao ?? 0m);

        var c5 = ApuracaoDeCustas.Calcular(
            CustasBR() with { Autos = [new ItemDeAuto(100000.00m)] },
            new BasesDasCustas());
        Add("C5_AUTO_TETO", "totalAuto", c5.ConsolidadoReclamado);
        Add("C5_AUTO_TETO", "consolidado", c5.ConsolidadoReclamado);

        var c5b = ApuracaoDeCustas.Calcular(
            CustasBR() with { Autos = [new ItemDeAuto(10000.00m)] },
            new BasesDasCustas());
        Add("C5b_AUTO", "totalAuto", c5b.ConsolidadoReclamado);

        var c6 = ApuracaoDeCustas.Calcular(
            CustasBR() with { Armazenamentos = [new ItemDeArmazenamento(50000.00m, Dias: 31)] },
            new BasesDasCustas());
        Add("C6_ARMAZENAMENTO", "qtdeDias", 31m);
        Add("C6_ARMAZENAMENTO", "totalArmazenamento", c6.ConsolidadoReclamado);

        var c7 = ApuracaoDeCustas.Calcular(
            CustasBR() with { CustasFixas = new CustasFixasQuantidades { AtosUrbanos = 2, RecursoRevista = 1 } },
            new BasesDasCustas());
        Add("C7_FIXAS", "consolidado", c7.ConsolidadoReclamado);

        var c8 = ApuracaoDeCustas.Calcular(
            CustasBR() with
            {
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                CustasPagasReclamado = [new CustaPagaDeCustas(500.00m)],
            },
            new BasesDasCustas { BrutoDevidoAoReclamante = 100000.00m });
        Add("C8_PAGAS", "consolidado", c8.ConsolidadoReclamado);

        var c8b = ApuracaoDeCustas.Calcular(
            CustasBR() with
            {
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                CustasPagasReclamado = [new CustaPagaDeCustas(5000.00m)],
            },
            new BasesDasCustas { BrutoDevidoAoReclamante = 1000.00m });
        Add("C8b_PISO0", "consolidado", c8b.ConsolidadoReclamado);

        var c9 = ApuracaoDeCustas.Calcular(
            CustasBR() with
            {
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                Liquidacao = TipoDeCustasDeLiquidacaoEnum.CalculadaMeioPorCento,
                CustasFixas = new CustasFixasQuantidades { AgravoPeticao = 1 },
                CustasPagasReclamado = [new CustaPagaDeCustas(100.00m)],
            },
            new BasesDasCustas { BrutoDevidoAoReclamante = 80000.00m });
        Add("C9_COMBINADO", "consolidado", c9.ConsolidadoReclamado);

        return r;
    }

    private static TabelaIrpf CarregarTabelaIrpf(DateOnly competencia)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Irpf", "irpf_tabela.csv");
        var linha = File.ReadAllLines(caminho).Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(',').Select(campo => campo.Trim().Trim('"')).ToArray())
            .First(c => DateOnly.ParseExact(c[0], "yyyy-MM-dd", CultureInfo.InvariantCulture) == competencia);

        FaixaFiscal? Faixa(int i) => string.IsNullOrWhiteSpace(linha[i + 2])
            ? null
            : new FaixaFiscal(Num(linha[i]), Opcional(linha[i + 1]), Num(linha[i + 2]), Num(linha[i + 3]));

        return new TabelaIrpf
        {
            Competencia = competencia,
            Faixa1 = Faixa(1)!,
            Faixa2 = Faixa(5),
            Faixa3 = Faixa(9),
            Faixa4 = Faixa(13),
            Faixa5 = Faixa(17),
            DeducaoPorDependente = Num(linha[21]),
            DeducaoAposentadoMaior65 = Num(linha[22]),
        };
    }

    private static decimal Num(string s) =>
        string.IsNullOrWhiteSpace(s) ? 0m : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static decimal? Opcional(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_penalidades.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(
                c => $"{c[0]};{c[1]}",
                c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
