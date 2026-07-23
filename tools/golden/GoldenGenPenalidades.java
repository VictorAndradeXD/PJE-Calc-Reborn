import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaApuracaoDeHonorarioEnum;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaApuracaoDeMultaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaCustasCalculadasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CredorDevedorMultaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoCobrancaReclamanteEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCustasDeConhecimentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCustasDeLiquidacaoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeDevedorDoHonorarioEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeImpostoDeRendaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoValorEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.atualizacao.ParametrosDeAtualizacao;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.Armazenamento;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.AutoJudicial;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.CustaPaga;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.CustasJudiciais;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.MaquinaDeCalculoDeCustas;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.salariofamilia.SalarioFamilia;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.SeguroDesemprego;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.honorarios.Honorario;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.honorarios.MaquinaDeCalculoDeHonorarios;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.honorarios.TotalizadorDeHonorario;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.multa.MaquinaDeCalculoDeMulta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.multa.Multa;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.multa.TotalizadorDeMulta;
import br.jus.trt8.pjecalc.negocio.dominio.custas.ParametrosDeCustasFixas;
import br.jus.trt8.pjecalc.negocio.dominio.custas.RepositorioDeParametrosDeCustasFixas;
import br.jus.trt8.pjecalc.negocio.dominio.inss.seguradoempregado.RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado;
import br.jus.trt8.pjecalc.negocio.dominio.inss.seguradoempregado.TabelaPrevidenciariaSeguradoEmpregado;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.RepositorioDeTabelaIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.TabelaIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.PrimeiraFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.SegundaFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.TerceiraFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.QuartaFaixaFiscal;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.QuintaFaixaFiscal;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.LinkedHashSet;
import java.util.Set;

/**
 * Golden de Multas/Indenizacoes, Honorarios e Custas dirigindo o motor oficial.
 * As bases agregadas (bruto devido ao reclamante, principal corrigido + juros) sao
 * injetadas por uma subclasse de Calculo — o que se valida aqui e a logica NOVA:
 * determinacao da base, percentual, sequencia de arredondamento, IRRF, piso/teto das
 * custas (dados reais) e totalizacao por credor/devedor.
 * Saida: linhas "cenario;chave;valor".
 */
public class GoldenGenPenalidades {

