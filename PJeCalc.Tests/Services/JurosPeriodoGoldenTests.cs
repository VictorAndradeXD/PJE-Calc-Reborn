using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da matemática por período dos juros (<see cref="PeriodoDeJuros"/>) contra o
/// motor oficial do PJe-Calc (Java): contagem de meses (fração e regra dos ≥15 dias) e a
/// taxa acumulada (diária, simples e composta), em Fixtures/golden_juros_periodo.csv.
/// </summary>
public class JurosPeriodoGoldenTests
{
    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_juros_periodo.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return
            [
                DateOnly.ParseExact(c[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),          // início
                DateOnly.ParseExact(c[2], "yyyy-MM-dd", CultureInfo.InvariantCulture),          // fim
                decimal.Parse(c[3], CultureInfo.InvariantCulture),                              // alíquota
                c[4] == "FRACAO"                                                                // quantidade
                    ? TipoDeQuantidadeDeJurosBaseEnum.Fracao
                    : TipoDeQuantidadeDeJurosBaseEnum.Inteiro,
                c[5] == "COMPOSTOS" ? TipoDeJurosEnum.Compostos : TipoDeJurosEnum.Simples,      // tipo
                MapearTabela(c[6]),                                                             // tabela
                decimal.Parse(c[7], NumberStyles.Float, CultureInfo.InvariantCulture),          // meses (Java)
                decimal.Parse(c[8], NumberStyles.Float, CultureInfo.InvariantCulture),          // taxa (Java)
            ];
        }
    }

    private static JurosEnum MapearTabela(string s) => s switch
    {
        "JUROS_UM_PORCENTO" => JurosEnum.JurosUmPorcento,
        "JUROS_MEIO_PORCENTO" => JurosEnum.JurosMeioPorcento,
        "JUROS_ZERO_TRINTA_TRES" => JurosEnum.JurosZeroTrintaTres,
        "JUROS_PADRAO" => JurosEnum.JurosPadrao,
        _ => JurosEnum.JurosPadrao,
    };

    [Theory]
    [MemberData(nameof(Golden))]
    public void Meses_e_taxa_batem_com_o_motor_oficial(
        DateOnly inicio, DateOnly fim, decimal aliquota,
        TipoDeQuantidadeDeJurosBaseEnum quantidade, TipoDeJurosEnum tipo, JurosEnum tabela,
        decimal mesesEsperado, decimal taxaEsperada)
    {
        var periodo = new PeriodoDeJuros
        {
            Inicio = inicio,
            Fim = fim,
            Aliquota = aliquota,
            Quantidade = quantidade,
            Capitalizacao = tipo,
            Tabela = tabela,
        };

        AssertProximo(mesesEsperado, periodo.Meses, "meses");
        AssertProximo(taxaEsperada, periodo.Taxa, "taxa");
    }

    // Tolerância relativa: Java usa MathContext(38)/double; decimal tem ~28 dígitos.
    private static void AssertProximo(decimal esperado, decimal obtido, string campo)
    {
        var escala = Math.Max(Math.Abs(esperado), 1m);
        Assert.True(Math.Abs(obtido - esperado) <= escala * 0.0000000001m,
            $"{campo}: obtido {obtido} distante do esperado {esperado}.");
    }
}
