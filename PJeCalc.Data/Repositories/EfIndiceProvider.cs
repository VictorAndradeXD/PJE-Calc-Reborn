using PJeCalc.Core.Enums;
using PJeCalc.Core.Models.Indices;
using PJeCalc.Core.Services.CorrecaoMonetaria;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>
/// Implementação de <see cref="IIndiceProvider"/> que lê as séries mensais de índices
/// do banco de referência via EF Core. Escopo atual: índices mensais suportados pelo
/// <see cref="CorrecaoMonetariaService"/>. Índices diários (JAM, SELIC diária, Tabela
/// Única) entram quando a correção avançada for implementada.
/// </summary>
public sealed class EfIndiceProvider : IIndiceProvider
{
    private readonly ReferenciaDbContext _db;

    public EfIndiceProvider(ReferenciaDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public IReadOnlyList<IndiceMensal> ObterSerieMensal(IndiceMonetarioEnum indice)
    {
        IQueryable<IndiceBase> query = indice switch
        {
            IndiceMonetarioEnum.IGPM => _db.IndicesIGPM,
            IndiceMonetarioEnum.INPC => _db.IndicesINPC,
            IndiceMonetarioEnum.IPC => _db.IndicesIPC,
            IndiceMonetarioEnum.IPCA => _db.IndicesIPCA,
            IndiceMonetarioEnum.IPCAE => _db.IndicesIPCAE,
            IndiceMonetarioEnum.IPCAETR => _db.IndicesIPCAETR,
            IndiceMonetarioEnum.TR => _db.IndicesTR,
            IndiceMonetarioEnum.SelicFazenda => _db.IndicesSelicFazenda,
            _ => throw new NotSupportedException(
                $"Índice {indice} ainda não disponível no provider EF (série mensal).")
        };

        // DateOnly.FromDateTime não é traduzível em SQL; materializa e projeta em memória.
        return query
            .OrderBy(i => i.Competencia)
            .Select(i => new { i.Competencia, i.Taxa })
            .AsEnumerable()
            .Select(x => new IndiceMensal(DateOnly.FromDateTime(x.Competencia), x.Taxa))
            .ToList();
    }
}