    static Date d(int ano, int mes, int dia) {
        return new GregorianCalendar(ano, mes - 1, dia).getTime();
    }

    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }

    static void row(String cenario, String chave, BigDecimal valor) {
        System.out.println(cenario + ";" + chave + ";" + p(valor));
    }

    // Calculo com os agregados fixados; o resto vem do motor real.
    static class CalcStub extends Calculo {
        Date liq, ajz;
        BigDecimal corrigido = BigDecimal.ZERO;
        BigDecimal jurosMora = BigDecimal.ZERO;
        BigDecimal bruto = BigDecimal.ZERO;
        Set<Multa> multas = new LinkedHashSet<Multa>();
        Set<Honorario> honorarios = new LinkedHashSet<Honorario>();
        CustasJudiciais custas;
        ParametrosDeAtualizacao pa;
        Fgts fgtsStub = new Fgts() {
            @Override public boolean isComporOPrincipal() { return false; }
        };
        SalarioFamilia salarioFamiliaStub = new SalarioFamilia();
        SeguroDesemprego seguroDesempregoStub = new SeguroDesemprego();

        CalcStub(Date liq, Date ajz) {
            this.liq = liq;
            this.ajz = ajz;
            this.pa = new ParametrosDeAtualizacao(this);
            this.pa.setCorrecaoDasCustas(Boolean.FALSE);
        }

        @Override public Fgts getFgts() { return fgtsStub; }
        @Override public SalarioFamilia getSalarioFamilia() { return salarioFamiliaStub; }
        @Override public SeguroDesemprego getSeguroDesemprego() { return seguroDesempregoStub; }

        @Override public Date getDataDeLiquidacao() { return liq; }
        @Override public Date getDataAjuizamento() { return ajz; }
        @Override public BigDecimal getTotalDeValorCorrigidoDaApuracaoDeJuros() { return corrigido; }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJuros() { return jurosMora; }
        @Override public BigDecimal calcularBrutoDevidoAoReclamante() { return bruto; }
        @Override public Set<Multa> getMultasDoCalculo() { return multas; }
        @Override public Set<Honorario> getHonorariosDoCalculo() { return honorarios; }
        @Override public CustasJudiciais getCustasJudiciais() { return custas; }
        @Override public ParametrosDeAtualizacao getParametrosDeAtualizacao() { return pa; }
        @Override public Boolean isCalculoExterno() { return Boolean.FALSE; }
    }

    // ---- Repositorios estubados (piso/teto de custas + teto RGPS) ----

    static ParametrosDeCustasFixas novoParametro() {
        ParametrosDeCustasFixas x = new ParametrosDeCustasFixas();
        x.setDataInicio(d(2002, 9, 27));
        x.setDataFim(null);
        x.setValorPisoCustasConhecimento(new BigDecimal("10.64"));
        x.setValorTetoCustasLiquidacao(new BigDecimal("638.46"));
        x.setValorTetoCustasDeAutos(new BigDecimal("1915.38"));
        x.setValorAtosUrbanosOficialJustica(new BigDecimal("11.06"));
        x.setValorAtosRuraisOficialJustica(new BigDecimal("22.13"));
        x.setValorAgravoDeInstrumento(new BigDecimal("44.26"));
        x.setValorAgravoDePeticao(new BigDecimal("44.26"));
        x.setValorImpugnacaoSentencaDeLiquidacao(new BigDecimal("55.35"));
        x.setValorEmbargosAArrematacao(new BigDecimal("44.26"));
        x.setValorEmbargosAExecucao(new BigDecimal("44.26"));
        x.setValorEmbargosDeTerceiros(new BigDecimal("44.26"));
        x.setValorRecursoDeRevista(new BigDecimal("55.35"));
        return x;
    }

    static class RepoParametroStub extends RepositorioDeParametrosDeCustasFixas {
        @Override public ParametrosDeCustasFixas obterPorData(Date data) { return novoParametro(); }
        @Override public ParametrosDeCustasFixas obterRegistroMaisAntigo() { return novoParametro(); }
    }

    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";

    // Carrega a tabela do IRPF de uma competencia (para o IRRF de pessoa fisica).
    static TabelaIrpf carregarTabelaIrpf(String competencia) throws Exception {
        Class.forName("org.h2.Driver");
        String sql = "SELECT RVLINICIALFAIXAUM, RVLFINALFAIXAUM, RVLALIQUOTAFAIXAUM, RVLDEDUCAOFAIXAUM,"
            + " RVLINICIALFAIXADOIS, RVLFINALFAIXADOIS, RVLALIQUOTAFAIXADOIS, RVLDEDUCAOFAIXADOIS,"
            + " RVLINICIALFAIXATRES, RVLFINALFAIXATRES, RVLALIQUOTAFAIXATRES, RVLDEDUCAOFAIXATRES,"
            + " RVLINICIALFAIXAQUATRO, RVLFINALFAIXAQUATRO, RVLALIQUOTAFAIXAQUATRO, RVLDEDUCAOFAIXAQUATRO,"
            + " RVLINICIALFAIXACINCO, RVLFINALFAIXACINCO, RVLALIQUOTAFAIXACINCO, RVLDEDUCAOFAIXACINCO"
            + " FROM TBTABELAIMPOSTORENDA WHERE DDTCOMPETENCIAREGISTRO = ?";
        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/");
             PreparedStatement ps = cn.prepareStatement(sql)) {
            java.text.SimpleDateFormat iso = new java.text.SimpleDateFormat("yyyy-MM-dd");
            ps.setDate(1, new java.sql.Date(iso.parse(competencia).getTime()));
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalStateException("sem tabela IRPF para " + competencia);
                TabelaIrpf t = new TabelaIrpf();
                t.setCompetencia(iso.parse(competencia));
                t.setPrimeiraFaixaFiscal(new PrimeiraFaixaFiscal(rs.getBigDecimal(1), rs.getBigDecimal(2), rs.getBigDecimal(3), rs.getBigDecimal(4)));
                t.setSegundaFaixaFiscal(new SegundaFaixaFiscal(rs.getBigDecimal(5), rs.getBigDecimal(6), rs.getBigDecimal(7), rs.getBigDecimal(8)));
                t.setTerceiraFaixaFiscal(new TerceiraFaixaFiscal(rs.getBigDecimal(9), rs.getBigDecimal(10), rs.getBigDecimal(11), rs.getBigDecimal(12)));
                t.setQuartaFaixaFiscal(new QuartaFaixaFiscal(rs.getBigDecimal(13), rs.getBigDecimal(14), rs.getBigDecimal(15), rs.getBigDecimal(16)));
                t.setQuintaFaixaFiscal(new QuintaFaixaFiscal(rs.getBigDecimal(17), rs.getBigDecimal(18), rs.getBigDecimal(19), rs.getBigDecimal(20)));
                return t;
            }
        }
    }

    static class RepoIrpfStub extends RepositorioDeTabelaIrpf {
        TabelaIrpf tabela;
        RepoIrpfStub(TabelaIrpf t) { this.tabela = t; }
        @Override public TabelaIrpf obterParaA(Date data) { return tabela; }
    }

    static class RepoTetoStub extends RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado {
        // Teto RGPS 01/2021 = 6433.57  (4x = 25734.28, teto de custas de conhecimento pos-reforma).
        @Override public TabelaPrevidenciariaSeguradoEmpregado obter(Date competencia) {
            return new TabelaPrevidenciariaSeguradoEmpregado() {
                @Override public BigDecimal getValorTetoBeneficio() { return new BigDecimal("6433.57"); }
            };
        }
        @Override public TabelaPrevidenciariaSeguradoEmpregado obterAtual() {
            return obter(null);
        }
    }

    // ---------------------------------------------------------------

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeParametrosDeCustasFixas.class, new RepoParametroStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado.class, new RepoTetoStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeTabelaIrpf.class, new RepoIrpfStub(carregarTabelaIrpf("2021-06-01")));

        multas();
        honorarios();
        custas();
    }

    // ===================== MULTAS =====================

    static Multa novaMulta(Calculo c) {
        Multa m = new Multa();
        m.setCalculo(c);
        m.setTipoCredorDevedor(CredorDevedorMultaEnum.RECLAMANTE_RECLAMADO);
        return m;
    }

    static void multas() {
        // M1: CALCULADO / PRINCIPAL, arredondamento em getValorCorrigido.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.corrigido = new BigDecimal("1234.56");
            c.jurosMora = new BigDecimal("0");
            Multa m = novaMulta(c);
            m.setTipoValorDaMulta(TipoValorEnum.CALCULADO);
            m.setTipoBaseMulta(BaseParaApuracaoDeMultaEnum.PRINCIPAL);
            m.setAliquotaMulta(new BigDecimal("7.5"));
            new MaquinaDeCalculoDeMulta(m).liquidar();
            row("M1_PRINCIPAL", "base", m.getBaseMulta());
            row("M1_PRINCIPAL", "valorCorrigido", m.getValorCorrigido());
            row("M1_PRINCIPAL", "valorTotal", m.getValorTotal());
        }
        // M2: CALCULADO / PRINCIPAL com principal = corrigido + juros de mora.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.corrigido = new BigDecimal("10000.00");
            c.jurosMora = new BigDecimal("1500.00");
            Multa m = novaMulta(c);
            m.setTipoValorDaMulta(TipoValorEnum.CALCULADO);
            m.setTipoBaseMulta(BaseParaApuracaoDeMultaEnum.PRINCIPAL);
            m.setAliquotaMulta(new BigDecimal("40"));
            new MaquinaDeCalculoDeMulta(m).liquidar();
            row("M2_PRINCIPAL", "base", m.getBaseMulta());
            row("M2_PRINCIPAL", "valorCorrigido", m.getValorCorrigido());
            row("M2_PRINCIPAL", "valorTotal", m.getValorTotal());
        }
        // M3: INFORMADO, vencimento = liquidacao (indice 1) + juros da multa.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Multa m = novaMulta(c);
            m.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            m.setValorMulta(new BigDecimal("2500.00"));
            m.setDataVencimentoMulta(d(2021, 6, 10));
            m.setTaxaJurosMulta(new BigDecimal("1.0"));
            new MaquinaDeCalculoDeMulta(m).liquidar();
            row("M3_INFORMADO", "indice", m.getIndiceCorrecaoMulta());
            row("M3_INFORMADO", "valorCorrigido", m.getValorCorrigido());
            row("M3_INFORMADO", "juros", m.getJuros());
            row("M3_INFORMADO", "valorTotal", m.getValorTotal());
        }
        // M4: INFORMADO com juros que geram fracao (total nao arredondado).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Multa m = novaMulta(c);
            m.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            m.setValorMulta(new BigDecimal("333.33"));
            m.setDataVencimentoMulta(d(2021, 6, 10));
            m.setTaxaJurosMulta(new BigDecimal("8.33"));
            new MaquinaDeCalculoDeMulta(m).liquidar();
            row("M4_INFORMADO", "valorCorrigido", m.getValorCorrigido());
            row("M4_INFORMADO", "juros", m.getJuros());
            row("M4_INFORMADO", "valorTotal", m.getValorTotal());
        }
        // MT: totalizador por credor/devedor (arredonda cada parcela antes de somar).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Multa a = novaMulta(c); // RTRD
            a.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            a.setValorMulta(new BigDecimal("92.59"));
            a.setDataVencimentoMulta(d(2021, 6, 10));
            new MaquinaDeCalculoDeMulta(a).liquidar();
            Multa b = novaMulta(c); // RTRD segunda
            b.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            b.setValorMulta(new BigDecimal("10.125"));
            b.setDataVencimentoMulta(d(2021, 6, 10));
            new MaquinaDeCalculoDeMulta(b).liquidar();
            Multa cc = novaMulta(c); // RDRT
            cc.setTipoCredorDevedor(CredorDevedorMultaEnum.RECLAMADO_RECLAMANTE);
            cc.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            cc.setValorMulta(new BigDecimal("50.00"));
            cc.setDataVencimentoMulta(d(2021, 6, 10));
            new MaquinaDeCalculoDeMulta(cc).liquidar();
            Multa e = novaMulta(c); // TRD
            e.setTipoCredorDevedor(CredorDevedorMultaEnum.TERCEIRO_RECLAMADO);
            e.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
            e.setValorMulta(new BigDecimal("333.33"));
            e.setDataVencimentoMulta(d(2021, 6, 10));
            e.setTaxaJurosMulta(new BigDecimal("8.33"));
            new MaquinaDeCalculoDeMulta(e).liquidar();
            c.multas.add(a); c.multas.add(b); c.multas.add(cc); c.multas.add(e);
            TotalizadorDeMulta t = new TotalizadorDeMulta(c);
            row("MT_TOTALIZADOR", "reclamanteReclamado", t.getTotalTipoReclamanteReclamado());
            row("MT_TOTALIZADOR", "reclamadoReclamante", t.getTotalTipoReclamadoReclamante());
            row("MT_TOTALIZADOR", "terceiroReclamado", t.getTotalTipoTerceiroReclamado());
        }
    }

    // ===================== HONORARIOS =====================

    static Honorario novoHonorario(Calculo c) {
        Honorario h = new Honorario();
        h.setCalculo(c);
        h.setApurarIRRF(Boolean.FALSE);
        h.setApurarIRPFSobreJuros(Boolean.FALSE);
        h.setTipoDeDevedor(TipoDeDevedorDoHonorarioEnum.RECLAMADO);
        return h;
    }

    static void honorarios() {
        // H1: CALCULADO / BRUTO, sem IRRF.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.bruto = new BigDecimal("10000.00");
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.CALCULADO);
            h.setBaseParaApuracao(BaseParaApuracaoDeHonorarioEnum.BRUTO);
            h.setAliquota(new BigDecimal("10"));
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H1_BRUTO", "base", h.getBaseHonorario());
            row("H1_BRUTO", "valorCorrigido", h.getValorCorrigido());
            row("H1_BRUTO", "valorTotal", h.getValorTotal());
            row("H1_BRUTO", "imposto", h.getValorImpostoRenda());
        }
        // H2: CALCULADO / BRUTO + IRRF PJ 1,50%.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.bruto = new BigDecimal("50000.00");
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.CALCULADO);
            h.setBaseParaApuracao(BaseParaApuracaoDeHonorarioEnum.BRUTO);
            h.setAliquota(new BigDecimal("10"));
            h.setApurarIRRF(Boolean.TRUE);
            h.setTipoImpostoRenda(TipoDeImpostoDeRendaEnum.PESSOA_JURIDICA);
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H2_BRUTO_PJ", "valorCorrigido", h.getValorCorrigido());
            row("H2_BRUTO_PJ", "imposto", h.getValorImpostoRenda());
        }
        // H3: CALCULADO / BRUTO + IRRF PJ, base com fracao.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.bruto = new BigDecimal("1234.56");
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.CALCULADO);
            h.setBaseParaApuracao(BaseParaApuracaoDeHonorarioEnum.BRUTO);
            h.setAliquota(new BigDecimal("7.5"));
            h.setApurarIRRF(Boolean.TRUE);
            h.setTipoImpostoRenda(TipoDeImpostoDeRendaEnum.PESSOA_JURIDICA);
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H3_BRUTO_PJ", "valorCorrigido", h.getValorCorrigido());
            row("H3_BRUTO_PJ", "imposto", h.getValorImpostoRenda());
        }
        // H4: INFORMADO + juros + IRRF PJ sobre juros.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.INFORMADO);
            h.setValor(new BigDecimal("8000.00"));
            h.setDataVencimento(d(2021, 6, 10));
            h.setTaxaJurosHonorario(new BigDecimal("1.0"));
            h.setApurarIRRF(Boolean.TRUE);
            h.setApurarIRPFSobreJuros(Boolean.TRUE);
            h.setTipoImpostoRenda(TipoDeImpostoDeRendaEnum.PESSOA_JURIDICA);
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H4_INFORMADO", "valorCorrigido", h.getValorCorrigido());
            row("H4_INFORMADO", "juros", h.getJuros());
            row("H4_INFORMADO", "valorTotal", h.getValorTotal());
            row("H4_INFORMADO", "imposto", h.getValorImpostoRenda());
        }
        // H5: CALCULADO / BRUTO + IRRF PF (tabela real de 06/2021, faixa progressiva).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            c.bruto = new BigDecimal("30000.00");
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.CALCULADO);
            h.setBaseParaApuracao(BaseParaApuracaoDeHonorarioEnum.BRUTO);
            h.setAliquota(new BigDecimal("10"));
            h.setApurarIRRF(Boolean.TRUE);
            h.setTipoImpostoRenda(TipoDeImpostoDeRendaEnum.PESSOA_FISICA);
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H5_BRUTO_PF", "valorCorrigido", h.getValorCorrigido());
            row("H5_BRUTO_PF", "aliquotaIrpf", h.getValorAliquotaIrpf());
            row("H5_BRUTO_PF", "imposto", h.getValorImpostoRenda());
        }
        // H6: INFORMADO + IRRF PF sobre valor corrigido (base em faixa superior).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Honorario h = novoHonorario(c);
            h.setTipoValor(TipoValorEnum.INFORMADO);
            h.setValor(new BigDecimal("6000.00"));
            h.setDataVencimento(d(2021, 6, 10));
            h.setApurarIRRF(Boolean.TRUE);
            h.setTipoImpostoRenda(TipoDeImpostoDeRendaEnum.PESSOA_FISICA);
            new MaquinaDeCalculoDeHonorarios(h).liquidar();
            row("H6_INFORMADO_PF", "valorCorrigido", h.getValorCorrigido());
            row("H6_INFORMADO_PF", "imposto", h.getValorImpostoRenda());
        }
        // HT: totalizador — reclamado soma; reclamante COBRAR=0 vs DESCONTAR=valor.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            Honorario a = novoHonorario(c); // RECLAMADO
            a.setTipoValor(TipoValorEnum.INFORMADO);
            a.setValor(new BigDecimal("1000.00"));
            a.setDataVencimento(d(2021, 6, 10));
            new MaquinaDeCalculoDeHonorarios(a).liquidar();
            Honorario b = novoHonorario(c); // RECLAMANTE COBRAR -> 0
            b.setTipoDeDevedor(TipoDeDevedorDoHonorarioEnum.RECLAMANTE);
            b.setTipoCobrancaReclamante(TipoCobrancaReclamanteEnum.COBRAR);
            b.setTipoValor(TipoValorEnum.INFORMADO);
            b.setValor(new BigDecimal("500.00"));
            b.setDataVencimento(d(2021, 6, 10));
            new MaquinaDeCalculoDeHonorarios(b).liquidar();
            Honorario e = novoHonorario(c); // RECLAMANTE DESCONTAR -> valor
            e.setTipoDeDevedor(TipoDeDevedorDoHonorarioEnum.RECLAMANTE);
            e.setTipoCobrancaReclamante(TipoCobrancaReclamanteEnum.DESCONTAR_CREDITO);
            e.setTipoValor(TipoValorEnum.INFORMADO);
            e.setValor(new BigDecimal("300.00"));
            e.setDataVencimento(d(2021, 6, 10));
            new MaquinaDeCalculoDeHonorarios(e).liquidar();
            c.honorarios.add(a); c.honorarios.add(b); c.honorarios.add(e);
            TotalizadorDeHonorario t = new TotalizadorDeHonorario(c);
            row("HT_TOTALIZADOR", "devidoPeloReclamante", t.getTotalDevidoPeloReclamante());
            row("HT_TOTALIZADOR", "devidoPeloReclamado", t.getTotalDevidoPeloReclamado());
        }
    }

    // ===================== CUSTAS =====================

    static CustasJudiciais novaCustas(CalcStub c, Date ajz) {
        CustasJudiciais cj = new CustasJudiciais();
        cj.setCalculo(c);
        cj.setBaseParaCustasCalculadas(BaseParaCustasCalculadasEnum.BRUTO_DEVIDO_AO_RECLAMANTE);
        cj.setTipoDeCustasDeConhecimentoDoReclamante(TipoDeCustasDeConhecimentoEnum.NAO_SE_APLICA);
        cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.NAO_SE_APLICA);
        cj.setTipoDeCustasDeLiquidacao(TipoDeCustasDeLiquidacaoEnum.NAO_SE_APLICA);
        c.custas = cj;
        return cj;
    }

    static void liquidarCustas(CustasJudiciais cj) {
        new MaquinaDeCalculoDeCustas(cj).liquidar();
    }

    static void custas() {
        // C1: conhecimento reclamado 2%, base grande (sem piso, ajuizamento pre-reforma -> sem teto).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("100000.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            liquidarCustas(cj);
            row("C1_CONHEC_2PC", "base", cj.getValorBaseCustasCalculadas());
            row("C1_CONHEC_2PC", "totalConhecimento", cj.getTotalCustasConhecimentoReclamadoCalculada());
            row("C1_CONHEC_2PC", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C2: conhecimento reclamado 2%, base pequena -> piso 10.64.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("100.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            liquidarCustas(cj);
            row("C2_CONHEC_PISO", "totalConhecimento", cj.getTotalCustasConhecimentoReclamadoCalculada());
            row("C2_CONHEC_PISO", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C3: conhecimento reclamado 2% + teto (ajuizamento 11/2017+, liq 2021 -> 4x6433.57=25734.28).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2019, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2019, 1, 1));
            c.bruto = new BigDecimal("2000000.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            liquidarCustas(cj);
            row("C3_CONHEC_TETO", "teto", cj.getTetoCustasConhecimentoReclamado());
            row("C3_CONHEC_TETO", "totalConhecimento", cj.getTotalCustasConhecimentoReclamadoCalculada());
        }
        // C4: liquidacao 0,5% + teto 638.46.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("200000.00");
            cj.setTipoDeCustasDeLiquidacao(TipoDeCustasDeLiquidacaoEnum.CALCULADA_MEIO_POR_CENTO);
            liquidarCustas(cj);
            row("C4_LIQ_TETO", "totalLiquidacao", cj.getTotalCustasLiquidacaoReclamadoCalculada());
            row("C4_LIQ_TETO", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C4b: liquidacao 0,5% sem teto (base menor).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("50000.00");
            cj.setTipoDeCustasDeLiquidacao(TipoDeCustasDeLiquidacaoEnum.CALCULADA_MEIO_POR_CENTO);
            liquidarCustas(cj);
            row("C4b_LIQ", "totalLiquidacao", cj.getTotalCustasLiquidacaoReclamadoCalculada());
        }
        // C5: auto judicial 5% + teto 1915.38.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("0.00");
            AutoJudicial auto = new AutoJudicial();
            auto.setCustasJudiciais(cj);
            auto.setValorAvaliacaoAuto(new BigDecimal("100000.00"));
            auto.setDataVencimentoAuto(d(2021, 6, 10));
            Set<AutoJudicial> autos = new LinkedHashSet<AutoJudicial>();
            autos.add(auto);
            cj.setAutosJudiciais(autos);
            liquidarCustas(cj);
            row("C5_AUTO_TETO", "totalAuto", auto.getTotal());
            row("C5_AUTO_TETO", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C5b: auto 5% sem teto.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            AutoJudicial auto = new AutoJudicial();
            auto.setCustasJudiciais(cj);
            auto.setValorAvaliacaoAuto(new BigDecimal("10000.00"));
            auto.setDataVencimentoAuto(d(2021, 6, 10));
            Set<AutoJudicial> autos = new LinkedHashSet<AutoJudicial>();
            autos.add(auto);
            cj.setAutosJudiciais(autos);
            liquidarCustas(cj);
            row("C5b_AUTO", "totalAuto", auto.getTotal());
        }
        // C6: armazenamento 0,1%/dia.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            Armazenamento arm = new Armazenamento();
            arm.setCustasJudiciais(cj);
            arm.setValorAvaliacaoArmazenamento(new BigDecimal("50000.00"));
            arm.setDataInicioArmazenamento(d(2021, 1, 1));
            arm.setDataTerminoArmazenamento(d(2021, 1, 31));
            Set<Armazenamento> arms = new LinkedHashSet<Armazenamento>();
            arms.add(arm);
            cj.setArmazenamentos(arms);
            liquidarCustas(cj);
            row("C6_ARMAZENAMENTO", "qtdeDias", new BigDecimal(arm.getQtdeDias()));
            row("C6_ARMAZENAMENTO", "totalArmazenamento", arm.getTotal());
        }
        // C7: custas fixas (atos urbanos x2 + recurso de revista x1).
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            cj.setDataVencimentoCustasFixas(d(2021, 6, 10));
            cj.setQtdeAtosUrbanos(Integer.valueOf(2));
            cj.setQtdeRecursoRevista(Integer.valueOf(1));
            liquidarCustas(cj);
            row("C7_FIXAS", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C8: consolidado com custa paga do reclamado abatida.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("100000.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            CustaPaga paga = new CustaPaga();
            paga.setValor(new BigDecimal("500.00"));
            paga.setDataVencimento(d(2021, 6, 10));
            cj.getCustasPagasDoReclamado().add(paga);
            liquidarCustas(cj);
            row("C8_PAGAS", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C8b: pagas superam custas -> piso 0.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("1000.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            CustaPaga paga = new CustaPaga();
            paga.setValor(new BigDecimal("5000.00"));
            paga.setDataVencimento(d(2021, 6, 10));
            cj.getCustasPagasDoReclamado().add(paga);
            liquidarCustas(cj);
            row("C8b_PISO0", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
        // C9: combinado conhecimento 2% + liquidacao 0,5% + fixas - paga.
        {
            CalcStub c = new CalcStub(d(2021, 6, 10), d(2015, 1, 1));
            CustasJudiciais cj = novaCustas(c, d(2015, 1, 1));
            c.bruto = new BigDecimal("80000.00");
            cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
            cj.setTipoDeCustasDeLiquidacao(TipoDeCustasDeLiquidacaoEnum.CALCULADA_MEIO_POR_CENTO);
            cj.setDataVencimentoCustasFixas(d(2021, 6, 10));
            cj.setQtdeAgravosDePeticao(Integer.valueOf(1));
            CustaPaga paga = new CustaPaga();
            paga.setValor(new BigDecimal("100.00"));
            paga.setDataVencimento(d(2021, 6, 10));
            cj.getCustasPagasDoReclamado().add(paga);
            liquidarCustas(cj);
            row("C9_COMBINADO", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
        }
    }
}
