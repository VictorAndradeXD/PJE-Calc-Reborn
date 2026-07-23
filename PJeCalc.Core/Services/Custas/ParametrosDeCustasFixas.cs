namespace PJeCalc.Core.Services.Custas;

/// <summary>
/// Parâmetros tabelados das custas (TBPARAMETROCUSTAS): piso das custas de conhecimento, tetos
/// de liquidação e de autos, e os nove valores fixos por tipo de ato. Vigem por período; na
/// versão 2.15.1 há um único registro vigente desde 27/09/2002.
/// </summary>
public sealed record ParametrosDeCustasFixas
{
    public required decimal PisoConhecimento { get; init; }
    public required decimal TetoLiquidacao { get; init; }
    public required decimal TetoAutos { get; init; }

    public required decimal AtosUrbanos { get; init; }
    public required decimal AtosRurais { get; init; }
    public required decimal AgravoInstrumento { get; init; }
    public required decimal AgravoPeticao { get; init; }
    public required decimal ImpugnacaoSentenca { get; init; }
    public required decimal EmbargosArrematacao { get; init; }
    public required decimal EmbargosExecucao { get; init; }
    public required decimal EmbargosTerceiros { get; init; }
    public required decimal RecursoRevista { get; init; }
}
