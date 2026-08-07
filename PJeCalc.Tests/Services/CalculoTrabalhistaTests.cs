using PJeCalc.Core.Enums;
using PJeCalc.Core.Services;
using PJeCalc.Core.Services.Custas;
using PJeCalc.Core.Services.Honorarios;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Core.Services.Multas;
using PJeCalc.Core.Services.Verbas;
using PJeCalc.Data.Context;
using PJeCalc.Data.Repositories;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida o caller end-to-end (<see cref="CalculoTrabalhista"/>): a orquestração que amarra os
/// resultados dos módulos acessórios ao cálculo principal (<see cref="MotorDeCalculo"/>) e ao
/// líquido (<see cref="LiquidoDevidoAoReclamante"/>). Cada número dos módulos já é validado contra o
/// motor oficial nos seus próprios testes golden; aqui confere-se a FIAÇÃO — cada acessório entra no
/// lugar certo (bruto, débitos do reclamado ou descontos do reclamante) e a soma final fecha.
/// O cenário base reusa o do <c>GoldenGenOrquestrador</c>.
/// </summary>
public sealed class CalculoTrabalhistaTests
{
    private static readonly DateOnly Ajuizamento = new(2019, 6, 1);
    private static readonly DateOnly Liquidacao = new(2021, 6, 1);

