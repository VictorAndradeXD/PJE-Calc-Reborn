using PJeCalc.Core.Models.Inss;

namespace PJeCalc.Tests.Models;

public class InssTests
{
    [Fact]
    public void NewInss_HasEmptyAliquotas()
    {
        // Arrange & Act
        var inss = new Inss();

        // Assert
        Assert.Empty(inss.AliquotasPorPeriodos);
    }

    [Fact]
    public void CanSetProperties()
    {
        // Arrange
        var inss = new Inss();

        // Act
        inss.TipoAliquotaSegurado = "Progressiva";
        inss.AliquotaSeguradoFixa = 11m;
        inss.LimitarTeto = true;
        inss.TipoAliquotaEmpregador = "Fixa";
        inss.AliquotaEmpresaFixa = 20m;
        inss.AliquotaRATFixa = 3m;
        inss.AliquotaTerceirosFixa = 5.8m;
        inss.ApurarInssSobreSalariosPagos = true;

        // Assert
        Assert.Equal("Progressiva", inss.TipoAliquotaSegurado);
        Assert.Equal(11m, inss.AliquotaSeguradoFixa);
        Assert.True(inss.LimitarTeto);
        Assert.Equal("Fixa", inss.TipoAliquotaEmpregador);
        Assert.Equal(20m, inss.AliquotaEmpresaFixa);
        Assert.Equal(3m, inss.AliquotaRATFixa);
        Assert.Equal(5.8m, inss.AliquotaTerceirosFixa);
        Assert.True(inss.ApurarInssSobreSalariosPagos);
    }

    [Fact]
    public void NewInss_NullableFieldsAreNull()
    {
        // Arrange & Act
        var inss = new Inss();

        // Assert
        Assert.Null(inss.TipoAliquotaSegurado);
        Assert.Null(inss.AliquotaSeguradoFixa);
        Assert.Null(inss.TipoAliquotaEmpregador);
        Assert.Null(inss.AliquotaEmpresaFixa);
        Assert.Null(inss.AliquotaRATFixa);
        Assert.Null(inss.AliquotaTerceirosFixa);
        Assert.Null(inss.Calculo);
    }

    [Fact]
    public void NewInss_DefaultBooleans_AreFalse()
    {
        // Arrange & Act
        var inss = new Inss();

        // Assert
        Assert.False(inss.LimitarTeto);
        Assert.False(inss.ApurarInssSobreSalariosPagos);
    }

    [Fact]
    public void NewInss_Id_DefaultsToZero()
    {
        // Arrange & Act
        var inss = new Inss();

        // Assert
        Assert.Equal(0, inss.Id);
    }
}
