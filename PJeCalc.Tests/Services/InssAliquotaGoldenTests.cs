using System.Globalization;
using PJeCalc.Core.Services.Inss;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da alíquota previdenciária do segurado contra o motor oficial do PJe-Calc
/// (Java), nas duas eras: alíquota única por faixa (até 02/2020) e alíquota efetiva
/// progressiva (a partir de 03/2020, Reforma da Previdência). As tabelas vêm do banco
/// oficial do TRT-8 (Fixtures/Inss/inss_segurado.csv).
/// </summary>
public class InssAliquotaGoldenTests
{
    private static readonly Dictionary<DateOnly, TabelaPrevidenciariaDoSegurado> Tabelas = CarregarTabelas();

    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_inss_aliquota.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return [Data(c[0]), Num(c[1]), Num(c[2])];
        }
    }

    [Theory]
    [MemberData(nameof(Golden))]
    public void Aliquota_bate_com_o_motor_oficial(DateOnly competencia, decimal valor, decimal aliquotaEsperada)
    {
        var tabela = Tabelas[competencia];
        var aliquota = tabela.ObterAliquotaParaValor(valor);

        // Java usa MathContext(38); decimal tem ~28 dígitos — comparação por tolerância relativa.
        var escala = Math.Max(Math.Abs(aliquotaEsperada), 1m);
        Assert.True(Math.Abs(aliquota - aliquotaEsperada) <= escala * 0.0000000001m,
            $"{competencia:yyyy-MM} valor {valor}: obtida {aliquota}, esperada {aliquotaEsperada}.");
    }

    [Fact]
    public void Reforma_muda_o_regime_na_mesma_base()
    {
        // R$ 1.800 em 02/2020 cai numa faixa de alíquota única; em 03/2020 passa a ter
        // alíquota efetiva progressiva (entre a da 1ª e a da 2ª faixa).
        var antes = Tabelas[new DateOnly(2020, 2, 1)].ObterAliquotaParaValor(1800m);
        var depois = Tabelas[new DateOnly(2020, 3, 1)].ObterAliquotaParaValor(1800m);

        Assert.Equal(8.00m, antes);
        Assert.InRange(depois, 7.5m, 9m);
        Assert.NotEqual(antes, depois);
    }

    [Fact]
    public void Acima_do_teto_a_aliquota_efetiva_satura()
    {
        var tabela = Tabelas[new DateOnly(2025, 1, 1)];
        var noTeto = tabela.ObterAliquotaParaValor(tabela.TetoBeneficio);
        var acimaDoTeto = tabela.ObterAliquotaParaValor(tabela.TetoBeneficio * 2m);

        Assert.Equal(noTeto, acimaDoTeto);
        // A alíquota efetiva no teto satura a contribuição máxima.
        Assert.Equal(tabela.TetoMaximo, Math.Round(noTeto * tabela.TetoBeneficio / 100m, 2));
    }

    private static Dictionary<DateOnly, TabelaPrevidenciariaDoSegurado> CarregarTabelas()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Inss", "inss_segurado.csv");
        var tabelas = new Dictionary<DateOnly, TabelaPrevidenciariaDoSegurado>();

        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(',').Select(campo => campo.Trim().Trim('"')).ToArray();
            var competencia = Data(c[0]);
            tabelas[competencia] = new TabelaPrevidenciariaDoSegurado
            {
                Competencia = competencia,
                Faixa1 = new FaixaPrevidenciaria(Num(c[1]), Opcional(c[2]), Num(c[3])),
                Faixa2 = Faixa(c[4], c[5], c[6]),
                Faixa3 = Faixa(c[7], c[8], c[9]),
                Faixa4 = Faixa(c[10], c[11], c[12]),
                Faixa5 = Faixa(c[13], c[14], c[15]),
                TetoMaximo = Num(c[16]),
                TetoBeneficio = Num(c[17]),
            };
        }
        return tabelas;
    }

    /// <summary>Uma faixa só existe quando tem alíquota.</summary>
    private static FaixaPrevidenciaria? Faixa(string inicial, string final, string aliquota) =>
        string.IsNullOrWhiteSpace(aliquota)
            ? null
            : new FaixaPrevidenciaria(Num(inicial), Opcional(final), Num(aliquota));

    private static DateOnly Data(string s) =>
        DateOnly.ParseExact(s.Trim().Trim('"'), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static decimal Num(string s) =>
        string.IsNullOrWhiteSpace(s) ? 0m : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static decimal? Opcional(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
