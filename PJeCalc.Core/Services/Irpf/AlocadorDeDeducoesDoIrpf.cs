namespace PJeCalc.Core.Services.Irpf;

/// <summary>Deduções da base do IRPF distribuídas pelos baldes correntes (13º / férias / demais).</summary>
public sealed record DeducoesDoIrpfPorBucket
{
    public decimal DecimoTerceiro { get; init; }
    public decimal Ferias { get; init; }
    public decimal DemaisVerbas { get; init; }
}

/// <summary>Agregados das deduções a distribuir (já apurados nos seus módulos).</summary>
public sealed record ContextoDeAlocacaoDeDeducoes
{
    public bool IncidirSobreJuros { get; init; }
    public decimal JurosDecimoTerceiro { get; init; }
    public decimal JurosFerias { get; init; }
    public decimal JurosDemaisVerbas { get; init; }

    /// <summary>Total do devido corrigido da previdência privada (rateado por verba).</summary>
    public decimal PrevidenciaPrivadaTotal { get; init; }

    /// <summary>Pensão devida só sobre as verbas tributáveis (rateada por verba + juros).</summary>
    public decimal PensaoTributavel { get; init; }

    /// <summary>Honorários devidos pelo reclamante (rateados pelo fator devidos ÷ bruto).</summary>
    public decimal HonorariosDevidosPeloReclamante { get; init; }

    public decimal BrutoDevidoAoReclamante { get; init; }
}

/// <summary>
/// Distribui as deduções da base do IRPF (previdência privada, pensão e honorários) pelos baldes
/// no regime de caixa, espelhando <c>MaquinaDeCalculoDeIrpf.liquidar</c>. É o que o
/// <see cref="GeradorDeOcorrenciasDeIrpf"/> antes recebia pronto por balde: aqui os agregados de
/// cada módulo são rateados e somados, prontos para entrar em
/// <see cref="ContextoDeGeracaoDeIrpf.DeducoesDecimoTerceiro"/> e afins (o INSS, rateado por
/// competência, continua entrando à parte).
///
/// <para>Previdência privada: rateada pela verba de cada balde (denominador = 13º + demais +
/// férias gozadas). Pensão: rateada por verba + juros de cada balde. Honorários: fator = honorários
/// devidos ÷ bruto, aplicado a verba + juros de cada balde. Os juros só entram quando o IR incide
/// sobre eles.</para>
/// </summary>
public static class AlocadorDeDeducoesDoIrpf
{
    public static DeducoesDoIrpfPorBucket AlocarRegimeCaixa(
        IReadOnlyList<VerbaParaIrpf> verbas, ContextoDeAlocacaoDeDeducoes ctx)
    {
        ArgumentNullException.ThrowIfNull(verbas);
        ArgumentNullException.ThrowIfNull(ctx);

        decimal verba13 = 0m, verbaFerias = 0m, verbaDemais = 0m;
        foreach (var verba in verbas)
        {
            switch (verba.Caracteristica)
            {
                case CaracteristicaParaIrpf.DecimoTerceiroSalario:
                    verba13 += Arredondar(verba.DiferencaCorrigida);
                    break;
                case CaracteristicaParaIrpf.Ferias:
                    if (verba.BaseParaIncidencias is { } gozada)
                        verbaFerias += gozada;
                    break;
                case CaracteristicaParaIrpf.Demais:
                    verbaDemais += Arredondar(verba.DiferencaCorrigida);
                    break;
            }
        }

        var juros13 = ctx.IncidirSobreJuros ? ctx.JurosDecimoTerceiro : 0m;
        var jurosFerias = ctx.IncidirSobreJuros ? ctx.JurosFerias : 0m;
        var jurosDemais = ctx.IncidirSobreJuros ? ctx.JurosDemaisVerbas : 0m;

        // Previdência privada: rateada só pela verba (denominador com férias gozadas).
        decimal prev13 = 0m, prevFerias = 0m, prevDemais = 0m;
        var prevDenom = verba13 + verbaDemais + verbaFerias;
        if (ctx.PrevidenciaPrivadaTotal != 0m && prevDenom != 0m)
        {
            prev13 = ctx.PrevidenciaPrivadaTotal * verba13 / prevDenom;
            prevFerias = ctx.PrevidenciaPrivadaTotal * verbaFerias / prevDenom;
            prevDemais = ctx.PrevidenciaPrivadaTotal * verbaDemais / prevDenom;
        }

        // Pensão: rateada por verba + juros.
        var valor13 = verba13 + juros13;
        var valorFerias = verbaFerias + jurosFerias;
        var valorDemais = verbaDemais + jurosDemais;
        decimal pensao13 = 0m, pensaoFerias = 0m, pensaoDemais = 0m;
        var pensaoDenom = valor13 + valorDemais + valorFerias;
        if (ctx.PensaoTributavel != 0m && pensaoDenom != 0m)
        {
            pensao13 = ctx.PensaoTributavel * valor13 / pensaoDenom;
            pensaoFerias = ctx.PensaoTributavel * valorFerias / pensaoDenom;
            pensaoDemais = ctx.PensaoTributavel * valorDemais / pensaoDenom;
        }

        // Honorários: fator devidos ÷ bruto, sobre verba + juros.
        decimal hon13 = 0m, honFerias = 0m, honDemais = 0m;
        if (ctx.HonorariosDevidosPeloReclamante != 0m && ctx.BrutoDevidoAoReclamante != 0m)
        {
            var fator = ctx.HonorariosDevidosPeloReclamante / ctx.BrutoDevidoAoReclamante;
            hon13 = valor13 * fator;
            honFerias = valorFerias * fator;
            honDemais = valorDemais * fator;
        }

        return new DeducoesDoIrpfPorBucket
        {
            DecimoTerceiro = prev13 + pensao13 + hon13,
            Ferias = prevFerias + pensaoFerias + honFerias,
            DemaisVerbas = prevDemais + pensaoDemais + honDemais,
        };
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
