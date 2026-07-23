using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// O banco de referência embarcado devolve exatamente os mesmos feriados que os CSVs
/// exportados do H2 oficial (o provedor em memória já validado).
/// </summary>
public class EfFeriadoProviderGoldenTests
{
    [Fact]
    public void Sqlite_embarcado_equivale_aos_csvs_exportados()
    {
        var doCsv = ProvedorDeFeriadosFixture.CarregarDoCsv();

        using var contexto = ReferenciaDbContextFactory.Criar();
        var doSqlite = new EfFeriadoProvider(contexto).CarregarTodos();

        Assert.Equal(doCsv.Count, doSqlite.Count);
        foreach (var (esperado, obtido) in doCsv.Zip(doSqlite))
        {
            Assert.Equal(esperado.Nome, obtido.Nome);
            Assert.Equal(esperado.Tipo, obtido.Tipo);
            Assert.Equal(esperado.Abrangencia, obtido.Abrangencia);
            Assert.Equal(esperado.Estado, obtido.Estado);
            Assert.Equal(esperado.Municipio, obtido.Municipio);
            Assert.Equal(esperado.Data, obtido.Data);
            Assert.Equal(esperado.InicioVigencia, obtido.InicioVigencia);
            Assert.Equal(esperado.FimVigencia, obtido.FimVigencia);
            Assert.Equal(esperado.Movel, obtido.Movel);
            Assert.Equal(esperado.Excecoes.Order(), obtido.Excecoes.Order());
        }
    }

    [Fact]
    public void Predicado_do_sqlite_reconhece_feriado_nacional()
    {
        using var contexto = ReferenciaDbContextFactory.Criar();
        var ehFeriado = new EfFeriadoProvider(contexto).CriarProvedor().ParaCalculo(null, null);
        Assert.True(ehFeriado(new DateOnly(2021, 9, 7)));
        Assert.False(ehFeriado(new DateOnly(2021, 9, 8)));
    }
}
