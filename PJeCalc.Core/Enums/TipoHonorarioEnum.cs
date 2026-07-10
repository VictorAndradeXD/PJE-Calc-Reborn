using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Tipos de honorarios.
/// </summary>
public enum TipoHonorarioEnum
{
    [Description("Honorários Advocatícios")]
    Advocaticios,

    [Description("Honorários Assistenciais")]
    Assistenciais,

    [Description("Honorários Contratuais")]
    Contratuais,

    [Description("Honorários Periciais - Contador")]
    PericiaisContador,

    [Description("Honorários Periciais - Documentoscópio")]
    PericiaisDocumentoscopio,

    [Description("Honorários Periciais - Engenheiro")]
    PericiaisEngenheiro,

    [Description("Honorários Periciais - Intérprete")]
    PericiaisInterprete,

    [Description("Honorários Periciais - Médico")]
    PericiaisMedico,

    [Description("Honorários Periciais - Outros")]
    PericiaisOutros,

    [Description("Honorários de Sucumbência")]
    Sucumbenciais,

    [Description("Leiloeiro")]
    Leiloeiro
}
