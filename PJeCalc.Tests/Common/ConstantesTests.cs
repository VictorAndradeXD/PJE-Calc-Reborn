using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class ConstantesTests
{
    [Fact]
    public void DataReformaTrabalhista_IsCorrect()
    {
        // Assert - Labor Reform: November 11, 2017
        Assert.Equal(new DateTime(2017, 11, 11), Constantes.DataReformaTrabalhista);
    }

    [Fact]
    public void DataReformaPrevidencia_IsCorrect()
    {
        // Assert - Social Security Reform: March 1, 2020
        Assert.Equal(new DateTime(2020, 3, 1), Constantes.DataReformaPrevidencia);
    }

    [Fact]
    public void DataLimiteAvisoPrevioCalculado_IsCorrect()
    {
        // Assert - October 13, 2011
        Assert.Equal(new DateTime(2011, 10, 13), Constantes.DataLimiteAvisoPrevioCalculado);
    }

    [Fact]
    public void DiasAvisoPrevioPadrao_Is30()
    {
        Assert.Equal(30, Constantes.DiasAvisoPrevioPadrao);
    }

    [Fact]
    public void ConversoesDeMoeda_HasFiveEntries()
    {
        // Assert - 5 historical Brazilian currency conversions
        Assert.Equal(5, Constantes.ConversoesDeMoedaDiarias.Count);
        Assert.Equal(5, Constantes.ConversoesDeMoedaMensais.Count);
    }

    [Fact]
    public void ConversoesDeMoeda_RealConversion_HasFactor2750()
    {
        // Assert - Cruzeiro Real to Real (1994-07-01) used divisor 2,750
        var realConversionDate = new DateTime(1994, 7, 1);
        Assert.True(Constantes.ConversoesDeMoedaDiarias.ContainsKey(realConversionDate));
        Assert.Equal(2750m, Constantes.ConversoesDeMoedaDiarias[realConversionDate]);
    }

    [Fact]
    public void ConversoesDeMoeda_FirstConversion_Is1967()
    {
        // Assert - first conversion: Cruzeiro to Cruzeiro Novo (1967-02-13)
        var firstDate = new DateTime(1967, 2, 13);
        Assert.True(Constantes.ConversoesDeMoedaDiarias.ContainsKey(firstDate));
        Assert.Equal(1000m, Constantes.ConversoesDeMoedaDiarias[firstDate]);
    }

    [Fact]
    public void DataUltimaConversaoDeMoeda_Is1994()
    {
        Assert.Equal(new DateTime(1994, 7, 1), Constantes.DataUltimaConversaoDeMoeda);
    }

    [Fact]
    public void ObterFatorConversaoMoeda_NoConversions_ReturnsOne()
    {
        // Arrange - period with no currency conversions (after 1994)
        var dataInicio = new DateTime(2000, 1, 1);
        var dataFim = new DateTime(2024, 1, 1);

        // Act
        var fator = Constantes.ObterFatorConversaoMoeda(dataInicio, dataFim);

        // Assert
        Assert.Equal(1m, fator);
    }

    [Fact]
    public void ObterFatorConversaoMoeda_SpanningOneConversion_ReturnsDivisor()
    {
        // Arrange - spans the Real conversion (1994-07-01, divisor 2750)
        var dataInicio = new DateTime(1994, 6, 1);
        var dataFim = new DateTime(1994, 8, 1);

        // Act
        var fator = Constantes.ObterFatorConversaoMoeda(dataInicio, dataFim);

        // Assert
        Assert.Equal(2750m, fator);
    }

    [Fact]
    public void ObterFatorConversaoMoeda_SpanningMultipleConversions_MultipliesAll()
    {
        // Arrange - spans all 5 conversions (1967 to 1994)
        var dataInicio = new DateTime(1960, 1, 1);
        var dataFim = new DateTime(1995, 1, 1);

        // Act
        var fator = Constantes.ObterFatorConversaoMoeda(dataInicio, dataFim);

        // Assert - 1000 * 1000 * 1000 * 1000 * 2750
        Assert.Equal(1000m * 1000m * 1000m * 1000m * 2750m, fator);
    }
}
