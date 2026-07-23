using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>
/// Carrega os feriados do banco de referência e os expõe como
/// <see cref="ProvedorDeFeriados"/> para alimentar o calendário trabalhista
/// (<see cref="ContextoDeVerbas.EhFeriado"/>).
/// </summary>
public sealed class EfFeriadoProvider(ReferenciaDbContext contexto)
{
    private readonly ReferenciaDbContext _contexto = contexto;

    public IReadOnlyList<FeriadoCadastrado> CarregarTodos() =>
        _contexto.Feriados
            .AsNoTracking()
            .Include(f => f.Excecoes)
            .OrderBy(f => f.Id)
            .AsEnumerable()
            .Select(f => new FeriadoCadastrado(
                f.Tipo,
                f.Abrangencia,
                f.Estado,
                f.Municipio,
                f.Nome,
                f.Data is { } data ? DateOnly.FromDateTime(data) : null,
                DateOnly.FromDateTime(f.InicioVigencia),
                f.FimVigencia is { } fim ? DateOnly.FromDateTime(fim) : null,
                f.Movel,
                f.Excecoes.Select(e => DateOnly.FromDateTime(e.Data)).ToHashSet()))
            .ToList();

    public ProvedorDeFeriados CriarProvedor() => new(CarregarTodos());
}
