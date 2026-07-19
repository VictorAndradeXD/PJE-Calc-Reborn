using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Juros;

/// <summary>
/// Trecho contínuo de incidência de juros, com uma alíquota e regime de capitalização.
/// Expõe a matemática por período do PJe-Calc: contagem de meses (com fração ou regra
/// dos ≥15 dias) e a taxa acumulada do trecho (diária, simples ou composta).
/// </summary>
public sealed record PeriodoDeJuros
{
    /// <summary>Primeiro dia de incidência.</summary>
    public required DateOnly Inicio { get; init; }

    /// <summary>Último dia de incidência (inclusivo).</summary>
    public required DateOnly Fim { get; init; }

    /// <summary>Alíquota do trecho, em pontos percentuais (ex.: 1 = 1% a.m.).</summary>
    public required decimal Aliquota { get; init; }

    /// <summary>Regra de contagem das pontas parciais (fração ou inteiro/15 dias).</summary>
    public TipoDeQuantidadeDeJurosBaseEnum Quantidade { get; init; } = TipoDeQuantidadeDeJurosBaseEnum.Fracao;

    /// <summary>Capitalização (simples ou composta).</summary>
    public TipoDeJurosEnum Capitalizacao { get; init; } = TipoDeJurosEnum.Simples;

    /// <summary>Tabela/regime de origem — distingue o regime diário (0,0333% a.d.).</summary>
    public JurosEnum Tabela { get; init; } = JurosEnum.JurosPadrao;

    /// <summary>Dias corridos do trecho, inclusivo nas duas pontas.</summary>
    public long Dias => (Fim.DayNumber - Inicio.DayNumber) + 1;

    /// <summary>
    /// Quantidade de meses do trecho. Base: meses inclusivos de 1º a 1º. Fração desconta
    /// pro-rata as pontas parciais; inteiro aplica a regra dos ≥15 dias (mês só conta
    /// cheio a partir de 15 dias).
    /// </summary>
    public decimal Meses
    {
        get
        {
            var meses = (decimal)ContarMeses(Inicio, Fim);

            var diasPrimeiroMes = UltimoDiaDoMes(Inicio).DayNumber - Inicio.DayNumber + 1;
            var totalDiasPrimeiroMes = DateTime.DaysInMonth(Inicio.Year, Inicio.Month);
            var diaFinal = Fim.Day;
            var totalDiasUltimoMes = DateTime.DaysInMonth(Fim.Year, Fim.Month);

            if (Quantidade == TipoDeQuantidadeDeJurosBaseEnum.Fracao)
            {
                if (diasPrimeiroMes < totalDiasPrimeiroMes)
                    meses = meses - 1m + (decimal)diasPrimeiroMes / totalDiasPrimeiroMes;
                if (diaFinal < totalDiasUltimoMes)
                    meses = meses - 1m + (decimal)diaFinal / totalDiasUltimoMes;
            }
            else
            {
                if (diasPrimeiroMes < 15) meses -= 1m;
                if (diaFinal < 15) meses -= 1m;
            }

            return meses;
        }
    }

    /// <summary>
    /// Taxa acumulada do trecho, em pontos percentuais. Regime diário: alíquota × dias.
    /// Simples: alíquota × meses. Composto: (1,01^meses − 1) × 100 (1% capitalizado,
    /// como no motor oficial — a base é fixa em 1%, via aritmética de ponto flutuante).
    /// </summary>
    public decimal Taxa
    {
        get
        {
            if (Tabela == JurosEnum.JurosZeroTrintaTres)
                return Aliquota * Dias;

            if (Capitalizacao == TipoDeJurosEnum.Simples)
                return Aliquota * Meses;

            var fator = Math.Pow(1.01, (double)Meses);
            return ((decimal)fator - 1m) * 100m;
        }
    }

    private static int ContarMeses(DateOnly inicio, DateOnly fim) =>
        (fim.Year - inicio.Year) * 12 + (fim.Month - inicio.Month) + 1;

    private static DateOnly UltimoDiaDoMes(DateOnly data) =>
        new(data.Year, data.Month, DateTime.DaysInMonth(data.Year, data.Month));
}
