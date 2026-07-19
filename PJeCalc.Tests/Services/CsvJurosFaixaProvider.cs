using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Juros;

namespace PJeCalc.Tests.Services;

/// <summary>
/// <see cref="IJurosFaixaProvider"/> de teste que lê as faixas de juros dos CSV
/// extraídos do banco oficial (Fixtures/Juros), garantindo que os testes usam os mesmos
/// dados da geração dos golden.
/// </summary>
public sealed class CsvJurosFaixaProvider : IJurosFaixaProvider
{
    private static readonly string FixturesDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Juros");

    private static readonly IReadOnlyDictionary<JurosEnum, string> Arquivos =
        new Dictionary<JurosEnum, string>
        {
            [JurosEnum.JurosPadrao] = "jurospadrao.csv",
        };

    private readonly Dictionary<JurosEnum, IReadOnlyList<FaixaDeJuros>> _cache = [];

    public IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim)
    {
        if (!_cache.TryGetValue(regime, out var todas))
        {
            if (!Arquivos.TryGetValue(regime, out var arquivo))
                throw new NotSupportedException($"Sem fixture de faixas para o regime {regime}.");
            todas = Carregar(Path.Combine(FixturesDir, arquivo));
            _cache[regime] = todas;
        }

        // Faixas que se sobrepõem ao intervalo [inicio, fim].
        return todas
            .Where(f => f.DataInicio <= fim && (f.DataFim is null || f.DataFim >= inicio))
            .OrderBy(f => f.DataInicio)
            .ToList();
    }

    private static List<FaixaDeJuros> Carregar(string caminho)
    {
        var faixas = new List<FaixaDeJuros>();
        // Formato: "DATAINICIO","DATAFIM","TAXA","TIPOJUROS","TIPOQUANTIDADE" (com cabeçalho).
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var p = linha.Split(',');
            var inicio = DateOnly.ParseExact(Limpar(p[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateOnly? fim = string.IsNullOrWhiteSpace(Limpar(p[1]))
                ? null
                : DateOnly.ParseExact(Limpar(p[1]), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var taxa = decimal.Parse(Limpar(p[2]), NumberStyles.Float, CultureInfo.InvariantCulture);
            var tipo = Limpar(p[3]) == "C" ? TipoDeJurosEnum.Compostos : TipoDeJurosEnum.Simples;
            var quantidade = Limpar(p[4]) == "I"
                ? TipoDeQuantidadeDeJurosBaseEnum.Inteiro
                : TipoDeQuantidadeDeJurosBaseEnum.Fracao;

            faixas.Add(new FaixaDeJuros(inicio, fim, taxa, tipo, quantidade));
        }
        return faixas;
    }

    private static string Limpar(string campo) => campo.Trim().Trim('"');
}
