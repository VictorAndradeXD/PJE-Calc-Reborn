using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Faixa histórica de juros de uma tabela (ex.: JurosPadrão): vigência, alíquota e
/// regime de capitalização/contagem. Uma tabela é uma sequência de faixas contíguas.
/// </summary>
/// <param name="DataInicio">Início de vigência da faixa.</param>
/// <param name="DataFim">Fim de vigência (nulo = faixa em aberto, vigente).</param>
/// <param name="Aliquota">Alíquota da faixa, em pontos percentuais.</param>
/// <param name="Capitalizacao">Capitalização (simples ou composta).</param>
/// <param name="Quantidade">Contagem das pontas parciais (fração ou inteiro/15 dias).</param>
public sealed record FaixaDeJuros(
    DateOnly DataInicio,
    DateOnly? DataFim,
    decimal Aliquota,
    TipoDeJurosEnum Capitalizacao,
    TipoDeQuantidadeDeJurosBaseEnum Quantidade);
