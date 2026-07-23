import br.jus.trt8.pjecalc.base.comum.HelperDate;
import br.jus.trt8.pjecalc.base.comum.Periodo;
import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculoDoPrazoDeFerias;
import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculoDoSalarioEmFerias;
import br.jus.trt8.pjecalc.negocio.constantes.BaseDeCalculoDoPrincipalEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.ComportamentoDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.DivisorDeVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.OcorrenciaDePagamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.PeriodoDaMediaDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.RegimeDoContratoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.SituacaoDaFeriasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeGeracaoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoValorPagoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TratamentoDaFracaoDeMesDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.RepositorioDeCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.faltas.Falta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.faltas.RepositorioDeFalta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.ferias.Ferias;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaInformada;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.termo.BaseTabelada;
import br.jus.trt8.pjecalc.negocio.dominio.termo.ItemBaseVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Calculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaInformada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Reflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.TabelaDeCorrecaoMonetaria;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;
import br.jus.trt8.pjecalc.negocio.servicos.ServicoDeCalculo;

import java.math.BigDecimal;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * Golden da etapa de FÉRIAS: tabela do art. 130 (prazo), breakInYears, pipeline
 * PERIODO_AQUISITIVO (gozos/dobra/saldo/indenizadas/fracionário/abono/prescrição),
 * faltas nos provedores, salário em férias e reflexos com destino férias.
 * Índice estubado: fator(aaaa-mm) = 1 + 0.01*((ano-2019)*12 + mes).
 */
public class GoldenGenFeriasPipeline {

    // ---------- stubs (mesma receita do GoldenGenVerbasPipeline) ----------

    static class RepoCalculoStub extends RepositorioDeCalculo {
        Calculo instancia;
        RepoCalculoStub(Calculo c) { this.instancia = c; }
        @Override public Calculo obter(Object id) { return instancia; }
    }

    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void limparOcorrencias(VerbaDeCalculo verba, boolean flush) {
            verba.getOcorrencias().clear();
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    static class RepoFaltaStub extends RepositorioDeFalta {
        @Override public List<Falta> obterTodosPor(Calculo calculo) {
            return new java.util.ArrayList<Falta>(calculo.getFaltas());
        }
    }

    static class ServicoStub extends ServicoDeCalculo {
        Calculo calculo;
        ServicoStub(Calculo c) { this.calculo = c; }
        @Override public Calculo obterCalculoAberto() { return calculo; }
        @Override public Set<Ferias> obterFeriasDoCalculo() {
            return calculo.getListaDeFerias() != null ? calculo.getListaDeFerias() : new HashSet<Ferias>();
        }
    }

    static class TabelaStub extends TabelaDeCorrecaoMonetaria {
        @Override public BigDecimal obterValorAcumuladoDoIndice(Date data) {
            return indiceDaCompetencia(data);
        }
    }

    static class MaqCalculada extends MaquinaDeCalculoDaVerbaCalculada {
        ServicoDeCalculo serv;
        MaqCalculada(Calculada v, ServicoDeCalculo s) { super(v); this.serv = s; }
        @Override protected ServicoDeCalculo getServicoDeCalculo() { return serv; }
    }

    static class MaqInformada extends MaquinaDeCalculoDaVerbaInformada {
        ServicoDeCalculo serv;
        MaqInformada(Informada v, ServicoDeCalculo s) { super(v); this.serv = s; }
        @Override protected ServicoDeCalculo getServicoDeCalculo() { return serv; }
    }

    static class MaqReflexo extends MaquinaDeCalculoDaVerbaReflexo {
        ServicoDeCalculo serv;
        MaqReflexo(Reflexo v, ServicoDeCalculo s) { super(v); this.serv = s; }
        @Override protected ServicoDeCalculo getServicoDeCalculo() { return serv; }
    }

    // ---------- helpers ----------

    static Date d(int ano, int mes, int dia) {
        return new GregorianCalendar(ano, mes - 1, dia).getTime();
    }

    static BigDecimal indiceDaCompetencia(Date data) {
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        int k = (c.get(GregorianCalendar.YEAR) - 2019) * 12 + (c.get(GregorianCalendar.MONTH) + 1);
        return BigDecimal.ONE.add(new BigDecimal("0.01").multiply(new BigDecimal(k)));
    }

    static String fmt(Date data) {
        if (data == null) return "";
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        return String.format("%04d-%02d-%02d",
            c.get(GregorianCalendar.YEAR), c.get(GregorianCalendar.MONTH) + 1, c.get(GregorianCalendar.DAY_OF_MONTH));
    }

    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }
    static String p(Boolean v) { return v == null ? "" : (v ? "1" : "0"); }

