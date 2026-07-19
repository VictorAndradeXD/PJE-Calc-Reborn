using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Testes estruturais da aplicação dos juros sobre o capital (base líquida) e da
/// composição período → taxa → juros.
/// </summary>
public class JurosServiceTests
{
    [Fact]
    public void Capital_e_o_valor_corrigido_menos_contribuicoes()
    {
        Assert.Equal(850m, JurosService.CalcularCapital(1000m, contribuicaoSocial: 120m, previdenciaPrivada: 30m));
        Assert.Equal(1000m, JurosService.CalcularCapital(1000m));
    }

    [Fact]
    public void Juros_incidem_sobre_o_capital_pela_taxa_acumulada()
    {
        // Capital 1000, taxa acumulada 12% => 120,00
        Assert.Equal(120.00m, JurosService.CalcularJuros(1000m, 12m));
    }

    [Fact]
    public void Juros_arredondam_a_duas_casas_com_half_even()
    {
        // 1000 * 0,125% = 1,25 (exato); 1000 * 0,1250001% -> 1,25 (2 casas)
        Assert.Equal(1.25m, JurosService.CalcularJuros(1000m, 0.125m));
    }

    [Fact]
    public void Periodo_de_um_ano_a_um_porcento_simples_da_doze_porcento()
    {
        var periodo = new PeriodoDeJuros
        {
            Inicio = new DateOnly(2020, 1, 1),
            Fim = new DateOnly(2020, 12, 31),
            Aliquota = 1m,
            Quantidade = TipoDeQuantidadeDeJurosBaseEnum.Fracao,
            Capitalizacao = TipoDeJurosEnum.Simples,
            Tabela = JurosEnum.JurosUmPorcento,
        };

        Assert.Equal(12m, periodo.Meses);
        Assert.Equal(12m, periodo.Taxa);

        // Capital líquido de 1000 rende 120 de juros nesse período.
        var capital = JurosService.CalcularCapital(1000m);
        Assert.Equal(120.00m, JurosService.CalcularJuros(capital, periodo.Taxa));
    }

    [Fact]
    public void Regime_diario_usa_aliquota_vezes_dias()
    {
        var periodo = new PeriodoDeJuros
        {
            Inicio = new DateOnly(2020, 1, 1),
            Fim = new DateOnly(2020, 1, 31),
            Aliquota = 0.0333333m,
            Tabela = JurosEnum.JurosZeroTrintaTres,
        };

        Assert.Equal(31, periodo.Dias);
        Assert.Equal(0.0333333m * 31, periodo.Taxa);
    }
}
