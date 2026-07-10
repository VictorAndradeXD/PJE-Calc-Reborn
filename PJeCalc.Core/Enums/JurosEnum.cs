using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipos de juros aplicáveis aos cálculos judiciais.
/// </summary>
public enum JurosEnum
{
    [Description("Juros Padrão")]
    JurosPadrao,

    [Description("Juros Caderneta de Poupança")]
    JurosPoupanca,

    [Description("Juros Fazenda Pública")]
    FazendaPublica,

    [Description("Juros Simples 0,5% a.m.")]
    JurosMeioPorcento,

    [Description("Juros Simples 1,0% a.m.")]
    JurosUmPorcento,

    [Description("Juros Simples 0,0333333% a.d.")]
    JurosZeroTrintaTres,

    [Description("Juros Precatório EC 136/2025")]
    JurosPrecatorioEC136_2025,

    [Description("SELIC (Receita Federal)")]
    Selic,

    [Description("SELIC Simples")]
    SelicFazenda,

    [Description("SELIC Composta")]
    SelicBacen,

    [Description("TRD Juros Simples")]
    TRDSimples,

    [Description("TRD Juros Compostos")]
    TRDCompostos,

    [Description("Taxa Legal")]
    TaxaLegal,

    [Description("Sem Juros")]
    SemJuros
}
