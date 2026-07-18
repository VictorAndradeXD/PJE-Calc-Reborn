using Microsoft.EntityFrameworkCore;

namespace PJeCalc.Data.Context;

/// <summary>
/// Abre o banco de referência pré-construído (referencia.sqlite) em modo somente-leitura.
/// Ponto de entrada único para a aplicação e os testes obterem um <see cref="ReferenciaDbContext"/>
/// sem repetir a montagem de caminho e opções.
/// </summary>
public static class ReferenciaDbContextFactory
{
    /// <summary>Caminho do banco de referência copiado para a saída do build.</summary>
    public static string CaminhoPadrao =>
        Path.Combine(AppContext.BaseDirectory, "Referencia", "referencia.sqlite");

    /// <summary>Cria um contexto somente-leitura sobre o banco de referência.</summary>
    /// <param name="caminho">Caminho alternativo do arquivo; usa <see cref="CaminhoPadrao"/> se nulo.</param>
    public static ReferenciaDbContext Criar(string? caminho = null)
    {
        var arquivo = caminho ?? CaminhoPadrao;
        var options = new DbContextOptionsBuilder<ReferenciaDbContext>()
            .UseSqlite($"Data Source={arquivo};Mode=ReadOnly")
            .Options;
        return new ReferenciaDbContext(options);
    }
}
