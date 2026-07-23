using System.Globalization;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// O banco de referência embarcado devolve os mesmos parâmetros de custas que o CSV
/// exportado do H2 oficial (TBPARAMETROCUSTAS).
/// </summary>
public class EfParametroDeCustasProviderTests
{
    [Fact]
    public void Sqlite_embarcado_equivale_ao_csv_exportado()
    {
        var csv = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Custas", "parametro_custas.csv");
        var c = File.ReadAllLines(csv).Skip(1).First(l => !string.IsNullOrWhiteSpace(l)).Split(',');
        decimal V(int i) => decimal.Parse(c[i], NumberStyles.Float, CultureInfo.InvariantCulture);

        using var contexto = ReferenciaDbContextFactory.Criar();
        var p = new EfParametroDeCustasProvider(contexto).ObterPorData(new DateOnly(2021, 6, 10));

        Assert.Equal(V(2), p.PisoConhecimento);
        Assert.Equal(V(3), p.TetoLiquidacao);
        Assert.Equal(V(4), p.TetoAutos);
        Assert.Equal(V(5), p.AtosUrbanos);
        Assert.Equal(V(6), p.AtosRurais);
        Assert.Equal(V(7), p.AgravoInstrumento);
        Assert.Equal(V(8), p.AgravoPeticao);
        Assert.Equal(V(9), p.ImpugnacaoSentenca);
        Assert.Equal(V(10), p.EmbargosArrematacao);
        Assert.Equal(V(11), p.EmbargosExecucao);
        Assert.Equal(V(12), p.EmbargosTerceiros);
        Assert.Equal(V(13), p.RecursoRevista);
    }
}
