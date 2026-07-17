using PJeCalc.Core.Models.Calculo;
using PJeCalc.Core.Enums;

namespace PJeCalc.Tests.Models;

public class CalculoTests
{
    [Fact]
    public void NewCalculo_HasEmptyCollections()
    {
        // Arrange & Act
        var calculo = new Calculo();

        // Assert
        Assert.Empty(calculo.Verbas);
        Assert.Empty(calculo.HistoricosSalariais);
        Assert.Empty(calculo.Ferias);
        Assert.Empty(calculo.Faltas);
        Assert.Empty(calculo.ApuracoesDeJuros);
        Assert.Empty(calculo.Multas);
        Assert.Empty(calculo.Honorarios);
        Assert.Empty(calculo.Pagamentos);
        Assert.Empty(calculo.CartoesDePonto);
        Assert.Empty(calculo.ExcecoesCargaHoraria);
        Assert.Empty(calculo.ExcecoesSabado);
    }

    [Fact]
    public void NewCalculo_AllNullableFieldsAreNull()
    {
        // Arrange & Act
        var calculo = new Calculo();

        // Assert - Nullable DateTime fields
        Assert.Null(calculo.DataAdmissao);
        Assert.Null(calculo.DataDemissao);
        Assert.Null(calculo.DataDeLiquidacao);
        Assert.Null(calculo.DataAjuizamento);
        Assert.Null(calculo.DataInicioCalculo);
        Assert.Null(calculo.DataTerminoCalculo);
        Assert.Null(calculo.DataCriacao);
        Assert.Null(calculo.InicioFeriasColetivas);

        // Assert - Nullable decimal fields
        Assert.Null(calculo.Salario);
        Assert.Null(calculo.MaiorRemuneracao);
        Assert.Null(calculo.CargaHorariaMensal);
        Assert.Null(calculo.DivisorDeHorasExtras);

        // Assert - Nullable navigation properties
        Assert.Null(calculo.Processo);
        Assert.Null(calculo.Fgts);
        Assert.Null(calculo.Inss);
        Assert.Null(calculo.Irpf);
        Assert.Null(calculo.CustasJudiciais);
        Assert.Null(calculo.ParametrosDeAtualizacao);

        // Assert - Nullable version
        Assert.Null(calculo.Versao);
    }

    [Fact]
    public void CanSetBasicProperties()
    {
        // Arrange
        var calculo = new Calculo();
        var admissao = new DateTime(2020, 3, 1);
        var demissao = new DateTime(2024, 6, 30);

        // Act
        calculo.DataAdmissao = admissao;
        calculo.DataDemissao = demissao;
        calculo.Salario = 5000m;
        calculo.CargaHorariaMensal = 220m;
        calculo.TipoDeCalculo = TipoCalculoEnum.Credor;
        calculo.RegimeDoContrato = RegimeDoContratoEnum.Integral;
        calculo.IncluirSabado = true;
        calculo.ZeraValorNegativo = true;

        // Assert
        Assert.Equal(admissao, calculo.DataAdmissao);
        Assert.Equal(demissao, calculo.DataDemissao);
        Assert.Equal(5000m, calculo.Salario);
        Assert.Equal(220m, calculo.CargaHorariaMensal);
        Assert.Equal(TipoCalculoEnum.Credor, calculo.TipoDeCalculo);
        Assert.Equal(RegimeDoContratoEnum.Integral, calculo.RegimeDoContrato);
        Assert.True(calculo.IncluirSabado);
        Assert.True(calculo.ZeraValorNegativo);
    }

    [Fact]
    public void NewCalculo_DefaultBooleans_AreCorrect()
    {
        // Arrange & Act
        var calculo = new Calculo();

        // Assert - Ativo defaults to true
        Assert.True(calculo.Ativo);

        // Assert - Other booleans default to false
        Assert.False(calculo.IncluirSabado);
        Assert.False(calculo.ConsiderarPagamento);
        Assert.False(calculo.PrescricaoQuinquenal);
        Assert.False(calculo.ProjetaAvisoIndenizado);
        Assert.False(calculo.Validado);
    }

    [Fact]
    public void NewCalculo_Id_DefaultsToZero()
    {
        // Arrange & Act
        var calculo = new Calculo();

        // Assert
        Assert.Equal(0, calculo.Id);
    }
}
