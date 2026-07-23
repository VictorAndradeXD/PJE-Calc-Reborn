import br.jus.trt8.pjecalc.base.comum.Feriado;
import br.jus.trt8.pjecalc.base.comum.LogicoFuzzy;
import br.jus.trt8.pjecalc.base.comum.Periodo;
import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.BaseDeCalculoDoPrincipalEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.ComportamentoDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.DivisorDeVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.OcorrenciaDePagamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.PeriodoDaMediaDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.SituacaoDaFeriasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeGeracaoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeImportadaDoCalendarioEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoValorPagoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoVinculoDeCartaoDePontoDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TratamentoDaFracaoDeMesDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.ExcecaoDaCargaHorariaDoCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.ExcecaoDoSabadoDoCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.RepositorioDeCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.faltas.Falta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.faltas.RepositorioDeFalta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.ferias.Ferias;
import br.jus.trt8.pjecalc.negocio.dominio.cartaodeponto.CartaoDePonto;
import br.jus.trt8.pjecalc.negocio.dominio.cartaodeponto.OcorrenciaDoCartaoDePonto;
import br.jus.trt8.pjecalc.negocio.dominio.cartaodeponto.RepositorioDeOcorrenciaDoCartaoDePonto;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.termo.BaseTabelada;
import br.jus.trt8.pjecalc.negocio.dominio.termo.ItemBaseVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Calculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.CartaoDePontoDaVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Reflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.TabelaDeCorrecaoMonetaria;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;
import br.jus.trt8.pjecalc.negocio.servicos.ServicoDeCalculo;
import org.jboss.seam.contexts.Contexts;
import org.jboss.seam.contexts.Lifecycle;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * Golden de CALENDÁRIO (dias úteis/repousos/feriados com feriados injetados via contexto
 * Seam mínimo), CARGA HORÁRIA, CARTÃO DE PONTO (consumo pela verba) e MÉDIA PELA
 * QUANTIDADE dos reflexos. Índice de correção estubado igual aos harnesses anteriores.
 */
public class GoldenGenCalendarioMq {

    // ---------- feriados fixos de 2021 injetados no lugar do repositório Seam ----------

    static final Set<String> FERIADOS_2021 = new HashSet<String>(java.util.Arrays.asList(
        "2021-01-01", "2021-02-16", "2021-04-21", "2021-05-01", "2021-06-03",
        "2021-09-07", "2021-10-12", "2021-11-02", "2021-11-15", "2021-12-25"));

    static class FeriadoStub implements Feriado {
        private final SimpleDateFormat formato = new SimpleDateFormat("yyyy-MM-dd");
        @Override public boolean buscarFeriado(Date data) {
            return FERIADOS_2021.contains(formato.format(data));
        }
        @Override public boolean buscarFeriadoFederal(Date data) {
            return buscarFeriado(data);
        }
    }

    // ---------- stubs (mesma receita dos harnesses anteriores) ----------

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

    static class RepoOcorrenciaCartaoStub extends RepositorioDeOcorrenciaDoCartaoDePonto {
        @Override public List<OcorrenciaDoCartaoDePonto> obterOcorrencias(CartaoDePonto cartao) {
            return cartao.getOcorrencias();
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
        return new SimpleDateFormat("yyyy-MM-dd").format(data);
    }

    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }
    static String p(Boolean v) { return v == null ? "" : (v ? "1" : "0"); }

