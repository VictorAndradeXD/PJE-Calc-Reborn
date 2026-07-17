using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Models.Auditoria;
using PJeCalc.Core.Models.Calculo;
using PJeCalc.Core.Models.CartaoDePonto;
using PJeCalc.Core.Models.Custas;
using PJeCalc.Core.Models.Faltas;
using PJeCalc.Core.Models.Feriado;
using PJeCalc.Core.Models.Ferias;
using PJeCalc.Core.Models.Fgts;
using PJeCalc.Core.Models.HistoricoSalarial;
using PJeCalc.Core.Models.Honorarios;
using PJeCalc.Core.Models.Indices;
using PJeCalc.Core.Models.Inss;
using PJeCalc.Core.Models.Irpf;
using PJeCalc.Core.Models.Juros;
using PJeCalc.Core.Models.Multas;
using PJeCalc.Core.Models.Pagamento;
using PJeCalc.Core.Models.Processo;
using PJeCalc.Core.Models.SalarioCategoria;
using PJeCalc.Core.Models.SalarioMinimo;
using PJeCalc.Core.Models.Usuario;
using PJeCalc.Core.Models.VerbaCalculo;

namespace PJeCalc.Data.Context;

public class PJeCalcDbContext : DbContext
{
    // Core
    public DbSet<Calculo> Calculos => Set<Calculo>();
    public DbSet<ExcecaoCargaHoraria> ExcecoesCargaHoraria => Set<ExcecaoCargaHoraria>();
    public DbSet<ExcecaoSabado> ExcecoesSabado => Set<ExcecaoSabado>();
    public DbSet<ParametrosDeAtualizacao> ParametrosDeAtualizacao => Set<ParametrosDeAtualizacao>();

    // Processo
    public DbSet<Processo> Processos => Set<Processo>();
    public DbSet<Advogado> Advogados => Set<Advogado>();

    // Verbas
    public DbSet<VerbaDeCalculo> Verbas => Set<VerbaDeCalculo>();

    // FGTS
    public DbSet<Fgts> Fgts => Set<Fgts>();
    public DbSet<OcorrenciaDeFgts> OcorrenciasDeFgts => Set<OcorrenciaDeFgts>();
    public DbSet<OperacaoDeFgts> OperacoesDeFgts => Set<OperacaoDeFgts>();

    // INSS
    public DbSet<Inss> Inss => Set<Inss>();
    public DbSet<AliquotaEmpregadorPorPeriodo> AliquotasEmpregadorPorPeriodo => Set<AliquotaEmpregadorPorPeriodo>();

    // IRPF
    public DbSet<Irpf> Irpf => Set<Irpf>();
    public DbSet<OcorrenciaDeIrpf> OcorrenciasDeIrpf => Set<OcorrenciaDeIrpf>();

    // Ferias & Faltas
    public DbSet<Ferias> Ferias => Set<Ferias>();
    public DbSet<Falta> Faltas => Set<Falta>();

    // Juros
    public DbSet<ApuracaoDeJuros> ApuracoesDeJuros => Set<ApuracaoDeJuros>();
    public DbSet<JurosPadrao> JurosPadrao => Set<JurosPadrao>();
    public DbSet<JurosFazendaPublica> JurosFazendaPublica => Set<JurosFazendaPublica>();
    public DbSet<JurosTaxaLegal> JurosTaxaLegal => Set<JurosTaxaLegal>();
    public DbSet<JurosSelicIrpf> JurosSelicIrpf => Set<JurosSelicIrpf>();
    public DbSet<JurosSelicInss> JurosSelicInss => Set<JurosSelicInss>();

    // Multas & Honorarios
    public DbSet<Multa> Multas => Set<Multa>();
    public DbSet<Honorario> Honorarios => Set<Honorario>();

    // Custas
    public DbSet<CustasJudiciais> CustasJudiciais => Set<CustasJudiciais>();
    public DbSet<Armazenamento> Armazenamentos => Set<Armazenamento>();
    public DbSet<AutoJudicial> AutosJudiciais => Set<AutoJudicial>();
    public DbSet<CustaPaga> CustasPagas => Set<CustaPaga>();

