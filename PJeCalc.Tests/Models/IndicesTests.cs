using PJeCalc.Core.Models.Indices;

namespace PJeCalc.Tests.Models;

public class IndicesTests
{
    [Fact]
    public void IndiceIGPM_ExtendsIndiceBase()
    {
        // Arrange & Act
        var indice = new IndiceIGPM();

        // Assert
        Assert.IsAssignableFrom<IndiceBase>(indice);
    }

    [Fact]
    public void IndiceINPC_ExtendsIndiceBase()
    {
        // Arrange & Act
        var indice = new IndiceINPC();

        // Assert
        Assert.IsAssignableFrom<IndiceBase>(indice);
    }

    [Fact]
    public void IndiceIPCA_ExtendsIndiceBase()
    {
        // Arrange & Act
        var indice = new IndiceIPCA();

        // Assert
        Assert.IsAssignableFrom<IndiceBase>(indice);
    }

    [Fact]
    public void IndiceTR_ExtendsIndiceBase()
    {
        // Arrange & Act
        var indice = new IndiceTR();

        // Assert
        Assert.IsAssignableFrom<IndiceBase>(indice);
    }

    [Fact]
    public void CanSetCompetenciaAndTaxa()
    {
        // Arrange
        var indice = new IndiceIGPM();
        var competencia = new DateTime(2024, 3, 1);

        // Act
        indice.Competencia = competencia;
        indice.Taxa = 1.0234m;

        // Assert
        Assert.Equal(competencia, indice.Competencia);
        Assert.Equal(1.0234m, indice.Taxa);
    }

    [Fact]
    public void CanSetDataCriacao()
    {
        // Arrange
        var indice = new IndiceIPCA();
        var dataCriacao = new DateTime(2024, 6, 15, 10, 30, 0);

        // Act
        indice.DataCriacao = dataCriacao;

        // Assert
        Assert.Equal(dataCriacao, indice.DataCriacao);
    }

    [Fact]
    public void NewIndice_DataCriacao_IsNull()
    {
        // Arrange & Act
        var indice = new IndiceIGPM();

        // Assert
        Assert.Null(indice.DataCriacao);
    }

    [Fact]
    public void NewIndice_Taxa_DefaultsToZero()
    {
        // Arrange & Act
        var indice = new IndiceIGPM();

        // Assert
        Assert.Equal(0m, indice.Taxa);
    }

    [Fact]
    public void NewIndice_Id_DefaultsToZero()
    {
        // Arrange & Act
        var indice = new IndiceIGPM();

        // Assert
        Assert.Equal(0, indice.Id);
    }

    [Fact]
    public void AllIndiceSubclasses_AreAssignableFromIndiceBase()
    {
        // Verify all concrete index types inherit from IndiceBase
        Assert.IsAssignableFrom<IndiceBase>(new IndiceIGPM());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceINPC());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceIPC());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceIPCA());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceIPCAE());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceIPCAETR());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceJAM());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceSelicDiaria());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceSelicFazenda());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceTR());
        Assert.IsAssignableFrom<IndiceBase>(new IndiceTUACDT());
    }
}
