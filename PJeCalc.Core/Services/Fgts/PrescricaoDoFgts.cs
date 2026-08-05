namespace PJeCalc.Core.Services.Fgts;

/// <summary>
/// Prescrição do FGTS e janela de geração das ocorrências mensais.
///
/// <para>A prescrição segue o STF (ARE 709212, j. 13/11/2014): a regra histórica é de
/// <b>30 anos</b>; passou a <b>5 anos</b> para ações ajuizadas a partir de 13/11/2019 e, na
/// transição (ajuizamento entre 13/11/2014 e 13/11/2019), aplica-se o menor prazo (5 anos)
/// quando a admissão é posterior a 13/11/1989 — do contrário permanece em 30 anos.</para>
/// </summary>
public static class PrescricaoDoFgts
{
    private static readonly DateOnly TrezeNovembro1989 = new(1989, 11, 13);
    private static readonly DateOnly TrezeNovembro2014 = new(2014, 11, 13);
    private static readonly DateOnly TrezeNovembro2019 = new(2019, 11, 13);

    /// <summary>Marco prescricional do FGTS: ajuizamento menos 30 (ou 5) anos.</summary>
    public static DateOnly CalcularData(DateOnly ajuizamento, DateOnly admissao)
    {
        var anos = -30;
        if (ajuizamento >= TrezeNovembro2014 && ajuizamento < TrezeNovembro2019 && admissao > TrezeNovembro1989)
            anos = -5;
        else if (ajuizamento >= TrezeNovembro2019)
            anos = -5;

        return ajuizamento.AddYears(anos);
    }

    /// <summary>
    /// Janela sugerida da geração: início = admissão (ou a prescrição, se ligada e posterior);
    /// fim = demissão, ou o término do cálculo quando não há demissão.
    /// </summary>
    public static (DateOnly Inicial, DateOnly Final) JanelaDeGeracao(
        DateOnly admissao,
        DateOnly? demissao,
        DateOnly terminoCalculo,
        DateOnly ajuizamento,
        bool aplicarPrescricao)
    {
        var inicial = admissao;
        if (aplicarPrescricao)
        {
            var prescricao = CalcularData(ajuizamento, admissao);
            if (prescricao > inicial)
                inicial = prescricao;
        }

        var final = demissao ?? terminoCalculo;
        return (inicial, final);
    }
}
