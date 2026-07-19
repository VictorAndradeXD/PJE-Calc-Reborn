using PJeCalc.Core.Enums;
using PJeCalc.Core.Models.Juros;
using PJeCalc.Core.Services.Juros;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>
/// Implementação de <see cref="IJurosFaixaProvider"/> que lê as faixas de juros do banco
/// de referência via EF Core. Escopo atual: JurosPadrão. Fazenda Pública, SELIC e TRD
/// entram conforme forem implementados no cálculo.
/// </summary>
public sealed class EfJurosFaixaProvider : IJurosFaixaProvider
{
    private readonly ReferenciaDbContext _db;

    public EfJurosFaixaProvider(ReferenciaDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public IReadOnlyList<FaixaDeJuros> ObterFaixas(JurosEnum regime, DateOnly inicio, DateOnly fim)
    {
        IQueryable<JurosBase> query = regime switch
        {
            JurosEnum.JurosPadrao => _db.JurosPadrao,
            _ => throw new NotSupportedException(
                $"Regime de juros {regime} ainda não disponível no provider EF.")
        };

        var inicioDt = inicio.ToDateTime(TimeOnly.MinValue);
        var fimDt = fim.ToDateTime(TimeOnly.MinValue);

        return query
            .Where(f => f.DataInicio <= fimDt && (f.DataFim == null || f.DataFim >= inicioDt))
            .OrderBy(f => f.DataInicio)
            .Select(f => new { f.DataInicio, f.DataFim, f.Aliquota, f.TipoDeJuros, f.TipoDeQuantidade })
            .AsEnumerable()
            .Select(f => new FaixaDeJuros(
                DateOnly.FromDateTime(f.DataInicio),
                f.DataFim is null ? null : DateOnly.FromDateTime(f.DataFim.Value),
                f.Aliquota,
                f.TipoDeJuros ?? TipoDeJurosEnum.Simples,
                f.TipoDeQuantidade ?? TipoDeQuantidadeDeJurosBaseEnum.Fracao))
            .ToList();
    }
}
