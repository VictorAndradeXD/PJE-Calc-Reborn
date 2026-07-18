using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Models.Indices;

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

    public ReferenciaDbContext(DbContextOptions<ReferenciaDbContext> options) : base(options) { }
}
