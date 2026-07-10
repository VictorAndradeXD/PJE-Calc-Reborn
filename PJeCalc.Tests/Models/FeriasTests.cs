using PJeCalc.Core.Models.Ferias;
using PJeCalc.Core.Enums;

namespace PJeCalc.Tests.Models;

public class FeriasTests
{
    [Fact]
    public void DefaultPrazo_Is30()
    {
        // Arrange & Act
        var ferias = new Ferias();

        // Assert
        Assert.Equal(30, ferias.Prazo);
    }

    [Fact]
    public void CanSetSituacao()
    {
        // Arrange
        var ferias = new Ferias();

        // Act
        ferias.Situacao = SituacaoDaFeriasEnum.Gozadas;

        // Assert
        Assert.Equal(SituacaoDaFeriasEnum.Gozadas, ferias.Situacao);
    }

    [Fact]
    public void CanSetSituacao_AllValues()
    {
        // Arrange
        var ferias = new Ferias();

        // Act & Assert - each situation can be set
        ferias.Situacao = SituacaoDaFeriasEnum.NaoGozadas;
        Assert.Equal(SituacaoDaFeriasEnum.NaoGozadas, ferias.Situacao);

        ferias.Situacao = SituacaoDaFeriasEnum.Indenizadas;
        Assert.Equal(SituacaoDaFeriasEnum.Indenizadas, ferias.Situacao);

        ferias.Situacao = SituacaoDaFeriasEnum.Perdidas;
        Assert.Equal(SituacaoDaFeriasEnum.Perdidas, ferias.Situacao);
    }

    [Fact]
    public void CanSetPeriodos()
    {
        // Arrange
        var ferias = new Ferias();

        // Act
        ferias.DataInicialPeriodoAquisitivo = new DateTime(2023, 1, 1);
        ferias.DataFinalPeriodoAquisitivo = new DateTime(2023, 12, 31);
        ferias.DataInicialPeriodoConcessivo = new DateTime(2024, 1, 1);
        ferias.DataFinalPeriodoConcessivo = new DateTime(2024, 12, 31);

        // Assert
        Assert.Equal(new DateTime(2023, 1, 1), ferias.DataInicialPeriodoAquisitivo);
        Assert.Equal(new DateTime(2023, 12, 31), ferias.DataFinalPeriodoAquisitivo);
        Assert.Equal(new DateTime(2024, 1, 1), ferias.DataInicialPeriodoConcessivo);
        Assert.Equal(new DateTime(2024, 12, 31), ferias.DataFinalPeriodoConcessivo);
    }

    [Fact]
    public void NewFerias_NullableFieldsAreNull()
    {
        // Arrange & Act
        var ferias = new Ferias();

        // Assert
        Assert.Null(ferias.Relativa);
        Assert.Null(ferias.DataInicialPeriodoAquisitivo);
        Assert.Null(ferias.DataFinalPeriodoAquisitivo);
        Assert.Null(ferias.DataInicialPeriodoConcessivo);
        Assert.Null(ferias.DataFinalPeriodoConcessivo);
        Assert.Null(ferias.Situacao);
        Assert.Null(ferias.DiasDeAbono);
        Assert.Null(ferias.Calculo);
    }

    [Fact]
    public void CanSetDobraAndAbono()
    {
        // Arrange
        var ferias = new Ferias();

        // Act
        ferias.Dobra = true;
        ferias.Abono = true;
        ferias.DiasDeAbono = 10;

        // Assert
        Assert.True(ferias.Dobra);
        Assert.True(ferias.Abono);
        Assert.Equal(10, ferias.DiasDeAbono);
    }

    [Fact]
    public void DefaultBooleans_AreFalse()
    {
        // Arrange & Act
        var ferias = new Ferias();

        // Assert
        Assert.False(ferias.Dobra);
        Assert.False(ferias.Abono);
    }
}
