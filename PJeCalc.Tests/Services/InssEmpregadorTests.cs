using PJeCalc.Core.Services.Inss;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Cotas patronais do INSS (empresa com teto, SAT e terceiros) e a resolução das alíquotas por
/// período. Fórmulas transcritas de <c>MaquinaDeCalculoDoInss.liquidarInssSobreSalariosDevidos</c>
/// (a máquina do original é ampla demais para golden headless; a base por competência é a geração,
/// ainda adiada, e entra como parâmetro).
/// </summary>
public sealed class InssEmpregadorTests
{
    private static readonly AliquotasDoEmpregador Padrao =
        new(new(2019, 1, 1), new(2021, 12, 31), Empresa: 20m, Rat: 3m, Terceiros: 5.8m);

    [Fact]
    public void Aliquotas_por_periodo_resolvem_a_competencia()
    {
        AliquotasDoEmpregador[] periodos =
        [
            new(new(2018, 1, 1), new(2018, 12, 31), 20m, 2m, 5.8m),
            new(new(2019, 1, 1), new(2021, 12, 31), 20m, 3m, 5.8m),
        ];

        Assert.Equal(2m, AliquotasDoEmpregador.Vigentes(periodos, new(2018, 6, 1))!.Rat);
        Assert.Equal(3m, AliquotasDoEmpregador.Vigentes(periodos, new(2020, 6, 1))!.Rat);
        Assert.Null(AliquotasDoEmpregador.Vigentes(periodos, new(2022, 1, 1)));
    }

    [Fact]
    public void Cotas_sem_teto_incidem_empresa_no_total_e_devido_nas_verbas()
    {
        var cotas = ApuracaoDoInssEmpregador.Calcular(baseTotal: 10000m, baseVerbas: 3000m, Padrao);

        Assert.Equal(2000m, cotas.EmpresaSobreBaseTotal); // 10000 × 20%
        Assert.Equal(600m, cotas.EmpresaDevida);          // 3000 × 20% (sem teto)
        Assert.Equal(90m, cotas.Sat);                     // 3000 × 3%
        Assert.Equal(174m, cotas.Terceiros);              // 3000 × 5,8%
    }

    [Fact]
    public void Teto_limita_a_empresa_e_a_parcela_devida()
    {
        // Teto acima do histórico: a empresa devida usa o que resta do teto.
        var parcial = ApuracaoDoInssEmpregador.Calcular(10000m, 3000m, Padrao, tetoEmpresa: 2200m);
        Assert.Equal(2000m, parcial.EmpresaSobreBaseTotal);      // min(2000, 2200)
        Assert.Equal(200m, parcial.EmpresaDevida);               // min(2200 − 2000, 600)

        // Teto já esgotado pelo histórico: nada devido na ação.
        var esgotado = ApuracaoDoInssEmpregador.Calcular(10000m, 3000m, Padrao, tetoEmpresa: 1500m);
        Assert.Equal(1500m, esgotado.EmpresaSobreBaseTotal);     // min(2000, 1500)
        Assert.Equal(0m, esgotado.EmpresaDevida);                // min(1500 − 1500, 600)
    }

    [Fact]
    public void Simples_zera_toda_a_contribuicao_patronal()
    {
        var cotas = ApuracaoDoInssEmpregador.Calcular(10000m, 3000m, Padrao, aplicarSimples: true);

        Assert.Equal(new CotasDoEmpregador(0m, 0m, 0m, 0m), cotas);
    }
}
