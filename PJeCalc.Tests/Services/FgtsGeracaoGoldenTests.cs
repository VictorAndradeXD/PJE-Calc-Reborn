using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Fgts;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a prescrição do FGTS (STF ARE 709212) e a janela sugerida de geração contra o motor
/// oficial (harness <c>tools/golden/GoldenGenFgtsGeracao.java</c>), e a geração das ocorrências
/// mensais (janela + quebra em meses + proporcionalidade do histórico salarial).
/// </summary>
public sealed class FgtsGeracaoGoldenTests
{
    private static readonly DateOnly Termino = new(2021, 6, 1);

    [Fact]
    public void Prescricao_e_janela_batem_com_o_motor_oficial()
    {
        var calculado = new Dictionary<string, string>();

        void Prescricao(string cenario, DateOnly admissao, DateOnly ajuizamento) =>
            calculado[$"{cenario};prescricao"] = PrescricaoDoFgts.CalcularData(ajuizamento, admissao).ToString("yyyy-MM-dd");

        void Janela(string cenario, DateOnly admissao, DateOnly? demissao, DateOnly ajuizamento, bool presc)
        {
            var (inicial, final) = PrescricaoDoFgts.JanelaDeGeracao(admissao, demissao, Termino, ajuizamento, presc);
            calculado[$"{cenario};inicial"] = inicial.ToString("yyyy-MM-dd");
            calculado[$"{cenario};final"] = final.ToString("yyyy-MM-dd");
        }

        Prescricao("P1_ANTES_2014", new(2010, 1, 1), new(2010, 1, 1));
        Prescricao("P2_TRANSICAO_ADM_APOS_1989", new(2005, 1, 1), new(2016, 6, 1));
        Prescricao("P3_TRANSICAO_ADM_ANTES_1989", new(1985, 1, 1), new(2016, 6, 1));
        Prescricao("P4_POS_2019", new(2005, 1, 1), new(2020, 3, 1));
        Prescricao("P5_LIMITE_2014", new(2000, 1, 1), new(2014, 11, 13));
        Prescricao("P6_LIMITE_2019", new(2000, 1, 1), new(2019, 11, 13));
        Prescricao("P7_ADM_LIMITE_1989", new(1989, 11, 13), new(2016, 6, 1));

        Janela("J1_SEM_PRESCRICAO", new(2015, 1, 10), new(2021, 3, 20), new(2020, 1, 1), false);
        Janela("J2_PRESCRICAO_APOS_ADM", new(2010, 1, 10), new(2021, 3, 20), new(2020, 6, 1), true);
        Janela("J3_PRESCRICAO_ANTES_ADM", new(2018, 1, 10), new(2021, 3, 20), new(2020, 6, 1), true);
        Janela("J4_SEM_DEMISSAO", new(2015, 1, 10), null, new(2020, 1, 1), false);

        var golden = LerGolden();
        Assert.NotEmpty(golden);
        foreach (var (chave, esperado) in golden)
        {
            Assert.True(calculado.ContainsKey(chave), $"cenário não reproduzido: {chave}");
            Assert.Equal(esperado, calculado[chave]);
        }
    }

    [Fact]
    public void Geracao_produz_ocorrencias_mensais_com_base_proporcional()
    {
        var salario = new Dictionary<DateOnly, decimal>
        {
            [new(2020, 1, 1)] = 2000m,
            [new(2020, 2, 1)] = 2000m,
            [new(2020, 3, 1)] = 2000m,
            [new(2020, 4, 1)] = 2000m,
        };

        var contexto = new ContextoDeGeracaoDeFgts
        {
            Admissao = new(2020, 1, 15),
            Demissao = new(2020, 4, 10),
            TerminoCalculo = Termino,
            Ajuizamento = new(2021, 1, 1),
            AplicarPrescricao = false,
            Aliquota = AliquotaDoFgtsEnum.OitoPorCento,
            Historicos = [new HistoricoSalarialDeFgts(salario, AplicarProporcionalidade: true, Recolhido: false)],
        };

        var ocorrencias = GeradorDeOcorrenciasDeFgts.Gerar(contexto);

        Assert.Equal(4, ocorrencias.Count);
        Assert.Equal([new(2020, 1, 1), new(2020, 2, 1), new(2020, 3, 1), new(2020, 4, 1)],
            ocorrencias.Select(o => o.Competencia));

        // Janeiro parcial: 15→31 = 17 dias → 2000 × 17/30.
        Assert.Equal(2000m * 17m / 30m, ocorrencias[0].BaseHistorico);
        // Fevereiro cheio (bissexto, divisor 29): base integral.
        Assert.Equal(2000m, ocorrencias[1].BaseHistorico);
        // Março cheio de 31 dias: regra do 31 → conta 30 dias → base integral.
        Assert.Equal(2000m, ocorrencias[2].BaseHistorico);
        // Abril parcial: 1→10 = 10 dias → 2000 × 10/30.
        Assert.Equal(2000m * 10m / 30m, ocorrencias[3].BaseHistorico);

        // Nada recolhido → depósito zero; FGTS devido = 8% da base.
        Assert.All(ocorrencias, o => Assert.Equal(0m, o.Depositado));
        Assert.Equal(Math.Round(2000m * 0.08m, 2, MidpointRounding.ToEven),
            Math.Round(ocorrencias[1].ValorDevido, 2, MidpointRounding.ToEven));
    }

    private static Dictionary<string, string> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_fgts_geracao.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => $"{c[0]};{c[1]}", c => c[2].Trim());
    }
}
