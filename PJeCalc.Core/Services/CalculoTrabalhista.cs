using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Core.Services;

/// <summary>
/// Resultados já apurados dos módulos acessórios, que o caller amarra ao cálculo principal e ao
/// líquido. Cada valor vem do seu módulo (todos validados contra o motor oficial); aqui apenas se
/// decide onde cada um entra: no bruto (quando compõe o principal), na base das custas do reclamado
/// (outros débitos), ou nos descontos do reclamante.
/// </summary>
public sealed record AcessoriosDoCalculo
{
    // ---- FGTS ----
    /// <summary>FGTS devido corrigido à liquidação.</summary>
    public decimal FgtsCorrigidoNaLiquidacao { get; init; }
    public decimal MultaDoFgts { get; init; }
    public decimal MultaDoArtigo467 { get; init; }

    /// <summary>Depósitos/saques a deduzir do FGTS (quando "deduzir do FGTS").</summary>
    public decimal DepositadoOuSacadoDeduzido { get; init; }

    /// <summary>Se o FGTS compõe o principal (entra no bruto devido ao reclamante).</summary>
    public bool FgtsCompoePrincipal { get; init; }

    /// <summary>Depósito do FGTS na conta vinculada (desconto do líquido quando o destino é depositar).</summary>
    public decimal DepositoFgts { get; init; }

    // ---- Salário-família / Seguro-desemprego ----
    public decimal SalarioFamilia { get; init; }
    public bool SalarioFamiliaCompoePrincipal { get; init; }
    public decimal SeguroDesemprego { get; init; }
    public bool SeguroDesempregoCompoePrincipal { get; init; }

    // ---- INSS ----
    /// <summary>Cota do segurado reclamante (desconto do reclamante e base − das multas/honorários).</summary>
    public decimal InssSeguradoReclamante { get; init; }

    /// <summary>Cota patronal do reclamado (outros débitos do reclamado — base BROR das custas).</summary>
    public decimal InssPatronalReclamado { get; init; }

    /// <summary>Contribuição social do FGTS devida pelo reclamado (outros débitos do reclamado).</summary>
    public decimal ContribuicaoSocialFgtsReclamado { get; init; }

    // ---- Previdência privada / pensão / IRPF ----
    /// <summary>Total do devido corrigido da previdência privada (desconto e base − das multas/honorários).</summary>
    public decimal PrevidenciaPrivada { get; init; }

    /// <summary>Pensão alimentícia devida (desconto do reclamante).</summary>
    public decimal PensaoAlimenticia { get; init; }

    /// <summary>IRPF devido pelo reclamante (soma das ocorrências geradas; desconto do reclamante).</summary>
    public decimal IrpfDoReclamante { get; init; }

    /// <summary>IRPF cobrado do reclamado (outros débitos do reclamado).</summary>
    public decimal IrpfCobradoDoReclamado { get; init; }
}

/// <summary>
/// Descontos do reclamante que dependem de flags por parcela (não deriváveis dos totalizadores):
/// multas contra o reclamante que descontam do crédito e custas do reclamante.
/// </summary>
public sealed record DescontosDiretosDoReclamante
{
    /// <summary>Multas reclamado→reclamante marcadas para descontar do crédito.</summary>
    public decimal MultasReclamadoReclamanteDescontar { get; init; }

    /// <summary>Multas terceiro→reclamante que descontam do crédito.</summary>
    public decimal MultasTerceiroReclamanteDescontar { get; init; }

    public decimal CustasDoReclamante { get; init; }
}

/// <summary>Resultado consolidado do cálculo completo: o principal e o líquido devido ao reclamante.</summary>
public sealed record ResultadoDoCalculoCompleto
{
    public required ResultadoDoCalculo Principal { get; init; }
    public required CreditoDoReclamante Credito { get; init; }
    public required DescontosDoReclamante Descontos { get; init; }
    public required ResultadoDoLiquido Liquido { get; init; }
}

