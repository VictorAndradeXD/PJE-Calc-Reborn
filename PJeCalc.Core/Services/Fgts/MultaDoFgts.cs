using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Fgts;

/// <summary>Parâmetros da multa do FGTS.</summary>
public sealed record ParametrosDaMultaDoFgts
{
    /// <summary>Se a multa é devida.</summary>
    public bool Aplicar { get; init; } = true;

    /// <summary>Percentual da multa (20% ou 40%).</summary>
    public ValorDaMultaDoFgtsEnum Percentual { get; init; } = ValorDaMultaDoFgtsEnum.QuarentaPorCento;

    /// <summary>Sobre o que a multa incide.</summary>
    public IncidenciaDeMultaDoFgtsEnum Incidencia { get; init; } = IncidenciaDeMultaDoFgtsEnum.SobreOTotalDevido;

    /// <summary>Se o aviso prévio sai da base da multa (padrão do original: sim).</summary>
    public bool ExcluirAvisoPrevio { get; init; } = true;

    /// <summary>Fator acumulado da multa (traz o valor da liquidação de volta à demissão).</summary>
    public required decimal IndiceMulta { get; init; }

    /// <summary>Total já depositado/sacado, corrigido até a liquidação.</summary>
    public decimal TotalDepositadoOuSacadoCorrigido { get; init; }

    /// <summary>Multa do art. 467 da CLT (50% da multa do FGTS).</summary>
    public bool MultaDoArtigo467 { get; init; }

    /// <summary>Fator de correção da multa do art. 467.</summary>
    public decimal IndiceMulta467 { get; init; }

    /// <summary>Multa de 10% da LC 110/2001.</summary>
    public bool Multa10 { get; init; }

    /// <summary>Se o valor da multa é calculado ou informado pelo usuário.</summary>
    public TipoDeBaseDoFgtsEnum TipoDoValor { get; init; } = TipoDeBaseDoFgtsEnum.Calculada;

    /// <summary>Valor da multa quando informado.</summary>
    public decimal? ValorInformado { get; init; }
}

/// <summary>Resultado do cálculo da multa do FGTS e das multas acessórias.</summary>
public sealed record ResultadoDaMultaDoFgts
{
    public required decimal Base { get; init; }
    public required decimal Valor { get; init; }
    public required decimal ValorCorrigido { get; init; }
    public required decimal ValorDoArtigo467 { get; init; }
    public required decimal ValorDaMulta10 { get; init; }
}

/// <summary>
/// Multa do FGTS (20%/40%) e acessórias (art. 467 da CLT e 10% da LC 110/2001).
///
/// <para>A base é apurada de forma indireta, como no original: toma-se o total corrigido
/// até a <b>liquidação</b> e divide-se pelo índice da multa, trazendo-o de volta à data
/// da demissão.</para>
/// </summary>
public static class MultaDoFgts
{
    public static ResultadoDaMultaDoFgts Calcular(TotaisDeFgts totais, ParametrosDaMultaDoFgts p)
    {
        ArgumentNullException.ThrowIfNull(totais);
        ArgumentNullException.ThrowIfNull(p);

        var baseDaMulta = CalcularBase(totais, p, p.Incidencia);
        var valor = CalcularValor(baseDaMulta, p);

        return new ResultadoDaMultaDoFgts
        {
            Base = baseDaMulta,
            Valor = valor,
            ValorCorrigido = AplicacaoDeFator.Aplicar(valor, p.IndiceMulta),
            ValorDoArtigo467 = p.MultaDoArtigo467 ? Arredondar(valor * 0.50m) : 0m,
            ValorDaMulta10 = p.Multa10
                ? Arredondar(totais.DevidoCorrigido(TipoDeCorrecaoDoFgtsEnum.PelaDataDeDemissao) * 0.10m)
                : 0m,
        };
    }

    private static decimal CalcularValor(decimal baseDaMulta, ParametrosDaMultaDoFgts p)
    {
        if (!p.Aplicar)
            return 0m;

        if (p.TipoDoValor == TipoDeBaseDoFgtsEnum.Informada)
            return p.ValorInformado ?? 0m;

        var fator = p.Percentual == ValorDaMultaDoFgtsEnum.VintePorCento ? 0.20m : 0.40m;
        return Arredondar(baseDaMulta * fator);
    }

    private static decimal CalcularBase(
        TotaisDeFgts totais, ParametrosDaMultaDoFgts p, IncidenciaDeMultaDoFgtsEnum incidencia)
    {
        if (!p.Aplicar || p.TipoDoValor == TipoDeBaseDoFgtsEnum.Informada)
            return 0m;

        const TipoDeCorrecaoDoFgtsEnum liq = TipoDeCorrecaoDoFgtsEnum.PelaDataDeLiquidacao;

        return incidencia switch
        {
            IncidenciaDeMultaDoFgtsEnum.SobreOTotalDevido =>
                TrazerParaDemissao(p.ExcluirAvisoPrevio
                    ? totais.DevidoSemAvisoCorrigido(liq)
                    : totais.DevidoCorrigido(liq), p),

            IncidenciaDeMultaDoFgtsEnum.SobreDepositadoSacado =>
                TrazerParaDemissao(p.TotalDepositadoOuSacadoCorrigido, p),

            IncidenciaDeMultaDoFgtsEnum.SobreDiferenca =>
                TrazerParaDemissao(p.ExcluirAvisoPrevio
                    ? totais.DiferencaSemAvisoCorrigida(liq)
                    : totais.DiferencaCorrigida(liq), p),

            IncidenciaDeMultaDoFgtsEnum.SobreTotalDevidoMaisSaqueOuSaldo =>
                CalcularBase(totais, p, IncidenciaDeMultaDoFgtsEnum.SobreOTotalDevido)
                + CalcularBase(totais, p, IncidenciaDeMultaDoFgtsEnum.SobreDepositadoSacado),

            IncidenciaDeMultaDoFgtsEnum.SobreTotalDevidoMenosSaqueOuSaldo =>
                CalcularBase(totais, p, IncidenciaDeMultaDoFgtsEnum.SobreOTotalDevido)
                - CalcularBase(totais, p, IncidenciaDeMultaDoFgtsEnum.SobreDepositadoSacado),

            _ => 0m,
        };
    }

    /// <summary>Divide o valor corrigido até a liquidação pelo índice da multa.</summary>
    private static decimal TrazerParaDemissao(decimal valorCorrigido, ParametrosDaMultaDoFgts p) =>
        p.IndiceMulta == 0m ? 0m : Arredondar(valorCorrigido / p.IndiceMulta);

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
