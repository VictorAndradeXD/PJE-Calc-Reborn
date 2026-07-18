using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Testes estruturais do walking skeleton da correção monetária. Validam as regras
/// de acumulação (multiplicativa vs aditiva), tratamento de taxa negativa, regime de
/// competência inicial e a convenção de fator negativo, com séries sintéticas.
///
/// A validação contra os valores oficiais (golden values do PJe-Calc Java) virá em
/// arquivo separado, com dados reais de índice.
/// </summary>
public class CorrecaoMonetariaServiceTests
{
    private static CorrecaoMonetariaService ComSerie(params IndiceMensal[] serie) =>
        new(new SerieFixa(serie));

    private static DateOnly Comp(int ano, int mes) => new(ano, mes, 1);

    [Fact]
    public void Acumulacao_multiplicativa_e_o_produtorio_dos_fatores_mensais()
    {
        // Jan 10% e Fev 10% => 1,10 * 1,10 = 1,21 => R$ 100,00 -> R$ 121,00
        var service = ComSerie(
            new IndiceMensal(Comp(2020, 1), 10m),
            new IndiceMensal(Comp(2020, 2), 10m));

        var r = service.Corrigir(new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 15),
            DataLiquidacao = new DateOnly(2020, 2, 10),
            Indice = IndiceMonetarioEnum.IPCAE,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
        });

        Assert.Equal(1.21m, r.FatorAcumulado);
        Assert.Equal(121.00m, r.ValorCorrigido);
        Assert.Equal(2, r.MesesConsiderados);
    }

    [Fact]
    public void Selic_acumula_de_forma_aditiva_e_nao_composta()
    {
        // Jan 1% e Fev 1% => aditivo 1 + 0,01 + 0,01 = 1,02 (e NÃO 1,0201 composto)
        var service = ComSerie(
            new IndiceMensal(Comp(2020, 1), 1m),
            new IndiceMensal(Comp(2020, 2), 1m));

        var r = service.Corrigir(new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 15),
            DataLiquidacao = new DateOnly(2020, 2, 10),
            Indice = IndiceMonetarioEnum.SelicFazenda,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
        });

        Assert.Equal(1.02m, r.FatorAcumulado);
        Assert.Equal(102.00m, r.ValorCorrigido);
    }

    [Fact]
    public void IgnorarTaxaNegativa_trata_mes_deflacionario_como_fator_um()
    {
        var serie = new[]
        {
            new IndiceMensal(Comp(2020, 1), 10m),
            new IndiceMensal(Comp(2020, 2), -5m),
            new IndiceMensal(Comp(2020, 3), 10m),
        };

        var pedido = new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 10),
            DataLiquidacao = new DateOnly(2020, 3, 10),
            Indice = IndiceMonetarioEnum.IPCAE,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
        };

        var ignorando = ComSerie(serie).Corrigir(pedido with { IgnorarTaxaNegativa = true });
        var considerando = ComSerie(serie).Corrigir(pedido with { IgnorarTaxaNegativa = false });

        Assert.Equal(1.21m, ignorando.FatorAcumulado);              // 1,10 * 1,10 (Fev vira fator 1)
        Assert.Equal(1.1495m, considerando.FatorAcumulado);         // 1,10 * 0,95 * 1,10
    }

    [Fact]
    public void Regime_subsequente_desloca_a_competencia_inicial_em_um_mes()
    {
        var serie = new[]
        {
            new IndiceMensal(Comp(2020, 1), 10m),
            new IndiceMensal(Comp(2020, 2), 10m),
        };
        var pedido = new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 15),
            DataLiquidacao = new DateOnly(2020, 2, 10),
            Indice = IndiceMonetarioEnum.IPCAE,
        };

        var doVencimento = ComSerie(serie).Corrigir(pedido with { Regime = IndicesAcumuladosEnum.MesDoVencimento });
        var subsequente = ComSerie(serie).Corrigir(pedido with { Regime = IndicesAcumuladosEnum.MesSubsequenteAoVencimento });

        Assert.Equal(Comp(2020, 1), doVencimento.CompetenciaInicial);
        Assert.Equal(1.21m, doVencimento.FatorAcumulado);

        Assert.Equal(Comp(2020, 2), subsequente.CompetenciaInicial); // pula janeiro
        Assert.Equal(1.10m, subsequente.FatorAcumulado);
    }

    [Fact]
    public void Fator_negativo_divide_o_valor_por_menos_fator()
    {
        // Taxa de -150% => fator -0,5 => 100 / 0,5 = 200
        var service = ComSerie(new IndiceMensal(Comp(2020, 1), -150m));

        var r = service.Corrigir(new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 10),
            DataLiquidacao = new DateOnly(2020, 1, 20),
            Indice = IndiceMonetarioEnum.IPCAE,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
        });

        Assert.Equal(-0.5m, r.FatorAcumulado);
        Assert.Equal(200.00m, r.ValorCorrigido);
    }

    [Fact]
    public void SemCorrecao_devolve_o_valor_inalterado()
    {
        var service = ComSerie(new IndiceMensal(Comp(2020, 1), 10m));

        var r = service.Corrigir(new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 1, 10),
            DataLiquidacao = new DateOnly(2021, 1, 10),
            Indice = IndiceMonetarioEnum.SemCorrecao,
        });

        Assert.Equal(1m, r.FatorAcumulado);
        Assert.Equal(100.00m, r.ValorCorrigido);
        Assert.Equal(0, r.MesesConsiderados);
    }

    [Fact]
    public void Liquidacao_anterior_ao_vencimento_e_rejeitada()
    {
        var service = ComSerie(new IndiceMensal(Comp(2020, 1), 10m));

        Assert.Throws<ArgumentException>(() => service.Corrigir(new PedidoDeCorrecao
        {
            Valor = 100m,
            DataVencimento = new DateOnly(2020, 5, 10),
            DataLiquidacao = new DateOnly(2020, 1, 10),
            Indice = IndiceMonetarioEnum.IPCAE,
        }));
    }

    /// <summary>Provider de teste que devolve sempre a mesma série, ignorando o índice pedido.</summary>
    private sealed class SerieFixa(IReadOnlyList<IndiceMensal> serie) : IIndiceProvider
    {
        public IReadOnlyList<IndiceMensal> ObterSerieMensal(IndiceMonetarioEnum indice) => serie;
    }
}
