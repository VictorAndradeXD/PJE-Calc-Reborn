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
/// <para>Escopo atual: regimes tabelados por faixa. Ainda NÃO cobre — próximos
/// incrementos: regimes de alíquota fixa (1%/0,5%/0,0333% a.d.), SELIC/TRD (acumulação
/// por competência, com composição em TRD_Compostos/SELIC_Bacen), combinações de tabela
/// e a projeção da data inicial (mês seguinte ao vencimento / ajuizamento).</para>
/// </summary>
public sealed class TabelaDeJurosService
{
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

    private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;
}
