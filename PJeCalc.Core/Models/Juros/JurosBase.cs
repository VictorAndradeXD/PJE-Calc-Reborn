namespace PJeCalc.Core.Models.Juros;

using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;

public abstract class JurosBase : EntityBase
{
    public DateTime DataInicio { get; set; }

    /// <summary>Fim de vigência; nulo indica a faixa em aberto (vigente).</summary>
    public DateTime? DataFim { get; set; }
    public decimal Aliquota { get; set; }
    public TipoDeJurosEnum? TipoDeJuros { get; set; }

    /// <summary>Contagem das pontas parciais (fração ou inteiro/regra dos 15 dias).</summary>
    public TipoDeQuantidadeDeJurosBaseEnum? TipoDeQuantidade { get; set; }
}