    private sealed class SemFaixas : IJurosFaixaProvider
    {
        public IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim) => [];
    }

    private static AcessoriosDoCalculo Acessorios() => new()
    {
        FgtsCorrigidoNaLiquidacao = 1000.00m,
        MultaDoFgts = 400.00m,
        MultaDoArtigo467 = 200.00m,
        DepositadoOuSacadoDeduzido = 100.00m,
        FgtsCompoePrincipal = true,
        DepositoFgts = 0m,
        SalarioFamilia = 50.00m,
        SalarioFamiliaCompoePrincipal = true,
        SeguroDesemprego = 80.00m,
        SeguroDesempregoCompoePrincipal = false,
        InssSeguradoReclamante = 300.00m,
        InssPatronalReclamado = 250.00m,
        ContribuicaoSocialFgtsReclamado = 30.00m,
        PrevidenciaPrivada = 40.00m,
        PensaoAlimenticia = 60.00m,
        IrpfDoReclamante = 150.00m,
        IrpfCobradoDoReclamado = 20.00m,
    };

    [Fact]
    public void Monta_a_configuracao_igual_a_montagem_manual_do_motor()
    {
        var acessorios = Acessorios();

        var completo = CalculoTrabalhista.Calcular(Verbas(), Configuracao(), acessorios);

        // A config derivada deve equivaler a montá-la à mão: acessórios que compõem entram no bruto;
        // INSS segurado/prev viram bases de desconto; INSS patronal + contrib FGTS + IRPF reclamado
        // viram outros débitos do reclamado.
        var configManual = Configuracao() with
        {
            PrincipalAdicional = new ComponentesDoPrincipal
            {
                FgtsCorrigidoNaLiquidacao = 1000.00m,
                MultaDoFgts = 400.00m,
                MultaDoArtigo467 = 200.00m,
                DepositadoOuSacadoDeduzido = 100.00m,
                SalarioFamilia = 50.00m,
                SeguroDesemprego = 0m, // não compõe
            },
            DescontoContribuicaoSocial = 300.00m,
            DescontoPrevidenciaPrivada = 40.00m,
            OutrosDebitosDoReclamado = 250.00m + 30.00m + 20.00m,
        };
        var esperado = MotorDeCalculo.Calcular(Verbas(), configManual);

        Assert.Equal(esperado.BrutoDevidoAoReclamante, completo.Principal.BrutoDevidoAoReclamante);
        Assert.Equal(esperado.Custas!.ConsolidadoReclamado, completo.Principal.Custas!.ConsolidadoReclamado);
        Assert.Equal(esperado.TotaisDeHonorarios.DevidoPeloReclamado, completo.Principal.TotaisDeHonorarios.DevidoPeloReclamado);
    }

    [Fact]
    public void Bruto_inclui_os_acessorios_que_compoem_o_principal()
    {
        var semAcessorios = MotorDeCalculo.Calcular(Verbas(), Configuracao());
        var completo = CalculoTrabalhista.Calcular(Verbas(), Configuracao(), Acessorios());

        // FGTS (1000 + 400 + 200 − 100) + salário-família (50); seguro-desemprego NÃO compõe.
        var acrescimo = 1000.00m + 400.00m + 200.00m - 100.00m + 50.00m;
        Assert.Equal(semAcessorios.BrutoDevidoAoReclamante + acrescimo, completo.Principal.BrutoDevidoAoReclamante);
    }

    [Fact]
    public void Descontos_do_reclamante_sao_fiados_dos_acessorios_e_o_liquido_fecha()
    {
        var completo = CalculoTrabalhista.Calcular(Verbas(), Configuracao(), Acessorios());
        var d = completo.Descontos;

        Assert.Equal(300.00m, d.ContribuicaoSocialSegurado);
        Assert.Equal(40.00m, d.PrevidenciaPrivada);
        Assert.Equal(60.00m, d.PensaoAlimenticia);
        Assert.Equal(150.00m, d.IrpfDoReclamante);
        Assert.Equal(completo.Principal.TotaisDeHonorarios.DevidoPeloReclamante, d.HonorariosReclamanteDescontar);

        // Crédito do FGTS: corrigido + multa − depositado, independentemente de compor o principal.
        Assert.Equal(1000.00m + 400.00m - 100.00m, completo.Credito.Fgts);

        Assert.Equal(completo.Credito.Bruto - completo.Descontos.Total, completo.Liquido.Liquido);
        Assert.Equal(completo.Credito.Bruto, completo.Liquido.CreditoBruto);
        Assert.Equal(completo.Descontos.Total, completo.Liquido.TotalDeDescontos);
    }

    [Fact]
    public void Fgts_que_nao_compoe_o_principal_fica_fora_do_bruto_mas_credita()
    {
        var acessorios = Acessorios() with { FgtsCompoePrincipal = false };
        var semFgtsNoBruto = MotorDeCalculo.Calcular(Verbas(), Configuracao() with
        {
            PrincipalAdicional = new ComponentesDoPrincipal { SalarioFamilia = 50.00m },
        });

        var completo = CalculoTrabalhista.Calcular(Verbas(), Configuracao(), acessorios);

        // Sem compor o principal, o FGTS não entra no bruto...
        Assert.Equal(semFgtsNoBruto.BrutoDevidoAoReclamante, completo.Principal.BrutoDevidoAoReclamante);
        // ...mas continua no crédito do reclamante.
        Assert.Equal(1000.00m + 400.00m - 100.00m, completo.Credito.Fgts);
    }

    [Fact]
    public void Deposito_do_fgts_entra_como_desconto_do_liquido()
    {
        var acessorios = Acessorios() with { DepositoFgts = 1300.00m };
        var completo = CalculoTrabalhista.Calcular(Verbas(), Configuracao(), acessorios);

        Assert.Equal(1300.00m, completo.Descontos.DepositoFgts);
        Assert.Equal(completo.Credito.Bruto - completo.Descontos.Total, completo.Liquido.Liquido);
    }

    // ---- Cenário base (mesmo do GoldenGenOrquestrador) ----

    private static ConfiguracaoDoCalculo Configuracao()
    {
        var tabela = new TabelaDeJurosService(new SemFaixas());
        using var contexto = ReferenciaDbContextFactory.Criar();
        var parametrosCustas = new EfParametroDeCustasProvider(contexto).ObterPorData(Liquidacao);

        return new ConfiguracaoDoCalculo
        {
            Juros = new ContextoDeApuracaoDeJuros
            {
                DataAjuizamento = Ajuizamento,
                TaxaAcumuladaAPartirDe = dia =>
                    tabela.CalcularTaxaAcumulada(JurosEnum.JurosUmPorcento, dia, Liquidacao),
            },
            Multas =
            [
                new ParametrosDaMulta
                {
                    CredorDevedor = CredorDevedorMultaEnum.ReclamanteReclamado,
                    Base = BaseParaApuracaoDeMultaEnum.Principal,
                    Aliquota = 10m,
                },
                new ParametrosDaMulta
                {
                    CredorDevedor = CredorDevedorMultaEnum.ReclamadoReclamante,
                    TipoValor = TipoValorEnum.Informado,
                    ValorInformado = 500.00m,
                },
            ],
            Honorarios =
            [
                new ParametrosDoHonorario
                {
                    Devedor = TipoDeDevedorDoHonorarioEnum.Reclamado,
                    Base = BaseParaApuracaoDeHonorarioEnum.Bruto,
                    Aliquota = 10m,
                },
            ],
            Custas = new ParametrosDeCustas
            {
                Parametros = parametrosCustas,
                BaseCalculada = BaseParaCustasCalculadasEnum.BrutoDevidoAoReclamante,
                ConhecimentoReclamado = TipoDeCustasDeConhecimentoEnum.Calculada2PorCento,
                TetoConhecimento = 4m * 6433.57m,
            },
        };
    }

    private static List<VerbaEmCalculo> Verbas()
    {
        var verbas = new List<VerbaEmCalculo>();

        var a = Nova("A", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(a, new(2019, 3, 1), new(2019, 3, 31), 1000.00m, 0m, 1.2m);
        Add(a, new(2019, 8, 1), new(2019, 8, 31), 500.00m, 0m, 1.1m);
        verbas.Add(a);

        var b = Nova("B", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(b, new(2019, 3, 1), new(2019, 3, 31), 200.00m, 0m, 1.0m);
        verbas.Add(b);

        var c = Nova("C", CaracteristicaDaVerbaEnum.Ferias, JurosDoAjuizamentoEnum.OcorrenciasVencidas);
        Add(c, new(2020, 1, 1), new(2020, 1, 31), 3000.00m, 500.00m, 1.05m);
        verbas.Add(c);

        var d = Nova("D", CaracteristicaDaVerbaEnum.Comum, JurosDoAjuizamentoEnum.OcorrenciasVencidasEVincendas);
        Add(d, new(2020, 5, 1), new(2020, 5, 31), 800.00m, 0m, 1.0m);
        verbas.Add(d);

        return verbas;
    }

    private static VerbaEmCalculo Nova(
        string nome, CaracteristicaDaVerbaEnum caracteristica, JurosDoAjuizamentoEnum juros) =>
        new() { Nome = nome, Tipo = TipoDaVerbaEnum.Informada, Caracteristica = caracteristica, JurosDoAjuizamento = juros };

    private static void Add(
        VerbaEmCalculo verba, DateOnly inicio, DateOnly fim, decimal devido, decimal pago, decimal indice) =>
        verba.Ocorrencias.Add(new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = inicio,
            DataFinal = fim,
            Devido = devido,
            Pago = pago,
            IndiceAcumulado = indice,
            Ativo = true,
        });
}
