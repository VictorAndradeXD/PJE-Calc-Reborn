using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Integração de runtime dos juros: calcula a taxa acumulada do JurosPadrão lendo as
/// faixas do banco de referência pré-construído (referencia.sqlite) via
/// <see cref="EfJurosFaixaProvider"/>, conferindo contra os mesmos golden do motor oficial.
/// </summary>
public class EfJurosFaixaProviderGoldenTests
{
    [Theory]
    [MemberData(nameof(JurosTabelaGoldenTests.Golden), MemberType = typeof(JurosTabelaGoldenTests))]
    public void Taxa_via_EF_bate_com_o_motor_oficial(
        JurosEnum regime, DateOnly inicio, DateOnly liquidacao, decimal taxaEsperada)
    {
        using var ctx = ReferenciaDbContextFactory.Criar();
        var service = new TabelaDeJurosService(new EfJurosFaixaProvider(ctx));

        var taxa = service.CalcularTaxaAcumulada(regime, inicio, liquidacao);

        var escala = Math.Max(Math.Abs(taxaEsperada), 1m);
        Assert.True(Math.Abs(taxa - taxaEsperada) <= escala * 0.0000000001m,
            $"Taxa {taxa} distante do esperado {taxaEsperada}.");
    }

    [Fact]
    public void Banco_de_referencia_tem_as_faixas_do_juros_padrao()
    {
        using var ctx = ReferenciaDbContextFactory.Criar();
        var provider = new EfJurosFaixaProvider(ctx);

        // Cobre todo o histórico: deve trazer as três faixas (0,5% -> 1% comp -> 1% simples).
        var faixas = provider.ObterFaixas(JurosEnum.JurosPadrao, new(1960, 1, 1), new(2030, 1, 1));

        Assert.Equal(3, faixas.Count);
        Assert.Null(faixas[^1].DataFim); // a última faixa é aberta
    }
}
