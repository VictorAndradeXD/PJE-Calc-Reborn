using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class UtilsTests
{
    [Fact]
    public void AplicarCorrecaoMonetaria_MultipliesCorrectly()
    {
        // Arrange
        decimal valor = 1000m;
        decimal indice = 1.5m;

        // Act
        var result = Utils.AplicarCorrecaoMonetaria(valor, indice);

        // Assert
        Assert.Equal(1500m, result);
    }

    [Fact]
    public void AplicarCorrecaoMonetaria_IndexZero_ReturnsOriginalValue()
    {
        // Arrange
        decimal valor = 1000m;
        decimal indice = 0m;

        // Act
        var result = Utils.AplicarCorrecaoMonetaria(valor, indice);

        // Assert
        Assert.Equal(1000m, result);
    }

    [Fact]
    public void AplicarJuros_MultipliesCorrectly()
    {
        // Arrange
        decimal valor = 1000m;
        decimal taxa = 12m; // 12%

        // Act
        var result = Utils.AplicarJuros(valor, taxa);

        // Assert
        Assert.Equal(120m, result);
    }

    [Fact]
    public void AplicarMulta_CalculatesCorrectly()
    {
        // Arrange
        decimal valor = 2000m;
        decimal percentual = 40m; // 40%

        // Act
        var result = Utils.AplicarMulta(valor, percentual);

        // Assert
        Assert.Equal(800m, result);
    }

    [Theory]
    [InlineData(10.555, 10.56)]
    [InlineData(10.554, 10.55)]
    [InlineData(10.545, 10.55)] // MidpointRounding.AwayFromZero
    [InlineData(100.999, 101.00)]
    [InlineData(0.001, 0.00)]
    public void ArredondarValorMonetario_RoundsTo2Decimals(decimal input, decimal expected)
    {
        // Act
        var result = Utils.ArredondarValorMonetario(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(-100.50, 0.0)]
    [InlineData(-0.01, 0.0)]
    public void ZerarSeNegativo_ReturnsZeroForNegative(decimal input, decimal expected)
    {
        // Act
        var result = Utils.ZerarSeNegativo(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(100.50, 100.50)]
    public void ZerarSeNegativo_ReturnsValueForPositive(decimal input, decimal expected)
    {
        // Act
        var result = Utils.ZerarSeNegativo(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatarMoeda_FormatsInBRL()
    {
        // Arrange
        decimal valor = 1234.56m;

        // Act
        var result = Utils.FormatarMoeda(valor);

        // Assert
        Assert.Contains("1.234,56", result);
        Assert.Contains("R$", result);
    }

    [Fact]
    public void FormatarData_FormatsAsDDMMYYYY()
    {
        // Arrange
        var data = new DateTime(2024, 3, 15);

        // Act
        var result = Utils.FormatarData(data);

        // Assert
        Assert.Equal("15/03/2024", result);
    }

    [Fact]
    public void FormatarCPF_FormatsCorrectly()
    {
        // Arrange
        var cpf = "52998224725";

        // Act
        var result = Utils.FormatarCPF(cpf);

        // Assert
        Assert.Equal("529.982.247-25", result);
    }

    [Fact]
    public void FormatarCPF_AlreadyFormatted_FormatsCorrectly()
    {
        // Arrange
        var cpf = "529.982.247-25";

        // Act
        var result = Utils.FormatarCPF(cpf);

        // Assert
        Assert.Equal("529.982.247-25", result);
    }

    [Fact]
    public void FormatarCPF_InvalidLength_ReturnsOriginal()
    {
        // Arrange
        var cpf = "123";

        // Act
        var result = Utils.FormatarCPF(cpf);

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void FormatarCNPJ_FormatsCorrectly()
    {
        // Arrange
        var cnpj = "11222333000181";

        // Act
        var result = Utils.FormatarCNPJ(cnpj);

        // Assert
        Assert.Equal("11.222.333/0001-81", result);
    }

    [Fact]
    public void FormatarCNPJ_InvalidLength_ReturnsOriginal()
    {
        // Arrange
        var cnpj = "123";

        // Act
        var result = Utils.FormatarCNPJ(cnpj);

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void ValidarCPF_ValidCPF_ReturnsTrue()
    {
        // 529.982.247-25 is a known valid test CPF
        Assert.True(Utils.ValidarCPF("529.982.247-25"));
        Assert.True(Utils.ValidarCPF("52998224725"));
    }

    [Fact]
    public void ValidarCPF_InvalidCPF_ReturnsFalse()
    {
        Assert.False(Utils.ValidarCPF("529.982.247-26")); // Wrong check digit
        Assert.False(Utils.ValidarCPF("12345678901"));
    }

    [Fact]
    public void ValidarCPF_AllSameDigits_ReturnsFalse()
    {
        Assert.False(Utils.ValidarCPF("111.111.111-11"));
        Assert.False(Utils.ValidarCPF("000.000.000-00"));
        Assert.False(Utils.ValidarCPF("999.999.999-99"));
    }

    [Fact]
    public void ValidarCPF_WrongLength_ReturnsFalse()
    {
        Assert.False(Utils.ValidarCPF("123"));
        Assert.False(Utils.ValidarCPF(""));
    }

    [Fact]
    public void ValidarCNPJ_ValidCNPJ_ReturnsTrue()
    {
        // 11.222.333/0001-81 is a known valid test CNPJ
        Assert.True(Utils.ValidarCNPJ("11.222.333/0001-81"));
        Assert.True(Utils.ValidarCNPJ("11222333000181"));
    }

    [Fact]
    public void ValidarCNPJ_InvalidCNPJ_ReturnsFalse()
    {
        Assert.False(Utils.ValidarCNPJ("11.222.333/0001-82")); // Wrong check digit
        Assert.False(Utils.ValidarCNPJ("12345678000199"));
    }

    [Fact]
    public void ValidarCNPJ_AllSameDigits_ReturnsFalse()
    {
        Assert.False(Utils.ValidarCNPJ("11.111.111/1111-11"));
        Assert.False(Utils.ValidarCNPJ("00.000.000/0000-00"));
    }

    [Theory]
    [InlineData(1000, 10, 100)]
    [InlineData(500, 50, 250)]
    [InlineData(200, 25, 50)]
    public void ObterPercentualPara_CalculatesCorrectly(decimal total, decimal percentual, decimal expected)
    {
        // Act
        var result = Utils.ObterPercentualPara(total, percentual);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Dividir_ByZero_ReturnsZero()
    {
        // Act
        var result = Utils.Dividir(100m, 0m);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Dividir_NormalDivision_ReturnsCorrectResult()
    {
        // Act
        var result = Utils.Dividir(100m, 4m);

        // Assert
        Assert.Equal(25m, result);
    }

    [Fact]
    public void Somar_MultipleValues_ReturnsSum()
    {
        // Act
        var result = Utils.Somar(10m, 20m, 30m, 40m);

        // Assert
        Assert.Equal(100m, result);
    }

    [Fact]
    public void Subtrair_ReturnsCorrectDifference()
    {
        // Act
        var result = Utils.Subtrair(100m, 30m);

        // Assert
        Assert.Equal(70m, result);
    }

    [Fact]
    public void Multiplicar_ReturnsCorrectProduct()
    {
        // Act
        var result = Utils.Multiplicar(12m, 5m);

        // Assert
        Assert.Equal(60m, result);
    }

    [Fact]
    public void FormatarCompetencia_FormatsCorrectly()
    {
        // Act
        var result = Utils.FormatarCompetencia(3, 2024);

        // Assert
        Assert.Equal("03/2024", result);
    }

    [Fact]
    public void FormatarCompetencia_FromDateTime_FormatsCorrectly()
    {
        // Arrange
        var data = new DateTime(2024, 11, 15);

        // Act
        var result = Utils.FormatarCompetencia(data);

        // Assert
        Assert.Equal("11/2024", result);
    }

    [Fact]
    public void FormatarValorPercentual_FormatsCorrectly()
    {
        // Act
        var result = Utils.FormatarValorPercentual(12.5m);

        // Assert
        Assert.Contains("12,50%", result);
    }

    [Fact]
    public void ZerarSeNulo_NullValue_ReturnsZero()
    {
        // Act
        var result = Utils.ZerarSeNulo(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ZerarSeNulo_HasValue_ReturnsValue()
    {
        // Act
        var result = Utils.ZerarSeNulo(42.5m);

        // Assert
        Assert.Equal(42.5m, result);
    }
}
