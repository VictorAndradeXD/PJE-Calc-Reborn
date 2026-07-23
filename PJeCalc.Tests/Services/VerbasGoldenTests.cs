using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação das primitivas de verbas contra o motor oficial do PJe-Calc (Java):
/// proporcionalização/integralização por dias (D=30, fevereiro 28/29, piso 0, teto 30) e
/// a fórmula da ocorrência (devido, diferença, diferença corrigida). Os valores esperados
/// foram gerados pelo CalculoDoProporcionalizar/Integralizar e OcorrenciaDeVerba reais.
/// </summary>
public class VerbasGoldenTests
{
    // tipo P/I; inicio; fim; valor; exclusões; resultado (golden do Java)
    public static IEnumerable<object[]> Proporcionalizacoes() =>
    [
        ["P", "2021-01-01", "2021-01-31", 3000.00m, 0, 3000.00m],
        ["I", "2021-01-01", "2021-01-31", 3000.00m, 0, 3000.00m],
        ["P", "2021-04-01", "2021-04-30", 3000.00m, 0, 3000.00m],
        ["I", "2021-04-01", "2021-04-30", 3000.00m, 0, 3000.00m],
        ["P", "2021-02-01", "2021-02-28", 3000.00m, 0, 3000.00m],
        ["I", "2021-02-01", "2021-02-28", 3000.00m, 0, 3000.00m],
        ["P", "2020-02-01", "2020-02-29", 3000.00m, 0, 3000.00m],
        ["I", "2020-02-01", "2020-02-29", 3000.00m, 0, 3000.00m],
        ["P", "2021-01-15", "2021-01-31", 3000.00m, 0, 1700.0000000000000000000000000m],
        ["I", "2021-01-15", "2021-01-31", 3000.00m, 0, 5294.1176470588235294117647059m],
        ["P", "2021-01-01", "2021-01-31", 3000.00m, 5, 2600.0000000000000000000000000m],
        ["I", "2021-01-01", "2021-01-31", 3000.00m, 5, 3461.5384615384615384615384615m],
        ["P", "2021-02-10", "2021-02-28", 2800.00m, 0, 1900.0000000000000000000000000m],
        ["I", "2021-02-10", "2021-02-28", 2800.00m, 0, 4126.3157894736842105263157895m],
        ["P", "2021-01-10", "2021-01-20", 3000.00m, 15, 0.00m],
        ["I", "2021-01-10", "2021-01-20", 3000.00m, 15, 0m],
    ];

    [Theory]
    [MemberData(nameof(Proporcionalizacoes))]
    public void Proporcionalizacao_bate_com_o_motor_oficial(
        string tipo, string inicio, string fim, decimal valor, int exclusoes, decimal esperado)
    {
        var ini = DateOnly.Parse(inicio);
        var f = DateOnly.Parse(fim);

        var obtido = tipo == "P"
            ? Proporcionalizacao.Proporcionalizar(ini, f, valor, exclusoes)
            : Proporcionalizacao.Integralizar(ini, f, valor, exclusoes);

        var escala = Math.Max(Math.Abs(esperado), 1m);
        Assert.True(Math.Abs(obtido - esperado) <= escala * 0.0000000001m,
            $"{tipo} {inicio}..{fim}: obtido {obtido}, esperado {esperado}.");
    }

    // base; divisor; mult; qtd; dobra; pago; índice; zeraNeg; devido; diferença; difCorrigida
    public static IEnumerable<object[]> Ocorrencias() =>
    [
        [2200.00m, 220m, 1.5m, 20m, false, 0m, 1.3m, true, 300.00m, 300.00m, 390.000m],
        [2200.00m, 220m, 1.5m, 20m, false, 120.50m, 1.3m, true, 300.00m, 179.50m, 233.350m],
        [1500.00m, 30m, 1m, 5m, true, 0m, 1.15m, true, 500.00m, 500.00m, 575.0000m],
        [1000.00m, 220m, 1.5m, 10m, false, 500.00m, 1.2m, true, 68.18m, 0m, 0.0m],
        [1000.00m, 220m, 1.5m, 10m, false, 500.00m, 1.2m, false, 68.18m, -431.82m, -518.184m],
        [3333.33m, 200m, 1.6m, 17.5m, false, 0m, 1.0m, true, 466.67m, 466.67m, 466.670m],
    ];

    [Theory]
    [MemberData(nameof(Ocorrencias))]
    public void Ocorrencia_de_verba_bate_com_o_motor_oficial(
        decimal baseCalculo, decimal divisor, decimal multiplicador, decimal quantidade,
        bool dobra, decimal pago, decimal indice, bool zeraNegativo,
        decimal devidoEsperado, decimal diferencaEsperada, decimal corrigidaEsperada)
    {
        var o = new OcorrenciaDeVerbaCalculo
        {
            Base = baseCalculo,
            Divisor = divisor,
            Multiplicador = multiplicador,
            Quantidade = quantidade,
            Dobra = dobra,
            Pago = pago,
            IndiceAcumulado = indice,
            ZeraValorNegativo = zeraNegativo,
        };

        Assert.Equal(devidoEsperado, o.Devido);
        Assert.Equal(diferencaEsperada, o.Diferenca);
        Assert.Equal(corrigidaEsperada, o.DiferencaCorrigida);
    }

    [Fact]
    public void Regra_do_31_ajusta_as_exclusoes()
    {
        // Mês de 31 dias sem exclusões: conta como 1 dia a excluir (o resultado é o mesmo
        // valor cheio, porque o teto de 30 dias já satura).
        Assert.Equal(1, Proporcionalizacao.AjustarExclusoesParaMesDe31(31, 0));
        Assert.Equal(5, Proporcionalizacao.AjustarExclusoesParaMesDe31(31, 5));
        Assert.Equal(0, Proporcionalizacao.AjustarExclusoesParaMesDe31(30, 0));
    }
}
