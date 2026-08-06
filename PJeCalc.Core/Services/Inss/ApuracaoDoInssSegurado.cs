namespace PJeCalc.Core.Services.Inss;

/// <summary>Cota do segurado numa competência.</summary>
/// <param name="SeguradoSobreHistorico">Segurado sobre o histórico já recolhido, com teto.</param>
/// <param name="SeguradoDevido">Segurado devido na ação (sobre as verbas, respeitando o teto).</param>
public sealed record CotaDoSegurado(decimal SeguradoSobreHistorico, decimal SeguradoDevido);

/// <summary>
/// Apuração da cota do segurado sobre os salários devidos, numa competência.
///
/// <para>A alíquota do segurado é a da tabela progressiva do salário: sobre o histórico usa-se a
/// alíquota da base do histórico; sobre as verbas usa-se a alíquota do total (histórico + verbas),
/// pois a progressividade considera o salário cheio. O devido na ação recai sobre a parcela das
/// verbas, limitado ao que resta do teto do segurado após o histórico.</para>
///
/// <para>Fórmulas transcritas de <c>MaquinaDeCalculoDoInss.liquidarInssSobreSalariosDevidos</c>
/// (a alíquota efetiva vem da <see cref="TabelaPrevidenciariaDoSegurado"/>, já validada; a base por
/// competência é a geração — <see cref="GeradorDaBaseDeInss"/> para as verbas).</para>
/// </summary>
public static class ApuracaoDoInssSegurado
{
    /// <param name="baseHistorico">Base do histórico salarial já recolhido.</param>
    /// <param name="baseVerbas">Parcela da base vinda das verbas (o que se cobra na ação).</param>
    /// <param name="tabela">Tabela previdenciária do segurado da competência.</param>
    /// <param name="tetoSegurado">Teto da contribuição do segurado; nulo quando não há teto.</param>
    public static CotaDoSegurado Calcular(
        decimal baseHistorico,
        decimal baseVerbas,
        TabelaPrevidenciariaDoSegurado tabela,
        decimal? tetoSegurado = null)
    {
        ArgumentNullException.ThrowIfNull(tabela);

        var seguradoSobreHistorico = baseHistorico * (tabela.ObterAliquotaParaValor(baseHistorico) / 100m);
        if (tetoSegurado is { } teto && seguradoSobreHistorico > teto)
            seguradoSobreHistorico = teto;

        var aliquotaDoTotal = tabela.ObterAliquotaParaValor(baseHistorico + baseVerbas);
        var seguradoSobreVerbas = baseVerbas * (aliquotaDoTotal / 100m);

        var seguradoDevido = tetoSegurado is { } t
            ? Math.Min(t - seguradoSobreHistorico, seguradoSobreVerbas)
            : seguradoSobreVerbas;

        return new CotaDoSegurado(seguradoSobreHistorico, seguradoDevido);
    }
}
