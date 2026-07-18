using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação de paridade: compara o <see cref="CorrecaoMonetariaService"/> C# contra
/// os valores corrigidos produzidos pelo motor oficial do PJe-Calc (Java), gerados em
/// Fixtures/golden_correcao.csv a partir dos índices reais do banco do TRT-8.
///
/// Regime: mês do vencimento (mesma fronteira usada na geração dos golden).
/// </summary>
public class CorrecaoMonetariaGoldenTests
{
    private static readonly CorrecaoMonetariaService Service = new(new CsvIndiceProvider());

    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_correcao.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return
            [
                Enum.Parse<IndiceMonetarioEnum>(c[0]),                                   // índice
                decimal.Parse(c[1], CultureInfo.InvariantCulture),                      // valor
                DateOnly.ParseExact(c[2], "yyyy-MM-dd", CultureInfo.InvariantCulture),  // vencimento
                DateOnly.ParseExact(c[3], "yyyy-MM-dd", CultureInfo.InvariantCulture),  // liquidação
                decimal.Parse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture),  // fator (Java)
                decimal.Parse(c[5], CultureInfo.InvariantCulture),                      // corrigido (Java)
            ];
        }
    }

    [Theory]
    [MemberData(nameof(Golden))]
    public void Correcao_bate_com_o_motor_oficial(
        IndiceMonetarioEnum indice, decimal valor, DateOnly vencimento, DateOnly liquidacao,
        decimal fatorEsperado, decimal corrigidoEsperado)
    {
        var r = Service.Corrigir(new PedidoDeCorrecao
        {
            Valor = valor,
            DataVencimento = vencimento,
            DataLiquidacao = liquidacao,
            Indice = indice,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
        });

        // Critério de aceitação: valor corrigido idêntico (2 casas, HALF_EVEN).
        Assert.Equal(corrigidoEsperado, r.ValorCorrigido);

        // Verificação secundária do fator acumulado (Java usa MathContext 38; decimal ~28
        // dígitos — comparamos a 10 casas, folgado dentro da precisão de ambos).
        Assert.Equal(Math.Round(fatorEsperado, 10), Math.Round(r.FatorAcumulado, 10));
    }
}
