using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Tests.Services;

/// <summary>
/// <see cref="IIndiceProvider"/> de teste que lê as séries mensais dos arquivos CSV
/// extraídos do banco oficial do PJe-Calc (pasta Fixtures/Indices). Garante que os
/// testes C# consomem exatamente os mesmos dados usados na geração dos golden values.
/// </summary>
public sealed class CsvIndiceProvider : IIndiceProvider
{
    private static readonly string FixturesDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Indices");

    private static readonly IReadOnlyDictionary<IndiceMonetarioEnum, string> Arquivos =
        new Dictionary<IndiceMonetarioEnum, string>
        {
            [IndiceMonetarioEnum.IPCAE] = "ipcae.csv",
            [IndiceMonetarioEnum.IPCA] = "ipca.csv",
            [IndiceMonetarioEnum.INPC] = "inpc.csv",
            [IndiceMonetarioEnum.IGPM] = "igpm.csv",
            [IndiceMonetarioEnum.TR] = "tr.csv",
        };

    private readonly Dictionary<IndiceMonetarioEnum, IReadOnlyList<IndiceMensal>> _cache = [];

    public IReadOnlyList<IndiceMensal> ObterSerieMensal(IndiceMonetarioEnum indice)
    {
        if (_cache.TryGetValue(indice, out var serie))
            return serie;

        if (!Arquivos.TryGetValue(indice, out var arquivo))
            throw new NotSupportedException($"Sem fixture CSV para o índice {indice}.");

        serie = Carregar(Path.Combine(FixturesDir, arquivo));
        _cache[indice] = serie;
        return serie;
    }

    private static List<IndiceMensal> Carregar(string caminho)
    {
        var linhas = File.ReadAllLines(caminho);
        var serie = new List<IndiceMensal>(linhas.Length);
        // Formato: "COMPETENCIA","TAXA" (primeira linha é cabeçalho).
        for (var i = 1; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var partes = linha.Split(',');
            var competencia = DateOnly.ParseExact(Desaspar(partes[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var taxa = decimal.Parse(Desaspar(partes[1]), NumberStyles.Float, CultureInfo.InvariantCulture);
            serie.Add(new IndiceMensal(competencia, taxa));
        }
        return serie;
    }

    private static string Desaspar(string campo) => campo.Trim().Trim('"');
}
