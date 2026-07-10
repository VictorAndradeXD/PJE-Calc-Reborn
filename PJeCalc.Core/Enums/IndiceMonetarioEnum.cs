using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Indices de correção monetária disponíveis para cálculos trabalhistas e judiciais.
/// </summary>
public enum IndiceMonetarioEnum
{
    [Description("Índice Trabalhista")]
    IndiceTrabalhista,

    [Description("Tabela Única de Atualização e Conversão de Débitos Trabalhistas")]
    TUACDT,

    [Description("Devedor Fazenda Pública")]
    TabelaDevedorFazenda,

    [Description("Repetição de Indébito Tributário")]
    TabelaIndebitoTributario,

    [Description("Tabela JT Mensal")]
    TabelaUnicaJTMensal,

    [Description("Tabela JT Diária")]
    TabelaUnicaJTDiario,

    [Description("TR")]
    TR,

    [Description("IGP-M")]
    IGPM,

    [Description("INPC")]
    INPC,

    [Description("IPC")]
    IPC,

    [Description("IPCA")]
    IPCA,

    [Description("IPCA-E")]
    IPCAE,

    [Description("IPCA-E/TR")]
    IPCAETR,

    [Description("JAM")]
    JAM,

    [Description("SELIC (Receita Federal)")]
    Selic,

    [Description("SELIC Simples")]
    SelicFazenda,

    [Description("SELIC Composta")]
    SelicBacen,

    [Description("Sem Correção")]
    SemCorrecao
}
