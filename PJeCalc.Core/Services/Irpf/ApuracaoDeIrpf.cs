namespace PJeCalc.Core.Services.Irpf;

/// <summary>Dados de uma ocorrência de IRPF a apurar.</summary>
public sealed record OcorrenciaDeIrpfEntrada
{
    public decimal Verbas { get; init; }

    /// <summary>Juros de mora. Só compõem a base quando o IR incide sobre juros (padrão: não).</summary>
    public decimal Juros { get; init; }

    // Deduções da base.
    public decimal ContribuicaoSocial { get; init; }
    public decimal PrevidenciaPrivada { get; init; }
    public decimal PensaoAlimenticia { get; init; }
    public decimal Honorarios { get; init; }
    public decimal Dependentes { get; init; }
    public decimal AposentadoMaior65 { get; init; }

    /// <summary>
    /// Número de meses do RRA (rendimentos recebidos acumuladamente). 1 = regime normal;
    /// mais de 1 multiplica os limites das faixas e a parcela a deduzir.
    /// </summary>
    public int NumeroDeMeses { get; init; } = 1;
}

/// <summary>Resultado da apuração do IRPF de uma ocorrência.</summary>
public sealed record ResultadoDoIrpf
{
    public required decimal Base { get; init; }
    public required decimal Aliquota { get; init; }
    public required decimal Deducao { get; init; }
    public required decimal Imposto { get; init; }
}

/// <summary>
/// Apuração do IRPF de uma ocorrência: base tributável e imposto devido pela tabela
/// progressiva, com suporte ao regime de rendimentos acumulados (RRA — tabela × NM).
///
/// <para>Base = verbas + juros − deduções (piso zero). Imposto = base × alíquota − parcela
/// a deduzir (piso zero). No RRA os limites das faixas e a parcela a deduzir escalam pelo
/// número de meses; a alíquota não. Nenhuma das duas etapas é arredondada — o
/// arredondamento acontece nos agregados, como no motor oficial.</para>
/// </summary>
public static class ApuracaoDeIrpf
{
    public static ResultadoDoIrpf Calcular(TabelaIrpf tabela, OcorrenciaDeIrpfEntrada entrada)
    {
        ArgumentNullException.ThrowIfNull(tabela);
        ArgumentNullException.ThrowIfNull(entrada);

        var nm = entrada.NumeroDeMeses;
        var baseTributavel = CalcularBase(entrada);

        var faixa = tabela.ObterFaixaParaValor(baseTributavel, nm);
        var deducao = faixa.Deducao * nm;

        var imposto = baseTributavel * faixa.Aliquota / 100m - deducao;
        if (imposto < 0m)
            imposto = 0m;

        return new ResultadoDoIrpf
        {
            Base = baseTributavel,
            Aliquota = faixa.Aliquota,
            Deducao = deducao,
            Imposto = imposto,
        };
    }

    /// <summary>Base tributável: verbas + juros menos as deduções, nunca negativa.</summary>
    public static decimal CalcularBase(OcorrenciaDeIrpfEntrada e)
    {
        var baseTributavel = e.Verbas + e.Juros
            - e.ContribuicaoSocial - e.PrevidenciaPrivada - e.PensaoAlimenticia
            - e.Honorarios - e.Dependentes - e.AposentadoMaior65;

        return baseTributavel < 0m ? 0m : baseTributavel;
    }
}