    static Calculo novoCalculo(Date admissao, Date demissao, Date ajuizamento) {
        Calculo c = new Calculo();
        c.setDataAdmissao(admissao);
        c.setDataDemissao(demissao);
        c.setDataAjuizamento(ajuizamento);
        c.setDataDeLiquidacao(d(2022, 6, 30));
        c.setZeraValorNegativo(Boolean.TRUE);
        c.setOrdem(0);
        c.setListaDeFerias(new LinkedHashSet<Ferias>());
        c.setFaltas(new LinkedHashSet<Falta>());
        Utils.adicionarRepositorioParaTeste(RepositorioDeCalculo.class, new RepoCalculoStub(c));
        return c;
    }

    static Ferias novasFerias(Calculo c, Date iniPA, Date fimPA, Date iniConc, Date fimConc,
                              int prazo, SituacaoDaFeriasEnum situacao) {
        Ferias f = new Ferias();
        f.setCalculo(c);
        f.setPeriodoAquisitivo(new Periodo(iniPA, fimPA));
        f.setPeriodoConcessivo(new Periodo(iniConc, fimConc));
        f.setPrazo(prazo);
        f.setSituacao(situacao);
        f.setDobraGeral(Boolean.FALSE);
        f.setAbono(Boolean.FALSE);
        f.setQuantidadeDiasAbono(10);
        f.setDobraDoPeriodoDeGozo1(Boolean.FALSE);
        f.setDobraDoPeriodoDeGozo2(Boolean.FALSE);
        f.setDobraDoPeriodoDeGozo3(Boolean.FALSE);
        c.getListaDeFerias().add(f);
        return f;
    }

    static Falta novaFalta(Calculo c, Date inicio, Date fim, boolean justificada, boolean reinicia) {
        Falta f = new Falta();
        f.setCalculo(c);
        f.setDataInicioPeriodoFalta(inicio);
        f.setDataTerminoPeriodoFalta(fim);
        f.setFaltaJustificada(justificada);
        f.setReiniciarFerias(reinicia);
        c.getFaltas().add(f);
        return f;
    }

    static void configurarFlags(VerbaDeCalculo v, String nome) {
        v.setNome(nome);
        v.setExcluirFeriasGozadas(Boolean.FALSE);
        v.setExcluirFaltaJustificada(Boolean.FALSE);
        v.setExcluirFaltaNaoJustificada(Boolean.FALSE);
        v.setZeraValorNegativo(Boolean.TRUE);
        v.setComporPrincipal(LogicoEnum.SIM);
        v.setGerarPrincipal(TipoDeGeracaoEnum.DIFERENCA);
        v.setGerarReflexo(TipoDeGeracaoEnum.DIFERENCA);
        v.setAplicarProporcionalidade(Boolean.FALSE);
    }

