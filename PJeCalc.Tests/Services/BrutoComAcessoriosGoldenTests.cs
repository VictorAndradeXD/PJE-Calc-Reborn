using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida a composição do bruto devido ao reclamante com os acessórios (FGTS que compõe o
/// principal, salário-família, seguro-desemprego) no <see cref="MotorDeCalculo"/> contra o motor
/// oficial (harness <c>tools/golden/GoldenGenBrutoComAcessorios.java</c>, que replica
/// <c>calcularBrutoDevidoAoReclamante</c>).
/// </summary>
public sealed class BrutoComAcessoriosGoldenTests
{
    [Theory]
    [InlineData("SEM_DEDUCAO", 0)]
    [InlineData("COM_DEDUCAO_FGTS", 2500)]
    public void Bruto_com_acessorios_bate_com_o_motor(string cenario, decimal depositadoDeduzido)
    {
        // Verbas que produzem principal corrigido 5000 e juros 1000 (taxa fixa de 20%).
        var verba = new VerbaEmCalculo { Nome = "v", Tipo = TipoDaVerbaEnum.Informada };
        verba.Ocorrencias.Add(new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = new(2020, 1, 1),
            DataFinal = new(2020, 1, 31),
            Devido = 5000m,
            Pago = 0m,
            IndiceAcumulado = 1m,
            Ativo = true,
        });

        var config = new ConfiguracaoDoCalculo
        {
            Juros = new ContextoDeApuracaoDeJuros
            {
                DataAjuizamento = new(2019, 1, 1),
                TaxaAcumuladaAPartirDe = _ => 20m, // juros = 5000 × 20% = 1000
            },
            Multas =
            [
                new ParametrosDaMulta { CredorDevedor = CredorDevedorMultaEnum.ReclamanteReclamado, TipoValor = TipoValorEnum.Informado, ValorInformado = 400m },
                new ParametrosDaMulta { CredorDevedor = CredorDevedorMultaEnum.ReclamadoReclamante, TipoValor = TipoValorEnum.Informado, ValorInformado = 200m },
            ],
            PrincipalAdicional = new ComponentesDoPrincipal
            {
                FgtsCorrigidoNaLiquidacao = 3000m,
                MultaDoFgts = 1200m,
                MultaDoArtigo467 = 600m,
                DepositadoOuSacadoDeduzido = depositadoDeduzido,
                SalarioFamilia = 300m,
                SeguroDesemprego = 800m,
            },
        };

        var resultado = MotorDeCalculo.Calcular([verba], config);

        Assert.Equal(5000m, resultado.ApuracaoDeJuros.TotalDeValorCorrigido);
        Assert.Equal(1000m, resultado.ApuracaoDeJuros.TotalDeJuros);
        Assert.Equal(LerGolden()[cenario], resultado.BrutoDevidoAoReclamante);
    }

    private static Dictionary<string, decimal> LerGolden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_bruto_acessorios.csv");
        return File.ReadAllLines(caminho)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(';'))
            .ToDictionary(c => c[0], c => decimal.Parse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}
