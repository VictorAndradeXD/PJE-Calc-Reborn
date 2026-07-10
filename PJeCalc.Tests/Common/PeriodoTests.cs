using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class PeriodoTests
{
    [Fact]
    public void TotalDeDias_SingleDay_ReturnsOne()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1));

        // Act
        var result = periodo.TotalDeDias();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void TotalDeDias_ThirtyDays_ReturnsThirty()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 30));

        // Act
        var result = periodo.TotalDeDias();

        // Assert
        Assert.Equal(30, result);
    }

    [Theory]
    [InlineData(2024, 1, 1, 2024, 1, 1, 1)]
    [InlineData(2024, 1, 1, 2024, 1, 31, 31)]
    [InlineData(2024, 3, 1, 2024, 3, 31, 31)]
    [InlineData(2024, 2, 1, 2024, 2, 29, 29)] // Leap year
    public void TotalDeDias_ReturnsCorrectCount(int y1, int m1, int d1, int y2, int m2, int d2, int expected)
    {
        // Arrange
        var periodo = new Periodo(new DateTime(y1, m1, d1), new DateTime(y2, m2, d2));

        // Act
        var result = periodo.TotalDeDias();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Intersecta_OverlappingPeriods_ReturnsTrue()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var p2 = new Periodo(new DateTime(2024, 1, 15), new DateTime(2024, 2, 15));

        // Act
        var result = p1.Intersecta(p2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Intersecta_AdjacentPeriods_ReturnsTrue()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 15));
        var p2 = new Periodo(new DateTime(2024, 1, 15), new DateTime(2024, 1, 31));

        // Act
        var result = p1.Intersecta(p2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Intersecta_NonOverlappingPeriods_ReturnsFalse()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 14));
        var p2 = new Periodo(new DateTime(2024, 2, 1), new DateTime(2024, 2, 28));

        // Act
        var result = p1.Intersecta(p2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Interseccao_ReturnsOverlapPeriod()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var p2 = new Periodo(new DateTime(2024, 1, 15), new DateTime(2024, 2, 15));

        // Act
        var result = p1.Interseccao(p2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 1, 15), result.DataInicial);
        Assert.Equal(new DateTime(2024, 1, 31), result.DataFinal);
    }

    [Fact]
    public void Interseccao_NoOverlap_ReturnsNull()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 14));
        var p2 = new Periodo(new DateTime(2024, 2, 1), new DateTime(2024, 2, 28));

        // Act
        var result = p1.Interseccao(p2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ContemData_DateInsidePeriod_ReturnsTrue()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        // Act & Assert
        Assert.True(periodo.ContemData(new DateTime(2024, 1, 15)));
    }

    [Fact]
    public void ContemData_DateOutsidePeriod_ReturnsFalse()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        // Act & Assert
        Assert.False(periodo.ContemData(new DateTime(2024, 2, 1)));
    }

    [Fact]
    public void IsCompleto_BothDatesSet_ReturnsTrue()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        // Act & Assert
        Assert.True(periodo.IsCompleto());
    }

    [Fact]
    public void IsCompleto_DefaultDates_ReturnsFalse()
    {
        // Arrange
        var periodo = new Periodo();

        // Act & Assert
        Assert.False(periodo.IsCompleto());
    }

    [Fact]
    public void IsMesmoPeriodo_SameDates_ReturnsTrue()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var p2 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        // Act & Assert
        Assert.True(p1.IsMesmoPeriodo(p2));
    }

    [Fact]
    public void IsDatasDoMesmoMes_SameMonth_ReturnsTrue()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 3, 5), new DateTime(2024, 3, 20));

        // Act & Assert
        Assert.True(periodo.IsDatasDoMesmoMes());
    }

    [Fact]
    public void IsDatasDoMesmoMes_DifferentMonths_ReturnsFalse()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 3, 5), new DateTime(2024, 4, 20));

        // Act & Assert
        Assert.False(periodo.IsDatasDoMesmoMes());
    }

    [Fact]
    public void Equals_SamePeriods_ReturnsTrue()
    {
        // Arrange
        var p1 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        var p2 = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

        // Act & Assert
        Assert.Equal(p1, p2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var periodo = new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Act
        var result = periodo.ToString();

        // Assert
        Assert.Equal("01/01/2024 - 31/12/2024", result);
    }
}
