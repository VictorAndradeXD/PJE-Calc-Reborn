using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class LogicoFuzzyTests
{
    [Fact]
    public void Verdadeiro_ReturnsTrueByDefault()
    {
        // Arrange
        var fuzzy = LogicoFuzzy.Verdadeiro;

        // Act & Assert
        Assert.True(fuzzy.ValorBase);
        Assert.True(fuzzy.IsValido(new DateTime(2024, 6, 15)));
    }

    [Fact]
    public void Falso_ReturnsFalseByDefault()
    {
        // Arrange
        var fuzzy = LogicoFuzzy.Falso;

        // Act & Assert
        Assert.False(fuzzy.ValorBase);
        Assert.False(fuzzy.IsValido(new DateTime(2024, 6, 15)));
    }

    [Fact]
    public void IsValido_WithExcecao_InvertsInExceptionPeriod()
    {
        // Arrange - base is true, but inverted during January 2024
        var fuzzy = new LogicoFuzzy(true, new List<Periodo>
        {
            new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31))
        });

        // Act - date inside exception period
        var result = fuzzy.IsValido(new DateTime(2024, 1, 15));

        // Assert - should be inverted (false)
        Assert.False(result);
    }

    [Fact]
    public void IsValido_WithExcecao_KeepsBaseOutsideExceptionPeriod()
    {
        // Arrange - base is true, exception during January 2024
        var fuzzy = new LogicoFuzzy(true, new List<Periodo>
        {
            new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31))
        });

        // Act - date outside exception period
        var result = fuzzy.IsValido(new DateTime(2024, 6, 15));

        // Assert - should keep base value (true)
        Assert.True(result);
    }

    [Fact]
    public void IsValido_FalseBase_WithExcecao_InvertsToTrue()
    {
        // Arrange - base is false, inverted to true during exception
        var fuzzy = new LogicoFuzzy(false, new List<Periodo>
        {
            new Periodo(new DateTime(2024, 3, 1), new DateTime(2024, 3, 31))
        });

        // Act
        var resultInside = fuzzy.IsValido(new DateTime(2024, 3, 15));
        var resultOutside = fuzzy.IsValido(new DateTime(2024, 4, 15));

        // Assert
        Assert.True(resultInside);   // Inverted to true
        Assert.False(resultOutside); // Keeps base (false)
    }

    [Fact]
    public void IsValido_WithMultipleExcecoes()
    {
        // Arrange - base is true, with two exception periods
        var fuzzy = new LogicoFuzzy(true, new List<Periodo>
        {
            new Periodo(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31)),
            new Periodo(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30))
        });

        // Act & Assert
        Assert.False(fuzzy.IsValido(new DateTime(2024, 1, 15)));  // In first exception
        Assert.True(fuzzy.IsValido(new DateTime(2024, 3, 15)));   // Between exceptions
        Assert.False(fuzzy.IsValido(new DateTime(2024, 6, 15)));  // In second exception
        Assert.True(fuzzy.IsValido(new DateTime(2024, 8, 15)));   // After exceptions
    }

    [Fact]
    public void IsValido_ExcecaoBoundary_IncludesStartAndEndDates()
    {
        // Arrange
        var fuzzy = new LogicoFuzzy(true, new List<Periodo>
        {
            new Periodo(new DateTime(2024, 3, 10), new DateTime(2024, 3, 20))
        });

        // Act & Assert
        Assert.True(fuzzy.IsValido(new DateTime(2024, 3, 9)));    // Before exception
        Assert.False(fuzzy.IsValido(new DateTime(2024, 3, 10)));  // Start of exception (inclusive)
        Assert.False(fuzzy.IsValido(new DateTime(2024, 3, 15)));  // Inside exception
        Assert.False(fuzzy.IsValido(new DateTime(2024, 3, 20)));  // End of exception (inclusive)
        Assert.True(fuzzy.IsValido(new DateTime(2024, 3, 21)));   // After exception
    }

    [Fact]
    public void DefaultConstructor_EmptyExcecoes()
    {
        // Arrange
        var fuzzy = new LogicoFuzzy();

        // Act & Assert
        Assert.Empty(fuzzy.Excecoes);
        Assert.False(fuzzy.ValorBase); // Default bool
    }
}
