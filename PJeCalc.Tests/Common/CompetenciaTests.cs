using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class CompetenciaTests
{
    [Fact]
    public void CriarPeriodoDaCompetencia_ReturnsFullMonth()
    {
        // Arrange
        var competencia = new Competencia(3, 2024); // March 2024

        // Act
        var periodo = competencia.CriarPeriodoDaCompetencia();

        // Assert
        Assert.Equal(new DateTime(2024, 3, 1), periodo.DataInicial);
        Assert.Equal(new DateTime(2024, 3, 31), periodo.DataFinal);
    }

    [Fact]
    public void CriarPeriodoDaCompetencia_February_LeapYear()
    {
        // Arrange
        var competencia = new Competencia(2, 2024); // Feb 2024 (leap year)

        // Act
        var periodo = competencia.CriarPeriodoDaCompetencia();

        // Assert
        Assert.Equal(new DateTime(2024, 2, 1), periodo.DataInicial);
        Assert.Equal(new DateTime(2024, 2, 29), periodo.DataFinal);
    }

    [Fact]
    public void CriarPeriodoDaCompetencia_February_NonLeapYear()
    {
        // Arrange
        var competencia = new Competencia(2, 2023);

        // Act
        var periodo = competencia.CriarPeriodoDaCompetencia();

        // Assert
        Assert.Equal(new DateTime(2023, 2, 1), periodo.DataInicial);
        Assert.Equal(new DateTime(2023, 2, 28), periodo.DataFinal);
    }

    [Fact]
    public void IsAnteriorA_BeforeDate_ReturnsTrue()
    {
        // Arrange - competencia January 2024
        var competencia = new Competencia(1, 2024);
        // Date in March 2024 (after the competencia's last day Jan 31)
        var data = new DateTime(2024, 3, 1);

        // Act
        var result = competencia.IsAnteriorA(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAnteriorA_AfterDate_ReturnsFalse()
    {
        // Arrange - competencia June 2024
        var competencia = new Competencia(6, 2024);
        // Date in January 2024 (before the competencia)
        var data = new DateTime(2024, 1, 1);

        // Act
        var result = competencia.IsAnteriorA(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAnteriorA_SameMonth_ReturnsFalse()
    {
        // Arrange - competencia January 2024
        var competencia = new Competencia(1, 2024);
        // Date inside the competencia month
        var data = new DateTime(2024, 1, 15);

        // Act
        var result = competencia.IsAnteriorA(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPosteriorA_AfterDate_ReturnsTrue()
    {
        // Arrange - competencia June 2024
        var competencia = new Competencia(6, 2024);
        // Date in January 2024
        var data = new DateTime(2024, 1, 1);

        // Act
        var result = competencia.IsPosteriorA(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContemData_DateInCompetencia_ReturnsTrue()
    {
        // Arrange
        var competencia = new Competencia(5, 2024);
        var data = new DateTime(2024, 5, 15);

        // Act & Assert
        Assert.True(competencia.ContemData(data));
    }

    [Fact]
    public void ContemData_DateOutsideCompetencia_ReturnsFalse()
    {
        // Arrange
        var competencia = new Competencia(5, 2024);
        var data = new DateTime(2024, 6, 1);

        // Act & Assert
        Assert.False(competencia.ContemData(data));
    }

    [Fact]
    public void Equals_SameCompetencia_ReturnsTrue()
    {
        // Arrange
        var c1 = new Competencia(3, 2024);
        var c2 = new Competencia(3, 2024);

        // Act & Assert
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void Equals_DifferentCompetencia_ReturnsFalse()
    {
        // Arrange
        var c1 = new Competencia(3, 2024);
        var c2 = new Competencia(4, 2024);

        // Act & Assert
        Assert.NotEqual(c1, c2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var competencia = new Competencia(3, 2024);

        // Act
        var result = competencia.ToString();

        // Assert
        Assert.Equal("03/2024", result);
    }

    [Fact]
    public void ToString_SingleDigitMonth_HasLeadingZero()
    {
        // Arrange
        var competencia = new Competencia(1, 2024);

        // Act
        var result = competencia.ToString();

        // Assert
        Assert.Equal("01/2024", result);
    }
}
