using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>Regime de imposto de renda do credor do honorário.</summary>
public enum TipoDeImpostoDeRendaEnum
{
    [Description("Pessoa Física")]
    PessoaFisica,

    [Description("Pessoa Jurídica")]
    PessoaJuridica
}
