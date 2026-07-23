namespace PJeCalc.Core.Models.Referencia;

/// <summary>
/// Parâmetros tabelados das custas no banco de referência (espelho de TBPARAMETROCUSTAS):
/// piso das custas de conhecimento, tetos de liquidação/autos e os nove valores fixos por
/// tipo de ato. Vigem por período (<see cref="FimVigencia"/> nulo = registro atual).
/// </summary>
public class ParametroDeCustasReferencia
{
    public long Id { get; set; }
    public DateTime InicioVigencia { get; set; }
    public DateTime? FimVigencia { get; set; }

    public decimal PisoConhecimento { get; set; }
    public decimal TetoLiquidacao { get; set; }
    public decimal TetoAutos { get; set; }

    public decimal AtosUrbanos { get; set; }
    public decimal AtosRurais { get; set; }
    public decimal AgravoInstrumento { get; set; }
    public decimal AgravoPeticao { get; set; }
    public decimal ImpugnacaoSentenca { get; set; }
    public decimal EmbargosArrematacao { get; set; }
    public decimal EmbargosExecucao { get; set; }
    public decimal EmbargosTerceiros { get; set; }
    public decimal RecursoRevista { get; set; }
}