/// <summary>
/// Caller end-to-end: roda o cálculo principal (<see cref="MotorDeCalculo"/>) e o líquido devido ao
/// reclamante (<see cref="LiquidoDevidoAoReclamante"/>) a partir das verbas liquidadas, dos
/// parâmetros dos módulos e dos resultados já apurados dos acessórios.
///
/// <para>É a camada de integração que antes o chamador tinha de montar à mão: distribui cada
/// acessório no seu lugar — os que compõem o principal entram no bruto; os débitos do reclamado
/// (INSS patronal, contribuição social do FGTS, IRPF cobrado do reclamado) entram na base das custas;
/// os descontos do reclamante (INSS do segurado, previdência privada, pensão, IRPF, honorários,
/// custas, depósito do FGTS) formam o líquido. Cada número vem de um módulo já validado contra o
/// motor oficial; aqui só se faz a orquestração e a soma final.</para>
/// </summary>
public static class CalculoTrabalhista
{
    public static ResultadoDoCalculoCompleto Calcular(
        IEnumerable<VerbaEmCalculo> verbas,
        ConfiguracaoDoCalculo configBase,
        AcessoriosDoCalculo acessorios,
        DescontosDiretosDoReclamante? descontosDiretos = null)
    {
        ArgumentNullException.ThrowIfNull(verbas);
        ArgumentNullException.ThrowIfNull(configBase);
        ArgumentNullException.ThrowIfNull(acessorios);
        descontosDiretos ??= new DescontosDiretosDoReclamante();

        // 1) Acessórios que compõem o principal entram no bruto (cada um só se marcado).
        var componentes = new ComponentesDoPrincipal
        {
            FgtsCorrigidoNaLiquidacao = acessorios.FgtsCompoePrincipal ? acessorios.FgtsCorrigidoNaLiquidacao : 0m,
            MultaDoFgts = acessorios.FgtsCompoePrincipal ? acessorios.MultaDoFgts : 0m,
            MultaDoArtigo467 = acessorios.FgtsCompoePrincipal ? acessorios.MultaDoArtigo467 : 0m,
            DepositadoOuSacadoDeduzido = acessorios.FgtsCompoePrincipal ? acessorios.DepositadoOuSacadoDeduzido : 0m,
            SalarioFamilia = acessorios.SalarioFamiliaCompoePrincipal ? acessorios.SalarioFamilia : 0m,
            SeguroDesemprego = acessorios.SeguroDesempregoCompoePrincipal ? acessorios.SeguroDesemprego : 0m,
        };

        // 2) Configuração derivada: bases de desconto e débitos do reclamado vêm dos acessórios.
        var config = configBase with
        {
            PrincipalAdicional = componentes,
            DescontoContribuicaoSocial = acessorios.InssSeguradoReclamante,
            DescontoPrevidenciaPrivada = acessorios.PrevidenciaPrivada,
            OutrosDebitosDoReclamado = configBase.OutrosDebitosDoReclamado
                + acessorios.InssPatronalReclamado
                + acessorios.ContribuicaoSocialFgtsReclamado
                + acessorios.IrpfCobradoDoReclamado,
        };

        var principal = MotorDeCalculo.Calcular(verbas, config);

        // 3) Crédito do reclamante: verbas (principal + juros + acessórios que compõem) e FGTS.
        var credito = new CreditoDoReclamante
        {
            Verbas = LiquidoDevidoAoReclamante.CreditoDeVerbas(
                principal.ApuracaoDeJuros.TotalDeValorCorrigido,
                principal.ApuracaoDeJuros.TotalDeJuros,
                componentes.SalarioFamilia,
                componentes.SeguroDesemprego,
                acessorios.FgtsCompoePrincipal ? acessorios.MultaDoArtigo467 : 0m),
            Fgts = LiquidoDevidoAoReclamante.CreditoDeFgts(
                acessorios.FgtsCorrigidoNaLiquidacao,
                acessorios.MultaDoFgts,
                acessorios.DepositadoOuSacadoDeduzido),
            MultasReclamanteReclamado = principal.TotaisDeMultas.ReclamanteReclamado,
            MultasReclamadoReclamanteDescontar = descontosDiretos.MultasReclamadoReclamanteDescontar,
        };

        // 4) Descontos do reclamante.
        var descontos = new DescontosDoReclamante
        {
            ContribuicaoSocialSegurado = acessorios.InssSeguradoReclamante,
            PrevidenciaPrivada = acessorios.PrevidenciaPrivada,
            PensaoAlimenticia = acessorios.PensaoAlimenticia,
            MultasTerceiroReclamanteDescontar = descontosDiretos.MultasTerceiroReclamanteDescontar,
            HonorariosReclamanteDescontar = principal.TotaisDeHonorarios.DevidoPeloReclamante,
            IrpfDoReclamante = acessorios.IrpfDoReclamante,
            CustasDoReclamante = descontosDiretos.CustasDoReclamante,
            DepositoFgts = acessorios.DepositoFgts,
        };

        var liquido = LiquidoDevidoAoReclamante.Calcular(credito, descontos);

        return new ResultadoDoCalculoCompleto
        {
            Principal = principal,
            Credito = credito,
            Descontos = descontos,
            Liquido = liquido,
        };
    }
}
