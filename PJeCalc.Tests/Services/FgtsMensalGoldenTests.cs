using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Fgts;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação da matemática por competência do FGTS (<see cref="ApuracaoMensalDeFgts"/>)
/// contra o motor oficial do PJe-Calc (Java): valor devido, diferença, correção pelos
/// dois índices (liquidação e demissão), juros, total e a contribuição social de 0,5%.
/// Casos em Fixtures/golden_fgts_mensal.csv.
/// </summary>
public class FgtsMensalGoldenTests
{
    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_fgts_mensal.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return
            [
                new ApuracaoMensalDeFgts
                {
                    Competencia = DateOnly.ParseExact(c[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Aliquota = c[2] == "2" ? AliquotaDoFgtsEnum.DoisPorCento : AliquotaDoFgtsEnum.OitoPorCento,
                    BaseHistorico = Num(c[3]),
                    BaseVerba = Num(c[4]),
                    BaseVerbaSemAvisoPrevio = Num(c[5]),
                    Depositado = Num(c[6]),
                    IndiceAcumulado = Num(c[7]),
                    IndiceAcumuladoDaMulta = Num(c[8]),
                    TaxaDeJuros = Num(c[9]),
                },
                Num(c[10]), Num(c[11]), Num(c[12]), Num(c[13]), Num(c[14]), Num(c[15]), Num(c[16]), Num(c[17]),
            ];
        }
    }

    private static decimal Num(string s) => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    [Theory]
    [MemberData(nameof(Golden))]
    public void Apuracao_mensal_bate_com_o_motor_oficial(
        ApuracaoMensalDeFgts a,
        decimal valorDevido, decimal valorDevidoSemAviso, decimal diferenca,
        decimal difCorrigidaLiquidacao, decimal difCorrigidaDemissao,
        decimal juros, decimal total, decimal contribSocial05)
    {
        const TipoDeCorrecaoDoFgtsEnum liq = TipoDeCorrecaoDoFgtsEnum.PelaDataDeLiquidacao;
        const TipoDeCorrecaoDoFgtsEnum dem = TipoDeCorrecaoDoFgtsEnum.PelaDataDeDemissao;

        AssertProximo(valorDevido, a.ValorDevido, nameof(a.ValorDevido));
        AssertProximo(valorDevidoSemAviso, a.ValorDevidoSemAviso, nameof(a.ValorDevidoSemAviso));
        AssertProximo(diferenca, a.Diferenca(), nameof(a.Diferenca));
        AssertProximo(difCorrigidaLiquidacao, a.DiferencaCorrigida(liq), "DiferencaCorrigida(liquidação)");
        AssertProximo(difCorrigidaDemissao, a.DiferencaCorrigida(dem), "DiferencaCorrigida(demissão)");
        AssertProximo(juros, a.Juros(liq), nameof(a.Juros));
        AssertProximo(total, a.Total(liq), nameof(a.Total));
        AssertProximo(contribSocial05, a.ContribuicaoSocialDe05, nameof(a.ContribuicaoSocialDe05));
    }

    private static void AssertProximo(decimal esperado, decimal obtido, string campo)
    {
        var escala = Math.Max(Math.Abs(esperado), 1m);
        Assert.True(Math.Abs(obtido - esperado) <= escala * 0.0000000001m,
            $"{campo}: obtido {obtido} distante do esperado {esperado}.");
    }
}
