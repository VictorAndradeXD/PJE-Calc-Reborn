import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.irpf.Irpf;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.irpf.MaquinaDeCalculoDeIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.irpf.OcorrenciaDeIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.RepositorioDeTabelaIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.TabelaIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.PrimeiraFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.SegundaFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.TerceiraFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.QuartaFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.QuintaFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

/**
 * Golden da GERACAO das ocorrencias de IRPF na liquidacao: dirige
 * MaquinaDeCalculoDeIrpf.liquidar() (classifica as verbas com incidencia de IRPF em baldes -
 * 13o/ferias/demais/anos-anteriores -, escolhe o regime pelo corte 28/07/2010, aloca juros e
 * deducoes fixas, e emite uma ocorrencia por forma de tributacao) e le irpf.getOcorrencias().
 *
 * As deducoes acopladas (INSS/prev/pensao/honorarios) ficam desligadas (deduzir* = false) para
 * isolar a logica NOVA: selecao de regime, bucketing, tipos de tributacao, contagem de
 * competencias do RRA e faixa x NM. Dependentes/aposentado (deducoes fixas da tabela) e a
 * incidencia sobre juros SAO exercitados.
 *
 * Saida: "cenario;chave;valor".
 */
public class GoldenGenIrpfGeracao {

    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");

    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }
    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }

    // ---- Verbas em memoria (getOcorrenciasAtivas filtra por ativo, sem repositorio) ----
    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    static class RepoIrpfStub extends RepositorioDeTabelaIrpf {
        TabelaIrpf tabela;
        RepoIrpfStub(TabelaIrpf t) { this.tabela = t; }
        @Override public TabelaIrpf obterParaA(Date data) { return tabela; }
    }

    static class CalcStub extends Calculo {
        List<VerbaDeCalculo> ativas = new ArrayList<VerbaDeCalculo>();
        Date liquidacao;
        BigDecimal jurosDecimo = BigDecimal.ZERO;
        BigDecimal jurosFerias = BigDecimal.ZERO;
        BigDecimal jurosDemais = BigDecimal.ZERO;

        CalcStub(Date liquidacao) { this.liquidacao = liquidacao; }

        @Override public List<VerbaDeCalculo> getVerbasAtivas() { return ativas; }
        @Override public Date getDataDeLiquidacao() { return liquidacao; }
        @Override public Boolean isCalculoExterno() { return Boolean.FALSE; }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJurosParaIrpfDecimoTerceiro() { return jurosDecimo; }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJurosParaIrpfFerias() { return jurosFerias; }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJurosParaIrpfDemaisVerbas() { return jurosDemais; }
    }

    static VerbaDeCalculo verba(Calculo calc, CaracteristicaDaVerbaEnum carac, String dataInicial, String devido) throws Exception {
        Informada v = new Informada();
        v.setCalculo(calc);
        v.setAtivo(Boolean.TRUE);
        v.setCaracteristica(carac);
        v.setIncidenciaIRPF(Boolean.TRUE);
        OcorrenciaDeVerba o = new OcorrenciaDeVerba();
        o.setDataInicial(d(dataInicial));
        o.setDataFinal(d(dataInicial));
        o.setDevido(b(devido));
        o.setPago(BigDecimal.ZERO);
        o.setIndiceAcumulado(BigDecimal.ONE);
        o.setAtivo(Boolean.TRUE);
        v.adicionarEmOcorrencias(o);
        return v;
    }

    static TabelaIrpf carregar(Connection cn, String competencia) throws Exception {
        String sql = "SELECT RVLINICIALFAIXAUM, RVLFINALFAIXAUM, RVLALIQUOTAFAIXAUM, RVLDEDUCAOFAIXAUM,"
            + " RVLINICIALFAIXADOIS, RVLFINALFAIXADOIS, RVLALIQUOTAFAIXADOIS, RVLDEDUCAOFAIXADOIS,"
            + " RVLINICIALFAIXATRES, RVLFINALFAIXATRES, RVLALIQUOTAFAIXATRES, RVLDEDUCAOFAIXATRES,"
            + " RVLINICIALFAIXAQUATRO, RVLFINALFAIXAQUATRO, RVLALIQUOTAFAIXAQUATRO, RVLDEDUCAOFAIXAQUATRO,"
            + " RVLINICIALFAIXACINCO, RVLFINALFAIXACINCO, RVLALIQUOTAFAIXACINCO, RVLDEDUCAOFAIXACINCO,"
            + " RVLDEDUCAOPORDEPENDENTE, RVLDEDUCAOAPOSENTADOMEIACINCO"
            + " FROM TBTABELAIMPOSTORENDA WHERE DDTCOMPETENCIAREGISTRO = ?";
        try (PreparedStatement ps = cn.prepareStatement(sql)) {
            ps.setDate(1, new java.sql.Date(d(competencia).getTime()));
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalStateException("sem tabela IRPF para " + competencia);
                TabelaIrpf t = new TabelaIrpf();
                t.setCompetencia(d(competencia));
                t.setPrimeiraFaixaFiscal(new PrimeiraFaixaFiscal(rs.getBigDecimal(1), rs.getBigDecimal(2), rs.getBigDecimal(3), rs.getBigDecimal(4)));
                t.setSegundaFaixaFiscal(new SegundaFaixaFiscal(rs.getBigDecimal(5), rs.getBigDecimal(6), rs.getBigDecimal(7), rs.getBigDecimal(8)));
                t.setTerceiraFaixaFiscal(new TerceiraFaixaFiscal(rs.getBigDecimal(9), rs.getBigDecimal(10), rs.getBigDecimal(11), rs.getBigDecimal(12)));
                t.setQuartaFaixaFiscal(new QuartaFaixaFiscal(rs.getBigDecimal(13), rs.getBigDecimal(14), rs.getBigDecimal(15), rs.getBigDecimal(16)));
                t.setQuintaFaixaFiscal(new QuintaFaixaFiscal(rs.getBigDecimal(17), rs.getBigDecimal(18), rs.getBigDecimal(19), rs.getBigDecimal(20)));
                t.setValorDeducaoPorDependente(rs.getBigDecimal(21));
                t.setValorDeducaoParaAposentadoMaiorQue65Anos(rs.getBigDecimal(22));
                return t;
            }
        }
    }

    static Irpf montarIrpf(CalcStub calc) {
        Irpf irpf = new Irpf();
        irpf.setCalculo(calc);
        irpf.setApurarImpostoRenda(Boolean.TRUE);
        irpf.setRegimeDeCaixa(Boolean.FALSE);
        irpf.setIncidirSobreJurosDeMora(Boolean.FALSE);
        irpf.setConsiderarTributacaoEmSeparado(Boolean.TRUE);
        irpf.setConsiderarTributacaoExclusiva(Boolean.TRUE);
        irpf.setDeduzirContribuicaoSocialDevidaPeloReclamante(Boolean.FALSE);
        irpf.setDeduzirPrevidenciaPrivada(Boolean.FALSE);
        irpf.setDeduzirPensaoAlimenticia(Boolean.FALSE);
        irpf.setDeduzirHonorariosDevidosPeloReclamante(Boolean.FALSE);
        irpf.setPossuiDependentes(Boolean.FALSE);
        irpf.setAposentadoMaiorQue65Anos(Boolean.FALSE);
        return irpf;
    }

    static void emitir(String cenario, Irpf irpf) {
        List<OcorrenciaDeIrpf> ocorrencias = new ArrayList<OcorrenciaDeIrpf>(irpf.getOcorrencias());
        row(cenario, "numOcorrencias", new BigDecimal(ocorrencias.size()));
        for (OcorrenciaDeIrpf o : ocorrencias) {
            String t = o.getTipo().name();
            row(cenario, t + ".base", o.getValorBase());
            row(cenario, t + ".aliquota", o.getValorAliquota());
            row(cenario, t + ".deducao", o.getValorDeducao());
            row(cenario, t + ".devido", o.getValorDevido());
            row(cenario, t + ".nm", o.getQuantidadeCompetencias() == null ? BigDecimal.ONE : new BigDecimal(o.getQuantidadeCompetencias()));
            row(cenario, t + ".faixaInicial", o.getValorInicialFaixa());
            row(cenario, t + ".faixaFinal", o.getValorFinalFaixa());
        }
    }

    static void row(String c, String k, BigDecimal v) {
        System.out.println(c + ";" + k + ";" + p(v));
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());

        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/")) {
            TabelaIrpf tabela = carregar(cn, "2024-02-01");
            Utils.adicionarRepositorioParaTeste(RepositorioDeTabelaIrpf.class, new RepoIrpfStub(tabela));

            // A1: regime de caixa, 3 tipos (ferias em separado, 13o exclusivo, demais normal), sem juros/deducoes.
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.FERIAS, "2024-01-05", "5000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO, "2024-01-05", "3000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2024-01-05", "4000.00"));
                Irpf irpf = montarIrpf(calc);
                irpf.setRegimeDeCaixa(Boolean.TRUE);
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("A1_CAIXA_3TIPOS", irpf);
            }
            // A2: regime de caixa, separado e exclusiva DESLIGADOS -> tudo cai em NORMAL.
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.FERIAS, "2024-01-05", "5000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO, "2024-01-05", "3000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2024-01-05", "4000.00"));
                Irpf irpf = montarIrpf(calc);
                irpf.setRegimeDeCaixa(Boolean.TRUE);
                irpf.setConsiderarTributacaoEmSeparado(Boolean.FALSE);
                irpf.setConsiderarTributacaoExclusiva(Boolean.FALSE);
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("A2_CAIXA_SO_NORMAL", irpf);
            }
            // A3: regime de caixa, incidencia sobre juros + dependentes (2) na base.
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.jurosDemais = b("500.00");
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2024-01-05", "4000.00"));
                Irpf irpf = montarIrpf(calc);
                irpf.setRegimeDeCaixa(Boolean.TRUE);
                irpf.setIncidirSobreJurosDeMora(Boolean.TRUE);
                irpf.setPossuiDependentes(Boolean.TRUE);
                irpf.setQuantidadeDependentes(Integer.valueOf(2));
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("A3_CAIXA_JUROS_DEP", irpf);
            }
            // B1: regime de competencia (liq 2024), verbas de anos anteriores (2022/2023, 3 competencias)
            //     -> RRA com NM=3, mais uma verba corrente (NORMAL).
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2022-05-01", "20000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2023-03-01", "20000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2023-07-01", "20000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2024-02-01", "4000.00"));
                Irpf irpf = montarIrpf(calc);
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("B1_RRA_NM3", irpf);
            }
            // B2: regime de competencia, RRA de anos anteriores com 13o (competencias distintas
            //     contam 13o separadamente) + aposentado>65 na base (x NM no RRA).
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2022-05-01", "15000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO, "2022-12-01", "10000.00"));
                Irpf irpf = montarIrpf(calc);
                irpf.setAposentadoMaiorQue65Anos(Boolean.TRUE);
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("B2_RRA_13_APOS", irpf);
            }
            // B3: regime de competencia, so verbas correntes (nenhuma anterior a 2024-01-01) -> sem RRA.
            {
                CalcStub calc = new CalcStub(d("2024-02-10"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.FERIAS, "2024-01-05", "5000.00"));
                calc.ativas.add(verba(calc, CaracteristicaDaVerbaEnum.COMUM, "2024-02-01", "4000.00"));
                Irpf irpf = montarIrpf(calc);
                new MaquinaDeCalculoDeIrpf(irpf).liquidar();
                emitir("B3_COMPETENCIA_CORRENTE", irpf);
            }
        }
    }
}
