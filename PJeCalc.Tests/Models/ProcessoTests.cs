using PJeCalc.Core.Models.Processo;

namespace PJeCalc.Tests.Models;

public class ProcessoTests
{
    [Fact]
    public void IdentificadorDoProcesso_FormatarNumero_FormatsCorrectly()
    {
        // Arrange - "0001234-56.2023.5.08.0001"
        var identificador = new IdentificadorDoProcesso
        {
            Numero = 1234,
            Digito = 56,
            Ano = 2023,
            Justica = 5,
            Regiao = 8,
            Vara = 1
        };

        // Act
        var result = identificador.FormatarNumero();

        // Assert
        Assert.Equal("0001234-56.2023.5.08.0001", result);
    }

    [Fact]
    public void IdentificadorDoProcesso_FormatarNumero_PadsWithZeros()
    {
        // Arrange
        var identificador = new IdentificadorDoProcesso
        {
            Numero = 1,
            Digito = 2,
            Ano = 2024,
            Justica = 5,
            Regiao = 1,
            Vara = 3
        };

        // Act
        var result = identificador.FormatarNumero();

        // Assert
        Assert.Equal("0000001-02.2024.5.01.0003", result);
    }

    [Fact]
    public void IdentificadorDoProcesso_FormatarNumero_LargeNumbers()
    {
        // Arrange
        var identificador = new IdentificadorDoProcesso
        {
            Numero = 9999999,
            Digito = 99,
            Ano = 2024,
            Justica = 5,
            Regiao = 15,
            Vara = 9999
        };

        // Act
        var result = identificador.FormatarNumero();

        // Assert
        Assert.Equal("9999999-99.2024.5.15.9999", result);
    }

    [Fact]
    public void NewProcesso_HasEmptyAdvogadoLists()
    {
        // Arrange & Act
        var processo = new Processo();

        // Assert
        Assert.Empty(processo.AdvogadosReclamante);
        Assert.Empty(processo.AdvogadosReclamado);
    }

    [Fact]
    public void NewProcesso_HasDefaultIdentificador()
    {
        // Arrange & Act
        var processo = new Processo();

        // Assert
        Assert.NotNull(processo.Identificador);
        Assert.NotNull(processo.Reclamante);
        Assert.NotNull(processo.Reclamado);
    }

    [Fact]
    public void NewProcesso_NullableFieldsAreNull()
    {
        // Arrange & Act
        var processo = new Processo();

        // Assert
        Assert.Null(processo.ValorDaCausa);
        Assert.Null(processo.DataAutuacao);
        Assert.Null(processo.Calculo);
    }

    [Fact]
    public void Processo_CanSetValorDaCausa()
    {
        // Arrange
        var processo = new Processo();

        // Act
        processo.ValorDaCausa = 50000m;
        processo.DataAutuacao = new DateTime(2024, 1, 15);

        // Assert
        Assert.Equal(50000m, processo.ValorDaCausa);
        Assert.Equal(new DateTime(2024, 1, 15), processo.DataAutuacao);
    }
}