    static Calculada novaVerbaDeFerias(Calculo c, Date periodoInicial, Date periodoFinal) {
        Calculada v = new Calculada(c);
        configurarFlags(v, "ferias");
        v.setCaracteristica(CaracteristicaDaVerbaEnum.FERIAS);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.PERIODO_AQUISITIVO);
        v.setPeriodoInicial(periodoInicial);
        v.setPeriodoFinal(periodoFinal);
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(BigDecimal.ONE);
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(BigDecimal.ONE);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        return v;
    }

    static void executar(VerbaDeCalculo v, ServicoDeCalculo serv) throws Exception {
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
    }

    /** Linhas OCF com os campos de férias (PA, indenizada, abono, incidências). */
    static void dump(String caso, VerbaDeCalculo v) {
        for (OcorrenciaDeVerba o : v.getOcorrencias()) {
            System.out.println("OCF;" + caso + ";" + fmt(o.getDataInicial()) + ";" + fmt(o.getDataFinal())
                + ";" + p(o.getAtivo()) + ";" + p(o.getBase()) + ";" + p(o.getDivisor()) + ";" + p(o.getMultiplicador())
                + ";" + p(o.getQuantidade()) + ";" + p(o.getQuantidadeIntegral()) + ";" + p(o.getDevido())
                + ";" + p(o.getDevidoIntegral()) + ";" + p(o.getPago()) + ";" + p(o.getPagoIntegral())
                + ";" + p(o.getDobra()) + ";" + p(o.getIndiceAcumulado()) + ";" + p(o.getDiferenca())
                + ";" + p(o.getDiferencaCorrigida())
                + ";" + fmt(o.getDataInicialPeriodoAquisitivo()) + ";" + fmt(o.getDataFinalPeriodoAquisitivo())
                + ";" + p(o.isFeriasIndenizadas()) + ";" + p(o.isFeriasComAbono())
                + ";" + p(o.getDiferencaParaCalculoDasIncidencias(true)));
        }
        System.out.println("TOTF;" + caso + ";" + p(v.getValorTotalDevido()) + ";" + p(v.getValorTotalPago())
            + ";" + p(v.getValorTotalDiferenca()) + ";" + p(v.getValorTotalDiferencaCorrigida())
            + ";" + p(v.getValorTotalDiferencaCorrigidaParaCalculoDasIncidencias())
            + ";" + p(v.getValorTotalDiferencaCorrigidaDeFeriasGozadas()));
    }

    // ---------- parte 1: tabela do art. 130 ----------

    static void prazos() {
        int[][] gerais = { {0}, {5}, {6}, {14}, {15}, {23}, {24}, {32}, {33} };
        for (int[] caso : gerais) {
            CalculoDoPrazoDeFerias calc = CalculoDoPrazoDeFerias.getInstance();
            calc.parametros(d(2018, 5, 31), RegimeDoContratoEnum.INTEGRAL, caso[0]).executar();
            System.out.println("PRAZO;2018-05-31;INTEGRAL;" + caso[0] + ";" + calc.getResultado());
        }
        int[][] parciais = { {0}, {7}, {8} };
        for (int[] caso : parciais) {
            CalculoDoPrazoDeFerias calc = CalculoDoPrazoDeFerias.getInstance();
            calc.parametros(d(2017, 6, 30), RegimeDoContratoEnum.PARCIAL, caso[0]).executar();
            System.out.println("PRAZO;2017-06-30;PARCIAL;" + caso[0] + ";" + calc.getResultado());
        }
        CalculoDoPrazoDeFerias calc = CalculoDoPrazoDeFerias.getInstance();
        calc.parametros(d(2018, 6, 30), RegimeDoContratoEnum.PARCIAL, 3).executar();
        System.out.println("PRAZO;2018-06-30;PARCIAL;3;" + calc.getResultado());
    }

    // ---------- parte 2: breakInYears (períodos aquisitivos) ----------

    static void quebrasEmAnos() {
        Date[][] casos = {
            { d(2019, 3, 20), d(2021, 12, 28) },
            { d(2020, 2, 29), d(2023, 3, 1) },
            { d(2019, 1, 10), d(2022, 3, 10) },
            { d(2021, 5, 1), d(2021, 12, 31) },
        };
        for (Date[] caso : casos) {
            StringBuilder sb = new StringBuilder("BRKY;" + fmt(caso[0]) + ";" + fmt(caso[1]));
            for (Periodo p : HelperDate.breakInYears(caso[0], caso[1], false)) {
                sb.append(";").append(fmt(p.getInicial())).append("..").append(fmt(p.getFinal()));
            }
            System.out.println(sb);
        }
    }

    // ---------- parte 3: salário em férias (rateio entre meses) ----------

    static void salarioEmFerias() {
        Object[][] casos = {
            { d(2021, 6, 1), d(2021, 6, 30), "3000.00", null },
            { d(2021, 6, 10), d(2021, 6, 24), "3000.00", null },
            { d(2021, 1, 20), d(2021, 2, 18), "3000.00", "3300.00" },
            { d(2021, 1, 25), d(2021, 2, 3), "3000.00", null },
        };
        for (Object[] caso : casos) {
            CalculoDoSalarioEmFerias calc = new CalculoDoSalarioEmFerias(
                new Periodo((Date) caso[0], (Date) caso[1]),
                new BigDecimal((String) caso[2]),
                caso[3] == null ? null : new BigDecimal((String) caso[3]));
            calc.executar();
            System.out.println("SALFER;" + fmt((Date) caso[0]) + ";" + fmt((Date) caso[1]) + ";" + caso[2]
                + ";" + (caso[3] == null ? "" : caso[3]) + ";" + p(calc.getResultado()));
        }
    }

    // ---------- parte 4: pipeline PERIODO_AQUISITIVO ----------

    /**
     * Contrato 2019-01-10 .. demissão 2022-03-10 (ajuizamento 2022-04-15, sem prescrição
     * efetiva). PA1 gozado cruzando o fim do concessivo (dobra na 2ª parte); PA2 gozado
     * parcialmente (15 dias) com saldo; PA3 indenizado; fracionário 2022-01-10..demissão
     * projetada.
     */
    static void casoFeriasCompletas() throws Exception {
        Calculo c = novoCalculo(d(2019, 1, 10), d(2022, 3, 10), d(2022, 4, 15));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        ServicoStub serv = new ServicoStub(c);

        Ferias pa1 = novasFerias(c, d(2019, 1, 10), d(2020, 1, 9), d(2020, 1, 10), d(2021, 1, 9),
            30, SituacaoDaFeriasEnum.GOZADAS);
        pa1.setPeriodoDeGozo1(new Periodo(d(2020, 12, 26), d(2021, 1, 24)));
        pa1.setDobraDoPeriodoDeGozo1(Boolean.TRUE);

        Ferias pa2 = novasFerias(c, d(2020, 1, 10), d(2021, 1, 9), d(2021, 1, 10), d(2022, 1, 9),
            30, SituacaoDaFeriasEnum.GOZADAS_PARCIALMENTE);
        pa2.setPeriodoDeGozo1(new Periodo(d(2021, 5, 1), d(2021, 5, 15)));

        novasFerias(c, d(2021, 1, 10), d(2022, 1, 9), d(2022, 1, 10), d(2023, 1, 9),
            30, SituacaoDaFeriasEnum.INDENIZADAS);

        Calculada v = novaVerbaDeFerias(c, d(2019, 1, 10), d(2022, 3, 10));
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v, serv);
        dump("FER_COMPLETAS", v);
    }

    /** Férias gozadas com abono (prazo 30, abono 10, gozo 20 dias): fator na base e retirada nas incidências. */
    static void casoFeriasComAbono() throws Exception {
        Calculo c = novoCalculo(d(2019, 1, 10), d(2022, 3, 10), d(2022, 4, 15));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        ServicoStub serv = new ServicoStub(c);

        Ferias pa1 = novasFerias(c, d(2019, 1, 10), d(2020, 1, 9), d(2020, 1, 10), d(2021, 1, 9),
            30, SituacaoDaFeriasEnum.GOZADAS_PARCIALMENTE);
        pa1.setAbono(Boolean.TRUE);
        pa1.setQuantidadeDiasAbono(10);
        pa1.setPeriodoDeGozo1(new Periodo(d(2020, 6, 1), d(2020, 6, 20)));

        Calculada v = novaVerbaDeFerias(c, d(2019, 1, 10), d(2022, 3, 10));
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v, serv);
        dump("FER_ABONO", v);
    }

    /** Prescrição quinquenal barra o PA cujo concessivo terminou antes da data de prescrição. */
    static void casoFeriasPrescritas() throws Exception {
        Calculo c = novoCalculo(d(2014, 1, 10), d(2022, 3, 10), d(2022, 4, 15));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        c.setPrescricaoQuinquenal(Boolean.TRUE); // prescrição: 2017-04-15
        ServicoStub serv = new ServicoStub(c);

        // Concessivo terminou 2016-01-09 < 2017-04-15 -> barrada (indenizada não gera).
        novasFerias(c, d(2014, 1, 10), d(2015, 1, 9), d(2015, 1, 10), d(2016, 1, 9),
            30, SituacaoDaFeriasEnum.INDENIZADAS);
        // Concessivo termina 2022-01-09 >= 2017-04-15 -> gera.
        novasFerias(c, d(2020, 1, 10), d(2021, 1, 9), d(2021, 1, 10), d(2022, 1, 9),
            30, SituacaoDaFeriasEnum.INDENIZADAS);

        Calculada v = novaVerbaDeFerias(c, d(2017, 4, 15), d(2022, 3, 10));
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v, serv);
        dump("FER_PRESCRITAS", v);
    }

    /**
     * Férias proporcionais puras: sem PA completo na lista, só o fracionário
     * (admissão 2021-10-01, demissão 2022-03-10). Quantidade em AVOS do PA,
     * prazo proporcional com 6 faltas NJ no PA (art. 130 -> 24 dias).
     */
    static void casoFeriasProporcionais() throws Exception {
        Calculo c = novoCalculo(d(2021, 10, 1), d(2022, 3, 10), d(2022, 4, 15));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        novaFalta(c, d(2021, 11, 8), d(2021, 11, 13), false, false); // 6 dias NJ
        Utils.adicionarRepositorioParaTeste(RepositorioDeFalta.class, new RepoFaltaStub());
        ServicoStub serv = new ServicoStub(c);

        Calculada v = novaVerbaDeFerias(c, d(2021, 10, 1), d(2022, 3, 10));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.getDivisor().setOutroValor(new BigDecimal("12"));
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.AVOS);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v, serv);
        dump("FER_PROPORCIONAIS", v);
    }

    // ---------- parte 5: faltas nos provedores (verba mensal) ----------

    /**
     * Verba mensal informada com proporcionalidade e exclusões reais: falta NJ de 6 dias
     * em nov/2021, falta justificada de 3 dias em dez/2021, férias gozadas 10 dias em
     * out/2021 (a verba exclui os três).
     */
    static void casoMensalComFaltas() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null, d(2022, 4, 15));
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        novaFalta(c, d(2021, 11, 8), d(2021, 11, 13), false, false);
        novaFalta(c, d(2021, 12, 6), d(2021, 12, 8), true, false);
        Ferias ferias = novasFerias(c, d(2020, 1, 10), d(2021, 1, 9), d(2021, 1, 10), d(2022, 1, 9),
            30, SituacaoDaFeriasEnum.GOZADAS_PARCIALMENTE);
        ferias.setPeriodoDeGozo1(new Periodo(d(2021, 10, 11), d(2021, 10, 20)));
        Utils.adicionarRepositorioParaTeste(RepositorioDeFalta.class, new RepoFaltaStub());
        // obterDiasFerias passa por LazyloadSecure, que exige coleção Hibernate:
        // envolve o set em um PersistentSet inicializado (sessão nula -> o "reload"
        // cai no repositório stub, que devolve o próprio cálculo).
        c.setListaDeFerias(new org.hibernate.collection.PersistentSet(null, c.getListaDeFerias()));
        ServicoStub serv = new ServicoStub(c);

        Informada v = new Informada(c);
        configurarFlags(v, "mensal-faltas");
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 10, 1));
        v.setPeriodoFinal(d(2021, 12, 31));
        v.setAplicarProporcionalidade(Boolean.TRUE);
        v.setExcluirFeriasGozadas(Boolean.TRUE);
        v.setExcluirFaltaJustificada(Boolean.TRUE);
        v.setExcluirFaltaNaoJustificada(Boolean.TRUE);
        FormulaInformada f = (FormulaInformada) v.getFormula();
        f.getConstante().setValor(new BigDecimal("3000.00"));
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqInformada(v, serv));
        executar(v, serv);
        dump("MENSAL_FALTAS", v);
    }

    // ---------- parte 6: reflexo com destino férias ----------

    /**
     * Reflexo de horas extras EM férias: origem informada mensal; reflexo com
     * característica FERIAS, PERIODO_AQUISITIVO, média pelo valor na janela do PA.
     * Férias: PA 2020-06-01..2021-05-31, gozo integral 2021-08-01..30.
     */
    static void casoReflexoEmFerias() throws Exception {
        Calculo c = novoCalculo(d(2020, 6, 1), d(2022, 3, 10), d(2022, 4, 15));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        ServicoStub serv = new ServicoStub(c);

        Ferias ferias = novasFerias(c, d(2020, 6, 1), d(2021, 5, 31), d(2021, 6, 1), d(2022, 5, 31),
            30, SituacaoDaFeriasEnum.GOZADAS);
        ferias.setPeriodoDeGozo1(new Periodo(d(2021, 8, 1), d(2021, 8, 30)));

        Informada origem = new Informada(c);
        configurarFlags(origem, "he");
        origem.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        origem.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        origem.setPeriodoInicial(d(2020, 6, 1));
        origem.setPeriodoFinal(d(2022, 3, 10));
        origem.setAplicarProporcionalidade(Boolean.TRUE);
        FormulaInformada fo = (FormulaInformada) origem.getFormula();
        fo.getConstante().setValor(new BigDecimal("600.00"));
        fo.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        fo.getValorPago().setValorInformado(BigDecimal.ZERO);
        origem.setMaquinaDeCalculorencias(new MaqInformada(origem, serv));
        executar(origem, serv);

        Reflexo r = new Reflexo(c);
        configurarFlags(r, "reflexo-he-ferias");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.FERIAS);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.PERIODO_AQUISITIVO);
        r.setPeriodoInicial(d(2020, 6, 1));
        r.setPeriodoFinal(d(2022, 3, 10));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.MEDIA_PELO_VALOR);
        r.setPeriodoMediaReflexo(PeriodoDaMediaDoReflexoEnum.PERIODO_AQUISITIVO);
        r.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo fr = (FormulaReflexo) r.getFormula();
        fr.getBaseVerba().getItens().add(new ItemBaseVerba(fr, origem, LogicoEnum.NAO));
        fr.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        fr.getDivisor().setOutroValor(BigDecimal.ONE);
        fr.getMultiplicador().setOutroValor(BigDecimal.ONE);
        fr.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        fr.getQuantidade().setValorInformado(BigDecimal.ONE);
        fr.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        fr.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        fr.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        executar(r, serv);
        dump("REFLEXO_MV_PA_FERIAS", r);

        // Variante VALOR_MENSAL com destino férias (mesma origem, mesmas férias).
        Reflexo r2 = new Reflexo(c);
        configurarFlags(r2, "reflexo-he-ferias-vm");
        r2.setCaracteristica(CaracteristicaDaVerbaEnum.FERIAS);
        r2.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.PERIODO_AQUISITIVO);
        r2.setPeriodoInicial(d(2020, 6, 1));
        r2.setPeriodoFinal(d(2022, 3, 10));
        r2.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.VALOR_MENSAL);
        r2.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo fr2 = (FormulaReflexo) r2.getFormula();
        fr2.getBaseVerba().getItens().add(new ItemBaseVerba(fr2, origem, LogicoEnum.NAO));
        fr2.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        fr2.getDivisor().setOutroValor(BigDecimal.ONE);
        fr2.getMultiplicador().setOutroValor(BigDecimal.ONE);
        fr2.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        fr2.getQuantidade().setValorInformado(BigDecimal.ONE);
        fr2.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        fr2.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        fr2.getValorPago().setValorInformado(BigDecimal.ZERO);
        r2.setMaquinaDeCalculorencias(new MaqReflexo(r2, serv));
        executar(r2, serv);
        dump("REFLEXO_VM_FERIAS", r2);
    }

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeFalta.class, new RepoFaltaStub());

        prazos();
        quebrasEmAnos();
        salarioEmFerias();
        casoFeriasCompletas();
        casoFeriasComAbono();
        casoFeriasPrescritas();
        casoFeriasProporcionais();
        casoMensalComFaltas();
        casoReflexoEmFerias();
    }
}
