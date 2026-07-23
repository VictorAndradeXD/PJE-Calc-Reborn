using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Models.Referencia;

/// <summary>
/// Feriado do banco de referência (espelho de TBFERIADO do PJe-Calc): fixos ocorrem no
/// dia/mês de <see cref="Data"/> dentro da vigência; móveis, nas datas exatas das
/// <see cref="Excecoes"/> (uma por ano). Em feriados fixos, a exceção suprime a data.
/// </summary>
public class FeriadoReferencia
{
    public long Id { get; set; }
    public TipoDeFeriadoEnum Tipo { get; set; }
    public AbrangenciaDoFeriadoEnum Abrangencia { get; set; }
    public string? Estado { get; set; }
    public long? Municipio { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime? Data { get; set; }
    public DateTime InicioVigencia { get; set; }
    public DateTime? FimVigencia { get; set; }
    public bool Movel { get; set; }

    public List<ExcecaoDeFeriadoReferencia> Excecoes { get; set; } = [];
}

public class ExcecaoDeFeriadoReferencia
{
    public long Id { get; set; }
    public long FeriadoReferenciaId { get; set; }
    public DateTime Data { get; set; }
}
