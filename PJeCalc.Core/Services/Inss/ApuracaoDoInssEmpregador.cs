namespace PJeCalc.Core.Services.Inss;

/// <summary>
/// Alíquotas patronais vigentes num período (configuradas pelo usuário, por faixa de datas):
/// empresa (tipicamente 20%), RAT/SAT (risco ambiental, 1%–3% por atividade) e terceiros
/// (sistema S, ~5,8%). Em pontos percentuais.
/// </summary>
public sealed record AliquotasDoEmpregador(
    DateOnly Inicio,
    DateOnly Fim,
    decimal Empresa,
    decimal Rat,
    decimal Terceiros)
{
    public bool Contem(DateOnly competencia) => competencia >= Inicio && competencia <= Fim;

    /// <summary>Alíquotas vigentes na competência, ou nulo quando nenhum período a cobre.</summary>
    public static AliquotasDoEmpregador? Vigentes(
        IEnumerable<AliquotasDoEmpregador> aliquotas, DateOnly competencia) =>
        aliquotas.FirstOrDefault(a => a.Contem(competencia));
}

/// <summary>Cotas patronais devidas numa competência.</summary>
/// <param name="EmpresaSobreBaseTotal">Empresa sobre a base total (histórico + verbas), com teto.</param>
/// <param name="EmpresaDevida">Empresa devida na ação (sobre as verbas, respeitando o teto).</param>
/// <param name="Sat">SAT/RAT sobre a base das verbas.</param>
/// <param name="Terceiros">Terceiros sobre a base das verbas.</param>
public sealed record CotasDoEmpregador(
    decimal EmpresaSobreBaseTotal,
    decimal EmpresaDevida,
    decimal Sat,
    decimal Terceiros);

/// <summary>
/// Apuração das cotas patronais do INSS sobre os salários devidos, numa competência.
///
/// <para>A cota da empresa sobre o histórico já recolhido (respeitando o teto) serve de
/// referência; o valor <b>devido</b> na ação recai sobre a parcela das verbas — limitado ao que
/// resta do teto após o histórico. SAT e terceiros incidem apenas sobre a base das verbas. No
/// Simples Nacional a contribuição patronal é substituída, então todas as cotas zeram.</para>
///
/// <para>Fórmulas transcritas de <c>MaquinaDeCalculoDoInss.liquidarInssSobreSalariosDevidos</c>
/// (a máquina do original é ampla demais para ser dirigida sem toda a infraestrutura; a base por
/// competência — histórico com proporcionalidade + verbas + avos do 13º — é a geração, ainda
/// adiada, e entra aqui como parâmetro).</para>
/// </summary>
public static class ApuracaoDoInssEmpregador
{
    /// <param name="baseHistorico">Base do histórico salarial já recolhido (a competência-referência).</param>
    /// <param name="baseVerbas">Parcela da base vinda das verbas (o que se cobra na ação).</param>
    /// <param name="aliquotas">Alíquotas patronais vigentes na competência.</param>
    /// <param name="tetoEmpresa">Teto da contribuição patronal da empresa; nulo quando não há teto.</param>
    /// <param name="aplicarSimples">Simples Nacional: zera toda a contribuição patronal.</param>
    public static CotasDoEmpregador Calcular(
        decimal baseHistorico,
        decimal baseVerbas,
        AliquotasDoEmpregador aliquotas,
        decimal? tetoEmpresa = null,
        bool aplicarSimples = false)
    {
        ArgumentNullException.ThrowIfNull(aliquotas);

        if (aplicarSimples)
            return new CotasDoEmpregador(0m, 0m, 0m, 0m);

        var empresaSobreBaseTotal = baseHistorico * (aliquotas.Empresa / 100m);
        if (tetoEmpresa is { } teto && empresaSobreBaseTotal > teto)
            empresaSobreBaseTotal = teto;

        var empresaSobreVerbas = baseVerbas * (aliquotas.Empresa / 100m);
        var empresaDevida = tetoEmpresa is { } t
            ? Math.Min(t - empresaSobreBaseTotal, empresaSobreVerbas)
            : empresaSobreVerbas;

        var sat = baseVerbas * (aliquotas.Rat / 100m);
        var terceiros = baseVerbas * (aliquotas.Terceiros / 100m);

        return new CotasDoEmpregador(empresaSobreBaseTotal, empresaDevida, sat, terceiros);
    }
}
