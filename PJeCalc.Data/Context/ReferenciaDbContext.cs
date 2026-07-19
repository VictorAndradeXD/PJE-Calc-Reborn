using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Models.Indices;
using PJeCalc.Core.Models.Juros;

namespace PJeCalc.Data.Context;

/// <summary>
/// Contexto do banco de referência (referencia.sqlite): séries históricas de índices
/// monetários, tratadas como dados de referência somente-leitura. Mantido separado do
/// <see cref="PJeCalcDbContext"/> (dados do usuário) — o banco de referência é
/// pré-construído e embarcado, enquanto o banco do usuário é criado por cálculo.
///
/// Escopo atual: índices mensais consumidos pela correção monetária. Índices diários
/// (JAM, SELIC diária, Tabela Única), juros e salário mínimo entram conforme os módulos
/// que os utilizam forem implementados.
/// </summary>
public class ReferenciaDbContext : DbContext
{
    public DbSet<IndiceIGPM> IndicesIGPM => Set<IndiceIGPM>();
    public DbSet<IndiceINPC> IndicesINPC => Set<IndiceINPC>();
    public DbSet<IndiceIPC> IndicesIPC => Set<IndiceIPC>();
    public DbSet<IndiceIPCA> IndicesIPCA => Set<IndiceIPCA>();
    public DbSet<IndiceIPCAE> IndicesIPCAE => Set<IndiceIPCAE>();
    public DbSet<IndiceIPCAETR> IndicesIPCAETR => Set<IndiceIPCAETR>();
    public DbSet<IndiceTR> IndicesTR => Set<IndiceTR>();
    public DbSet<IndiceSelicFazenda> IndicesSelicFazenda => Set<IndiceSelicFazenda>();

    // Faixas de juros por regime.
    public DbSet<JurosPadrao> JurosPadrao => Set<JurosPadrao>();

    public ReferenciaDbContext(DbContextOptions<ReferenciaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enums de juros persistidos como texto (legíveis no banco de referência).
        modelBuilder.Entity<JurosPadrao>().Property(j => j.TipoDeJuros).HasConversion<string>();
        modelBuilder.Entity<JurosPadrao>().Property(j => j.TipoDeQuantidade).HasConversion<string>();
    }
}
