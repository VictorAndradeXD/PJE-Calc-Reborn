using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Calcula a taxa acumulada de juros de mora de uma tabela por faixas (JurosPadrão,
/// Fazenda Pública), entre o início dos juros e a liquidação.
///
/// <para>A janela é recortada nas fronteiras das faixas: a faixa que contém o início
/// começa nessa data, a última termina na liquidação. A taxa de cada trecho vem do
/// <see cref="PeriodoDeJuros"/> e é somada (regimes não compostos — JurosPadrão/Fazenda
/// acumulam por soma, ainda que uma faixa seja de capitalização composta por trecho).</para>
///
/// <para>Escopo atual: regimes de alíquota fixa (1%/0,5%/0,0333% a.d.), regimes tabelados
/// por faixa (JurosPadrão/Fazenda) e a projeção da data inicial dos juros. Ainda NÃO cobre
/// — próximos incrementos: SELIC/TRD (acumulação por competência, com composição em
/// TRD_Compostos/SELIC_Bacen) e combinações de tabela.</para>
/// </summary>
public sealed class TabelaDeJurosService
{
    // Alíquotas constantes dos regimes fixos (montarPeriodo do original).
    private const decimal AliquotaMeioPorcento = 0.5m;
    private const decimal AliquotaZeroTrintaTres = 0.0333333m;

    private readonly IJurosFaixaProvider _faixas;

    public TabelaDeJurosService(IJurosFaixaProvider faixas)
    {
        ArgumentNullException.ThrowIfNull(faixas);
        _faixas = faixas;
    }

    /// <summary>
    /// Taxa acumulada (em pontos percentuais) do <paramref name="regime"/>, do início
    /// dos juros até a liquidação.
    /// </summary>
    public decimal CalcularTaxaAcumulada(JurosEnum regime, DateOnly dataInicioJuros, DateOnly dataLiquidacao)
    {
        if (regime == JurosEnum.SemJuros || dataLiquidacao < dataInicioJuros)
            return 0m;

        if (AliquotaFixaDe(regime) is decimal aliquota)
        {
            return new PeriodoDeJuros
            {
                Inicio = dataInicioJuros,
                Fim = dataLiquidacao,
                Aliquota = aliquota,
                Quantidade = TipoDeQuantidadeDeJurosBaseEnum.Fracao,
                Capitalizacao = TipoDeJurosEnum.Simples,
                Tabela = regime,
            }.Taxa;
        }

        var faixas = _faixas.ObterFaixas(regime, dataInicioJuros, dataLiquidacao)
            .OrderBy(f => f.DataInicio);

        var total = 0m;
        foreach (var faixa in faixas)
        {
            var inicio = Maior(dataInicioJuros, faixa.DataInicio);
            var fim = Menor(dataLiquidacao, faixa.DataFim ?? dataLiquidacao);
            if (fim < inicio)
                continue;

            var periodo = new PeriodoDeJuros
            {
                Inicio = inicio,
                Fim = fim,
                Aliquota = faixa.Aliquota,
                Quantidade = faixa.Quantidade,
                Capitalizacao = faixa.Capitalizacao,
                Tabela = regime,
            };
            total += periodo.Taxa;
        }
        return total;
    }

    /// <summary>
    /// Data em que os juros passam a incidir para uma verba, no regime de ocorrências
    /// vencidas: 1º dia do mês seguinte ao vencimento, nunca antes do dia seguinte ao
    /// ajuizamento. Com fase pré-judicial, ignora o piso do ajuizamento.
    /// </summary>
    public static DateOnly ProjetarInicioDosJuros(
        DateOnly vencimento, DateOnly? ajuizamento, bool aplicarFasePreJudicial)
    {
        var mesSeguinte = UltimoDiaDoMes(vencimento).AddDays(1);

        if (aplicarFasePreJudicial || ajuizamento is null || UltimoDiaDoMes(vencimento) >= ajuizamento)
            return mesSeguinte;

        return ajuizamento.Value.AddDays(1);
    }

    /// <summary>Alíquota constante do regime fixo, ou nulo se for regime tabelado.</summary>
    private static decimal? AliquotaFixaDe(JurosEnum regime) => regime switch
    {
        JurosEnum.JurosUmPorcento => 1m,
        JurosEnum.JurosMeioPorcento => AliquotaMeioPorcento,
        JurosEnum.JurosZeroTrintaTres => AliquotaZeroTrintaTres,
        _ => null,
    };

    private static DateOnly UltimoDiaDoMes(DateOnly data) =>
        new(data.Year, data.Month, DateTime.DaysInMonth(data.Year, data.Month));

    private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;
}