    // Historico Salarial
    public DbSet<HistoricoSalarial> HistoricosSalariais => Set<HistoricoSalarial>();
    public DbSet<OcorrenciaDoHistoricoSalarial> OcorrenciasDoHistoricoSalarial => Set<OcorrenciaDoHistoricoSalarial>();

    // Cartao de Ponto
    public DbSet<CartaoDePonto> CartoesDePonto => Set<CartaoDePonto>();
    public DbSet<OcorrenciaDoCartaoDePonto> OcorrenciasDoCartaoDePonto => Set<OcorrenciaDoCartaoDePonto>();

    // Pagamento
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    // Salarios
    public DbSet<SalarioMinimoNacional> SalariosMinimos => Set<SalarioMinimoNacional>();
    public DbSet<SalarioCategoria> SalariosCategorias => Set<SalarioCategoria>();
    public DbSet<OcorrenciaDeSalarioCategoria> OcorrenciasDeSalarioCategoria => Set<OcorrenciaDeSalarioCategoria>();

    // Indices Monetarios
    public DbSet<IndiceIGPM> IndicesIGPM => Set<IndiceIGPM>();
    public DbSet<IndiceINPC> IndicesINPC => Set<IndiceINPC>();
    public DbSet<IndiceIPC> IndicesIPC => Set<IndiceIPC>();
    public DbSet<IndiceIPCA> IndicesIPCA => Set<IndiceIPCA>();
    public DbSet<IndiceIPCAE> IndicesIPCAE => Set<IndiceIPCAE>();
    public DbSet<IndiceIPCAETR> IndicesIPCAETR => Set<IndiceIPCAETR>();
    public DbSet<IndiceTR> IndicesTR => Set<IndiceTR>();
    public DbSet<IndiceJAM> IndicesJAM => Set<IndiceJAM>();
    public DbSet<IndiceSelicDiaria> IndicesSelicDiaria => Set<IndiceSelicDiaria>();
    public DbSet<IndiceSelicFazenda> IndicesSelicFazenda => Set<IndiceSelicFazenda>();
    public DbSet<IndiceTUACDT> IndicesTUACDT => Set<IndiceTUACDT>();

    // Feriados
    public DbSet<Feriado> Feriados => Set<Feriado>();
    public DbSet<ExcecaoDoFeriado> ExcecoesDoFeriado => Set<ExcecaoDoFeriado>();

    // Usuarios & Auditoria
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SetorUsuario> SetoresUsuario => Set<SetorUsuario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    public PJeCalcDbContext(DbContextOptions<PJeCalcDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Processo owns IdentificadorDoProcesso, Reclamante, Reclamado
        modelBuilder.Entity<Processo>(entity =>
        {
            entity.OwnsOne(p => p.Identificador);
            entity.OwnsOne(p => p.Reclamante);
            entity.OwnsOne(p => p.Reclamado);
        });

        // Configure one-to-one relationships from Calculo
        modelBuilder.Entity<Calculo>(entity =>
        {
            entity.HasOne(c => c.Processo).WithOne(p => p.Calculo).HasForeignKey<Processo>(p => p.CalculoId);
            entity.HasOne(c => c.Fgts).WithOne(f => f.Calculo).HasForeignKey<Fgts>(f => f.CalculoId);
            entity.HasOne(c => c.Inss).WithOne(i => i.Calculo).HasForeignKey<Inss>(i => i.CalculoId);
            entity.HasOne(c => c.Irpf).WithOne(i => i.Calculo).HasForeignKey<Irpf>(i => i.CalculoId);
            entity.HasOne(c => c.CustasJudiciais).WithOne(c => c.Calculo).HasForeignKey<CustasJudiciais>(c => c.CalculoId);
            entity.HasOne(c => c.ParametrosDeAtualizacao).WithOne(p => p.Calculo).HasForeignKey<ParametrosDeAtualizacao>(p => p.CalculoId);
        });

        // Store enums as strings
        modelBuilder.Entity<Calculo>().Property(c => c.TipoDeCalculo).HasConversion<string>();
        modelBuilder.Entity<Calculo>().Property(c => c.RegimeDoContrato).HasConversion<string>();
    }
}
