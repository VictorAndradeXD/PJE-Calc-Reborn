using PJeCalc.Core.Models.Fgts;
using PJeCalc.Core.Enums;

namespace PJeCalc.Tests.Models;

public class FgtsTests
{
    [Fact]
    public void NewFgts_HasEmptyCollections()
    {
        // Arrange & Act
        var fgts = new Fgts();

        // Assert
        Assert.Empty(fgts.Ocorrencias);
        Assert.Empty(fgts.Operacoes);
    }

    [Fact]
    public void CanSetAliquota()
    {
        // Arrange
        var fgts = new Fgts();

        // Act
        fgts.Aliquota = AliquotaDoFgtsEnum.OitoPorCento;

        // Assert
        Assert.Equal(AliquotaDoFgtsEnum.OitoPorCento, fgts.Aliquota);
    }

    [Fact]
    public void CanSetAliquota_DoisPorCento()
    {
        // Arrange
        var fgts = new Fgts();

        // Act
        fgts.Aliquota = AliquotaDoFgtsEnum.DoisPorCento;

        // Assert
        Assert.Equal(AliquotaDoFgtsEnum.DoisPorCento, fgts.Aliquota);
    }

    [Fact]
    public void CanSetDestino()
    {
        // Arrange
        var fgts = new Fgts();

        // Act
        fgts.Destino = DestinoDoFgtsEnum.Pagar;

        // Assert
        Assert.Equal(DestinoDoFgtsEnum.Pagar, fgts.Destino);
    }

    [Fact]
    public void NewFgts_NullableFieldsAreNull()
    {
        // Arrange & Act
        var fgts = new Fgts();

        // Assert
        Assert.Null(fgts.PeriodoInicial);
        Assert.Null(fgts.PeriodoFinal);
        Assert.Null(fgts.Destino);
        Assert.Null(fgts.Aliquota);
        Assert.Null(fgts.TipoDoValorDaMulta);
        Assert.Null(fgts.ValorInformadoDaMulta);
        Assert.Null(fgts.MultaDoFgts);
        Assert.Null(fgts.Calculo);
    }

    [Fact]
    public void CanSetMultaFlags()
    {
        // Arrange
        var fgts = new Fgts();

        // Act
        fgts.Multa = true;
        fgts.MultaDoArtigo467 = true;
        fgts.Multa10 = true;
        fgts.ContribuicaoSocial05 = true;
        fgts.ExcluirAvisoDaMulta = true;

        // Assert
        Assert.True(fgts.Multa);
        Assert.True(fgts.MultaDoArtigo467);
        Assert.True(fgts.Multa10);
        Assert.True(fgts.ContribuicaoSocial05);
        Assert.True(fgts.ExcluirAvisoDaMulta);
    }

    [Fact]
    public void CanSetPeriodo()
    {
        // Arrange
        var fgts = new Fgts();

        // Act
        fgts.PeriodoInicial = new DateTime(2020, 1, 1);
        fgts.PeriodoFinal = new DateTime(2024, 6, 30);

        // Assert
        Assert.Equal(new DateTime(2020, 1, 1), fgts.PeriodoInicial);
        Assert.Equal(new DateTime(2024, 6, 30), fgts.PeriodoFinal);
    }
}