    static Calculo novoCalculo(Date admissao, Date demissao) {
        Calculo c = new Calculo();
        c.setDataAdmissao(admissao);
        c.setDataDemissao(demissao);
        c.setDataAjuizamento(d(2022, 4, 15));
        c.setDataDeLiquidacao(d(2022, 6, 30));
        c.setZeraValorNegativo(Boolean.TRUE);
        c.setPrescricaoQuinquenal(Boolean.FALSE);
        c.setOrdem(0);
        c.setListaDeFerias(new LinkedHashSet<Ferias>());
        c.setFaltas(new LinkedHashSet<Falta>());
        c.setSabadoDiaUtil(Boolean.TRUE);
        Utils.adicionarRepositorioParaTeste(RepositorioDeCalculo.class, new RepoCalculoStub(c));
        return c;
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

    static CartaoDePonto novoCartao(Calculo c, String nome, Object[][] competenciaValor) {
        CartaoDePonto cartao = new CartaoDePonto();
        cartao.setCalculo(c);
        cartao.setNome(nome);
        for (Object[] cv : competenciaValor) {
            cartao.getOcorrencias().add(new OcorrenciaDoCartaoDePonto(cartao, (Date) cv[0], new BigDecimal((String) cv[1])));
        }
        return cartao;
    }

    static void executar(VerbaDeCalculo v) throws Exception {
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
    }

    static void dump(String caso, VerbaDeCalculo v) {
        for (OcorrenciaDeVerba o : v.getOcorrencias()) {
            System.out.println("OCC;" + caso + ";" + fmt(o.getDataInicial()) + ";" + fmt(o.getDataFinal())
                + ";" + p(o.getAtivo()) + ";" + p(o.getBase()) + ";" + p(o.getDivisor()) + ";" + p(o.getMultiplicador())
                + ";" + p(o.getQuantidade()) + ";" + p(o.getDevido()) + ";" + p(o.getPago())
                + ";" + p(o.getIndiceAcumulado()) + ";" + p(o.getDiferenca()) + ";" + p(o.getDiferencaCorrigida()));
        }
        System.out.println("TOTC;" + caso + ";" + p(v.getValorTotalDevido()) + ";" + p(v.getValorTotalPago())
            + ";" + p(v.getValorTotalDiferenca()) + ";" + p(v.getValorTotalDiferencaCorrigida()));
    }

    // ---------- parte 1: contagens de calendário ----------

    static void calendario(Calculo calculoComExcecaoDeSabado) {
        Object[][] casos = {
            { d(2021, 3, 1), d(2021, 3, 31), "SAB_UTIL" },
            { d(2021, 3, 1), d(2021, 3, 31), "SAB_NAO_UTIL" },
            { d(2021, 4, 1), d(2021, 4, 30), "SAB_UTIL" },      // 21/04 quarta
            { d(2021, 4, 1), d(2021, 4, 30), "SAB_NAO_UTIL" },
            { d(2021, 5, 1), d(2021, 5, 31), "SAB_UTIL" },      // 01/05 SÁBADO feriado
            { d(2021, 5, 1), d(2021, 5, 31), "SAB_NAO_UTIL" },
            { d(2021, 1, 1), d(2021, 1, 31), "SAB_NAO_UTIL" },  // 01/01 sexta feriado
            { d(2021, 6, 1), d(2021, 6, 30), "SAB_UTIL" },      // 03/06 quinta feriado
            { d(2021, 4, 1), d(2021, 4, 30), "SAB_UTIL_COM_EXCECAO" }, // exceção 10..30/04 inverte sábados
        };
        for (Object[] caso : casos) {
            Periodo periodo = new Periodo((Date) caso[0], (Date) caso[1]);
            String config = (String) caso[2];
            LogicoFuzzy<?> fuzzy =
                config.equals("SAB_UTIL") ? LogicoFuzzy.VERDADEIRO :
                config.equals("SAB_NAO_UTIL") ? LogicoFuzzy.FALSO :
                calculoComExcecaoDeSabado.getSabadoDiaUtilComExcecao();
            System.out.println("CAL;" + fmt((Date) caso[0]) + ";" + fmt((Date) caso[1]) + ";" + config
                + ";" + periodo.totalDeDiasUteis(fuzzy) + ";" + periodo.totalDeDiasNaoUteis(fuzzy)
                + ";" + periodo.totalDeFeriados() + ";" + periodo.totalDeRepousosEFeriados(fuzzy));
        }
    }

    // ---------- parte 2: carga horária ----------

    static void cargaHoraria() {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorCargaHorariaPadrao(new BigDecimal("220.0"));
        LinkedHashSet<ExcecaoDaCargaHorariaDoCalculo> excecoes = new LinkedHashSet<ExcecaoDaCargaHorariaDoCalculo>();
        excecoes.add(new ExcecaoDaCargaHorariaDoCalculo(c, d(2021, 2, 1), d(2021, 4, 15), new BigDecimal("180.0")));
        c.setExcecoesDaCargaHoraria(excecoes);

        Object[][] periodos = {
            { d(2021, 1, 1), d(2021, 1, 31) },   // fora da exceção -> 220
            { d(2021, 2, 1), d(2021, 2, 28) },   // dentro -> 180
            { d(2021, 4, 1), d(2021, 4, 30) },   // 15 dias 180 + 15 dias 220 -> média
        };
        for (Object[] par : periodos) {
            Periodo periodo = new Periodo((Date) par[0], (Date) par[1]);
            System.out.println("CARGA;" + fmt((Date) par[0]) + ";" + fmt((Date) par[1])
                + ";" + p(c.getValorCargaHoraria(periodo)));
        }
    }

    // ---------- parte 3: verba com divisor DIAS_UTEIS + quantidade REPOUSOS (DSR) ----------

    static void casoDsr() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        ServicoStub serv = new ServicoStub(c);

        Calculada v = new Calculada(c);
        configurarFlags(v, "dsr");
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 4, 1));
        v.setPeriodoFinal(d(2021, 6, 30));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.DIAS_UTEIS);
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.IMPORTADA_DO_CALENDARIO);
        f.getQuantidade().setTipoImportadaCalendarioEnum(TipoDeQuantidadeImportadaDoCalendarioEnum.REPOUSOS);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v);
        dump("DSR", v);
    }

    /** DSR com sábado NÃO útil, falta NJ cobrindo um fim de semana e férias gozadas no mês seguinte. */
    static void casoDsrComExclusoes() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        c.setSabadoDiaUtil(Boolean.FALSE);
        // Falta não justificada 03/04 (sáb) .. 11/04 (dom): 2 sáb + 2 dom.
        Falta falta = new Falta();
        falta.setCalculo(c);
        falta.setDataInicioPeriodoFalta(d(2021, 4, 3));
        falta.setDataTerminoPeriodoFalta(d(2021, 4, 11));
        falta.setFaltaJustificada(Boolean.FALSE);
        falta.setReiniciarFerias(Boolean.FALSE);
        c.getFaltas().add(falta);
        // Férias gozadas 10..23/05 (2 sáb + 2 dom).
        Ferias ferias = new Ferias();
        ferias.setCalculo(c);
        ferias.setPeriodoAquisitivo(new Periodo(d(2020, 1, 10), d(2021, 1, 9)));
        ferias.setPeriodoConcessivo(new Periodo(d(2021, 1, 10), d(2022, 1, 9)));
        ferias.setPrazo(30);
        ferias.setSituacao(SituacaoDaFeriasEnum.GOZADAS_PARCIALMENTE);
        ferias.setDobraGeral(Boolean.FALSE);
        ferias.setAbono(Boolean.FALSE);
        ferias.setPeriodoDeGozo1(new Periodo(d(2021, 5, 10), d(2021, 5, 23)));
        c.getListaDeFerias().add(ferias);
        c.setListaDeFerias(new org.hibernate.collection.PersistentSet(null, c.getListaDeFerias()));
        ServicoStub serv = new ServicoStub(c);

        Calculada v = new Calculada(c);
        configurarFlags(v, "dsr-exclusoes");
        v.setExcluirFaltaNaoJustificada(Boolean.TRUE);
        v.setExcluirFeriasGozadas(Boolean.TRUE);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 4, 1));
        v.setPeriodoFinal(d(2021, 5, 31));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.DIAS_UTEIS);
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.IMPORTADA_DO_CALENDARIO);
        f.getQuantidade().setTipoImportadaCalendarioEnum(TipoDeQuantidadeImportadaDoCalendarioEnum.REPOUSOS_FERIADOS);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v);
        dump("DSR_EXCLUSOES", v);
    }

    // ---------- parte 4: verba com divisor e quantidade do cartão de ponto ----------

    static void casoCartao() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        ServicoStub serv = new ServicoStub(c);

        Calculada v = new Calculada(c);
        configurarFlags(v, "he-cartao");
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 1));
        v.setPeriodoFinal(d(2021, 3, 31));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.IMPORTADA_DO_CARTAO);
        f.getMultiplicador().setOutroValor(new BigDecimal("1.5"));
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.IMPORTADA_DO_CARTAO);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);

        CartaoDePonto cartaoDivisor = novoCartao(c, "horas-mes", new Object[][] {
            { d(2021, 1, 1), "200.00" }, { d(2021, 2, 1), "210.00" }, { d(2021, 3, 1), "220.00" },
        });
        CartaoDePonto cartaoQuantidade = novoCartao(c, "horas-extras", new Object[][] {
            { d(2021, 1, 1), "10.00" }, { d(2021, 2, 1), "12.50" }, { d(2021, 3, 1), "8.00" },
        });
        LinkedHashSet<CartaoDePontoDaVerba> vinculosDivisor = new LinkedHashSet<CartaoDePontoDaVerba>();
        vinculosDivisor.add(new CartaoDePontoDaVerba(v, cartaoDivisor, TipoVinculoDeCartaoDePontoDaVerbaEnum.DIVISOR));
        v.adicionarCartoesVinculadosAtravesDoDivisor(vinculosDivisor);
        LinkedHashSet<CartaoDePontoDaVerba> vinculosQuantidade = new LinkedHashSet<CartaoDePontoDaVerba>();
        vinculosQuantidade.add(new CartaoDePontoDaVerba(v, cartaoQuantidade, TipoVinculoDeCartaoDePontoDaVerbaEnum.QUANTIDADE));
        v.adicionarCartoesVinculadosAtravesDaQuantidade(vinculosQuantidade);

        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        executar(v);
        dump("CARTAO", v);
    }

    // ---------- parte 5: média pela quantidade ----------

    /** Origem: HE calculada com quantidade do cartão (12 meses variados). */
    static Calculada novaOrigemHe(Calculo c, ServicoStub serv, Date inicio, Date fim,
                                  BigDecimal pagoInformado, String nomeCartao, Object[][] cartaoValores) throws Exception {
        Calculada origem = new Calculada(c);
        configurarFlags(origem, "he-" + nomeCartao);
        origem.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        origem.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        origem.setPeriodoInicial(inicio);
        origem.setPeriodoFinal(fim);
        FormulaCalculada f = (FormulaCalculada) origem.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("220"));
        f.getMultiplicador().setOutroValor(new BigDecimal("1.5"));
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.IMPORTADA_DO_CARTAO);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(pagoInformado);

        CartaoDePonto cartao = novoCartao(c, nomeCartao, cartaoValores);
        LinkedHashSet<CartaoDePontoDaVerba> vinculos = new LinkedHashSet<CartaoDePontoDaVerba>();
        vinculos.add(new CartaoDePontoDaVerba(origem, cartao, TipoVinculoDeCartaoDePontoDaVerbaEnum.QUANTIDADE));
        origem.adicionarCartoesVinculadosAtravesDaQuantidade(vinculos);
        origem.setMaquinaDeCalculorencias(new MaqCalculada(origem, serv));
        executar(origem);
        return origem;
    }

    static Object[][] cartao2021() {
        return new Object[][] {
            { d(2021, 1, 1), "10.00" }, { d(2021, 2, 1), "12.00" }, { d(2021, 3, 1), "14.00" },
            { d(2021, 4, 1), "16.00" }, { d(2021, 5, 1), "18.00" }, { d(2021, 6, 1), "20.00" },
            { d(2021, 7, 1), "11.00" }, { d(2021, 8, 1), "13.00" }, { d(2021, 9, 1), "15.00" },
            { d(2021, 10, 1), "17.00" }, { d(2021, 11, 1), "19.00" }, { d(2021, 12, 1), "21.00" },
        };
    }

    static Reflexo novoReflexoDecimoTerceiroMq(Calculo c, ServicoStub serv, Calculada origem,
                                               TratamentoDaFracaoDeMesDoReflexoEnum tratamento) {
        Reflexo r = new Reflexo(c);
        configurarFlags(r, "reflexo-13-mq");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DEZEMBRO);
        r.setPeriodoInicial(d(2021, 1, 1));
        r.setPeriodoFinal(d(2021, 12, 31));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.MEDIA_PELA_QUANTIDADE);
        r.setPeriodoMediaReflexo(PeriodoDaMediaDoReflexoEnum.ANO_CIVIL);
        r.setTratamentoDaFracaoDeMesDoReflexo(tratamento);
        FormulaReflexo f = (FormulaReflexo) r.getFormula();
        f.getBaseVerba().getItens().add(new ItemBaseVerba(f, origem, LogicoEnum.NAO));
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("12"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("12"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        return r;
    }

    /** MQ sobre o DEVIDO da origem, ano civil completo. */
    static void casoMqDevido() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada origem = novaOrigemHe(c, serv, d(2021, 1, 1), d(2021, 12, 31),
            BigDecimal.ZERO, "mq-dev", cartao2021());
        origem.setGerarReflexo(TipoDeGeracaoEnum.DEVIDO);
        Reflexo r = novoReflexoDecimoTerceiroMq(c, serv, origem, TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        executar(r);
        dump("MQ_DEVIDO", r);
    }

    /** MQ sobre a DIFERENÇA (pago parcial fixo): proporção paga por competência. */
    static void casoMqDiferenca() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada origem = novaOrigemHe(c, serv, d(2021, 1, 1), d(2021, 12, 31),
            new BigDecimal("100.00"), "mq-dif", cartao2021());
        Reflexo r = novoReflexoDecimoTerceiroMq(c, serv, origem, TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        executar(r);
        dump("MQ_DIFERENCA", r);
    }

    /** MQ com mês parcial (origem começa 15/01) nos tratamentos DESPREZAR e DMQ15. */
    static void casoMqTratamentos() throws Exception {
        for (Object[] par : new Object[][] {
            { TratamentoDaFracaoDeMesDoReflexoEnum.DESPREZAR, "MQ_DESPREZAR" },
            { TratamentoDaFracaoDeMesDoReflexoEnum.DESPREZAR_MENOR_QUE_15_DIAS, "MQ_DMQ15" },
        }) {
            Calculo c = novoCalculo(d(2020, 1, 10), null);
            c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
            ServicoStub serv = new ServicoStub(c);
            Calculada origem = novaOrigemHe(c, serv, d(2021, 1, 15), d(2021, 12, 31),
                BigDecimal.ZERO, "mq-" + par[1], cartao2021());
            origem.setGerarReflexo(TipoDeGeracaoEnum.DEVIDO);
            Reflexo r = novoReflexoDecimoTerceiroMq(c, serv, origem,
                (TratamentoDaFracaoDeMesDoReflexoEnum) par[0]);
            executar(r);
            dump((String) par[1], r);
        }
    }

    /** MQ com abatimento: mês de quantidade zero mas pago > 0 e diferença negativa preservada. */
    static void casoMqAbatimento() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null);
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        ServicoStub serv = new ServicoStub(c);
        Object[][] cartao = cartao2021();
        cartao[2][1] = "0.00"; // março: devido zero, pago 100 -> abatimento
        Calculada origem = novaOrigemHe(c, serv, d(2021, 1, 1), d(2021, 12, 31),
            new BigDecimal("100.00"), "mq-abate", cartao);
        origem.setZeraValorNegativo(Boolean.FALSE);
        Reflexo r = novoReflexoDecimoTerceiroMq(c, serv, origem, TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        executar(r);
        dump("MQ_ABATIMENTO", r);
    }

    /** MQ em férias (PERIODO_AQUISITIVO): janela = últimos 12 meses do mês simulado. */
    static void casoMqFerias() throws Exception {
        Calculo c = novoCalculo(d(2020, 6, 1), d(2022, 3, 10));
        c.setValorUltimaRemuneracao(new BigDecimal("2200.00"));
        Ferias ferias = new Ferias();
        ferias.setCalculo(c);
        ferias.setPeriodoAquisitivo(new Periodo(d(2020, 6, 1), d(2021, 5, 31)));
        ferias.setPeriodoConcessivo(new Periodo(d(2021, 6, 1), d(2022, 5, 31)));
        ferias.setPrazo(30);
        ferias.setSituacao(SituacaoDaFeriasEnum.GOZADAS);
        ferias.setDobraGeral(Boolean.FALSE);
        ferias.setAbono(Boolean.FALSE);
        ferias.setPeriodoDeGozo1(new Periodo(d(2021, 8, 1), d(2021, 8, 30)));
        c.getListaDeFerias().add(ferias);
        ServicoStub serv = new ServicoStub(c);

        Object[][] cartao = new Object[][] {
            { d(2020, 6, 1), "10.00" }, { d(2020, 7, 1), "12.00" }, { d(2020, 8, 1), "14.00" },
            { d(2020, 9, 1), "16.00" }, { d(2020, 10, 1), "18.00" }, { d(2020, 11, 1), "20.00" },
            { d(2020, 12, 1), "11.00" }, { d(2021, 1, 1), "13.00" }, { d(2021, 2, 1), "15.00" },
            { d(2021, 3, 1), "17.00" }, { d(2021, 4, 1), "19.00" }, { d(2021, 5, 1), "21.00" },
            { d(2021, 6, 1), "9.00" }, { d(2021, 7, 1), "8.00" }, { d(2021, 8, 1), "7.00" },
        };
        Calculada origem = novaOrigemHe(c, serv, d(2020, 6, 1), d(2022, 3, 10),
            BigDecimal.ZERO, "mq-ferias", cartao);
        origem.setGerarReflexo(TipoDeGeracaoEnum.DEVIDO);

        Reflexo r = new Reflexo(c);
        configurarFlags(r, "reflexo-ferias-mq");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.FERIAS);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.PERIODO_AQUISITIVO);
        r.setPeriodoInicial(d(2020, 6, 1));
        r.setPeriodoFinal(d(2022, 3, 10));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.MEDIA_PELA_QUANTIDADE);
        r.setPeriodoMediaReflexo(PeriodoDaMediaDoReflexoEnum.PERIODO_AQUISITIVO);
        r.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo f = (FormulaReflexo) r.getFormula();
        f.getBaseVerba().getItens().add(new ItemBaseVerba(f, origem, LogicoEnum.NAO));
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(BigDecimal.ONE);
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(BigDecimal.ONE);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        executar(r);
        dump("MQ_FERIAS", r);
    }

    public static void main(String[] args) throws Exception {
        // Contexto Seam mínimo só para o repositório de feriados consultado por HelperDate.
        Lifecycle.beginApplication(new HashMap<String, Object>());
        Lifecycle.beginCall();
        Contexts.getApplicationContext().set("repositorioDeFeriado", new FeriadoStub());

        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeFalta.class, new RepoFaltaStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeOcorrenciaDoCartaoDePonto.class, new RepoOcorrenciaCartaoStub());

        Calculo calculoComExcecaoDeSabado = novoCalculo(d(2020, 1, 10), null);
        LinkedHashSet<ExcecaoDoSabadoDoCalculo> excecoes = new LinkedHashSet<ExcecaoDoSabadoDoCalculo>();
        excecoes.add(new ExcecaoDoSabadoDoCalculo(calculoComExcecaoDeSabado, d(2021, 4, 10), d(2021, 4, 30)));
        calculoComExcecaoDeSabado.setExcecoesDoSabado(excecoes);

        calendario(calculoComExcecaoDeSabado);
        cargaHoraria();
        casoDsr();
        casoDsrComExclusoes();
        casoCartao();
        casoMqDevido();
        casoMqDiferenca();
        casoMqTratamentos();
        casoMqAbatimento();
        casoMqFerias();
    }
}
