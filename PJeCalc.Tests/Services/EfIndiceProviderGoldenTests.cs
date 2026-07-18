using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Integração de runtime: corrige valores lendo os índices do banco de referência
/// pré-construído (referencia.sqlite) via <see cref="EfIndiceProvider"/>, e confere
/// contra os mesmos golden values do motor oficial. Prova o caminho fim-a-fim da app
/// (EF → provider → service) usando o artefato realmente embarcado.
/// </summary>
public class EfIndiceProviderGoldenTests
{
    private static ReferenciaDbContext AbrirReferencia()
    {
        Assert.True(File.Exists(ReferenciaDbContextFactory.CaminhoPadrao),
            $"referencia.sqlite não encontrado em {ReferenciaDbContextFactory.CaminhoPadrao}");
        return ReferenciaDbContextFactory.Criar();
    }

    [Theory]
    [MemberData(nameof(CorrecaoMonetariaGoldenTests.Golden), MemberType = typeof(CorrecaoMonetariaGoldenTests))]
    public void Correcao_via_EF_bate_com_o_motor_oficial(
        IndiceMonetarioEnum indice, decimal valor, DateOnly vencimento, DateOnly liquidacao,
        bool ignorarNegativa, decimal fatorEsperado, decimal corrigidoEsperado)
    {
        _ = fatorEsperado;

        using var ctx = AbrirReferencia();
        var service = new CorrecaoMonetariaService(new EfIndiceProvider(ctx));

        var r = service.Corrigir(new PedidoDeCorrecao
        {
            Valor = valor,
            DataVencimento = vencimento,
            DataLiquidacao = liquidacao,
            Indice = indice,
            Regime = IndicesAcumuladosEnum.MesDoVencimento,
            IgnorarTaxaNegativa = ignorarNegativa,
        });

        Assert.Equal(corrigidoEsperado, r.ValorCorrigido);
    }

    [Fact]
    public void Banco_de_referencia_tem_as_series_mensais_esperadas()
    {
        using var ctx = AbrirReferencia();
        var provider = new EfIndiceProvider(ctx);

        foreach (var indice in new[]
        {
            IndiceMonetarioEnum.IGPM, IndiceMonetarioEnum.INPC, IndiceMonetarioEnum.IPC,
            IndiceMonetarioEnum.IPCA, IndiceMonetarioEnum.IPCAE, IndiceMonetarioEnum.IPCAETR,
            IndiceMonetarioEnum.TR, IndiceMonetarioEnum.SelicFazenda,
        })
        {
            var serie = provider.ObterSerieMensal(indice);
            Assert.NotEmpty(serie);
            // Ordenada por competência ascendente.
            Assert.True(serie.Zip(serie.Skip(1)).All(par => par.First.Competencia <= par.Second.Competencia),
                $"Série de {indice} não está ordenada por competência.");
        }
    }

    [Fact]
    public void Indice_sem_serie_no_provider_lanca_NotSupported()
    {
        using var ctx = AbrirReferencia();
        var provider = new EfIndiceProvider(ctx);
        Assert.Throws<NotSupportedException>(() => provider.ObterSerieMensal(IndiceMonetarioEnum.JAM));
    }
}
