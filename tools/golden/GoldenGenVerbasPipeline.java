import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.BaseDeCalculoDoPrincipalEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.ComportamentoDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.DivisorDeVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.OcorrenciaDePagamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.PeriodoDaMediaDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeGeracaoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoValorPagoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TratamentoDaFracaoDeMesDoReflexoEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.RepositorioDeCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.ferias.Ferias;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaInformada;
import br.jus.trt8.pjecalc.negocio.dominio.formula.FormulaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.historicosalarial.HistoricoSalarial;
import br.jus.trt8.pjecalc.negocio.dominio.historicosalarial.OcorrenciaDoHistoricoSalarial;
import br.jus.trt8.pjecalc.negocio.dominio.historicosalarial.RepositorioDeHistoricoSalarial;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.termo.BaseTabelada;
import br.jus.trt8.pjecalc.negocio.dominio.termo.ItemBaseVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Calculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.HistoricoSalarialDaVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaCalculada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaInformada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.MaquinaDeCalculoDaVerbaReflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Reflexo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.TabelaDeCorrecaoMonetaria;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;
import br.jus.trt8.pjecalc.negocio.constantes.TipoVinculoDeVerbaEnum;
import br.jus.trt8.pjecalc.negocio.servicos.ServicoDeCalculo;

import java.math.BigDecimal;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.Set;

/**
 * Golden do pipeline de verbas: geração de ocorrências (MENSAL/DESLIGAMENTO/DEZEMBRO),
 * termos, liquidação e reflexos (VALOR_MENSAL e MÉDIA PELO VALOR).
 * Índice de correção estubado: fator(aaaa-mm) = 1 + 0.01*((ano-2019)*12 + mes)  [ex.: 2021-03 -> 1.27]
 */
public class GoldenGenVerbasPipeline {

    // ---------- stubs ----------

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

    static class RepoHistoricoStub extends RepositorioDeHistoricoSalarial {
        HistoricoSalarial instancia;
        RepoHistoricoStub(HistoricoSalarial h) { this.instancia = h; }
        @Override public HistoricoSalarial obter(Object id) { return instancia; }
    }

    static class ServicoStub extends ServicoDeCalculo {
        Calculo calculo;
        ServicoStub(Calculo c) { this.calculo = c; }
        @Override public Calculo obterCalculoAberto() { return calculo; }
        @Override public Set<Ferias> obterFeriasDoCalculo() { return new HashSet<Ferias>(); }
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

    static String fmtData(Date data) {
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        return String.format("%04d-%02d-%02d",
            c.get(GregorianCalendar.YEAR), c.get(GregorianCalendar.MONTH) + 1, c.get(GregorianCalendar.DAY_OF_MONTH));
    }

    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }
    static String p(Boolean v) { return v == null ? "" : (v ? "1" : "0"); }

    static Calculo novoCalculo(Date admissao, Date demissao, Date liquidacao) {
        Calculo c = new Calculo();
        c.setDataAdmissao(admissao);
        c.setDataDemissao(demissao);
        c.setDataAjuizamento(d(2022, 1, 15));
        c.setDataDeLiquidacao(liquidacao);
        c.setZeraValorNegativo(Boolean.TRUE);
        c.setOrdem(0);
        Utils.adicionarRepositorioParaTeste(RepositorioDeCalculo.class, new RepoCalculoStub(c));
        return c;
    }

    static void configurarFlags(VerbaDeCalculo v) {
        v.setNome("verba-teste");
        v.setExcluirFeriasGozadas(Boolean.FALSE);
        v.setExcluirFaltaJustificada(Boolean.FALSE);
        v.setExcluirFaltaNaoJustificada(Boolean.FALSE);
        v.setZeraValorNegativo(Boolean.TRUE);
        v.setComporPrincipal(LogicoEnum.SIM);
        v.setGerarPrincipal(TipoDeGeracaoEnum.DIFERENCA);
        v.setGerarReflexo(TipoDeGeracaoEnum.DIFERENCA);
        v.setAplicarProporcionalidade(Boolean.FALSE);
    }

    static void dump(String caso, VerbaDeCalculo v) {
        for (OcorrenciaDeVerba o : v.getOcorrencias()) {
            System.out.println("OC;" + caso + ";" + fmtData(o.getDataInicial()) + ";" + fmtData(o.getDataFinal())
                + ";" + p(o.getAtivo()) + ";" + p(o.getBase()) + ";" + p(o.getDivisor()) + ";" + p(o.getMultiplicador())
                + ";" + p(o.getQuantidade()) + ";" + p(o.getQuantidadeIntegral()) + ";" + p(o.getDevido())
                + ";" + p(o.getDevidoIntegral()) + ";" + p(o.getPago()) + ";" + p(o.getPagoIntegral())
                + ";" + p(o.getDobra()) + ";" + p(o.getIndiceAcumulado()) + ";" + p(o.getDiferenca())
                + ";" + p(o.getDiferencaCorrigida()));
        }
        System.out.println("TOT;" + caso + ";" + p(v.getValorTotalDevido()) + ";" + p(v.getValorTotalPago())
            + ";" + p(v.getValorTotalDiferenca()) + ";" + p(v.getValorTotalDiferencaCorrigida())
            + ";" + p(v.getValorTotalDiferencaCorrigidaParaCalculoDasIncidencias()));
    }

    // ---------- casos ----------

    /** Informada MENSAL com proporcionalidade: meses parciais no início e no fim. */
    static void casoInformadaProporcional() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        ServicoStub serv = new ServicoStub(c);
        Informada v = new Informada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 15));
        v.setPeriodoFinal(d(2021, 4, 10));
        v.setAplicarProporcionalidade(Boolean.TRUE);
        FormulaInformada f = (FormulaInformada) v.getFormula();
        f.getConstante().setValor(new BigDecimal("3000.00"));
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(new BigDecimal("500.00"));
        f.getValorPago().setAplicarProporcionalidade(Boolean.TRUE);
        v.setMaquinaDeCalculorencias(new MaqInformada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("INF_PROP", v);
    }

    /** Informada MENSAL sem proporcionalidade: valor cheio mesmo em mês parcial. */
    static void casoInformadaCheia() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        ServicoStub serv = new ServicoStub(c);
        Informada v = new Informada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 15));
        v.setPeriodoFinal(d(2021, 3, 10));
        FormulaInformada f = (FormulaInformada) v.getFormula();
        f.getConstante().setValor(new BigDecimal("2500.00"));
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqInformada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("INF_CHEIA", v);
    }

    /** Calculada MENSAL, base = última remuneração com proporcionalidade, termos completos. */
    static Calculada casoCalculadaUltimaRemuneracao() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3300.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 10));
        v.setPeriodoFinal(d(2021, 5, 20));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.TRUE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("220"));
        f.getMultiplicador().setOutroValor(new BigDecimal("1.5"));
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("20"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.TRUE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(new BigDecimal("100.00"));
        f.getValorPago().setAplicarProporcionalidade(Boolean.FALSE);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("CALC_UR", v);
        return v;
    }

    /** Calculada MENSAL, base = histórico salarial (competência sem ocorrência -> base 0). */
    static void casoCalculadaHistorico() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        ServicoStub serv = new ServicoStub(c);

        HistoricoSalarial h = new HistoricoSalarial();
        h.getOcorrencias().add(new OcorrenciaDoHistoricoSalarial(h, d(2021, 1, 1), new BigDecimal("3000.00"), null, null, null, null));
        h.getOcorrencias().add(new OcorrenciaDoHistoricoSalarial(h, d(2021, 2, 1), new BigDecimal("3000.00"), null, null, null, null));
        h.getOcorrencias().add(new OcorrenciaDoHistoricoSalarial(h, d(2021, 3, 1), new BigDecimal("3300.00"), null, null, null, null));
        // abril sem ocorrência de propósito
        Utils.adicionarRepositorioParaTeste(RepositorioDeHistoricoSalarial.class, new RepoHistoricoStub(h));

        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 1));
        v.setPeriodoFinal(d(2021, 4, 30));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.HISTORICO_SALARIAL));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        LinkedHashSet<HistoricoSalarialDaVerba> vinculos = new LinkedHashSet<HistoricoSalarialDaVerba>();
        vinculos.add(new HistoricoSalarialDaVerba(v, h, TipoVinculoDeVerbaEnum.BASE, Boolean.FALSE));
        v.adicionarHistoricosVinculadosAtravesDoValorDevido(vinculos);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("30"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("30"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("CALC_HS", v);
    }

    /** 13º (DEZEMBRO) com quantidade em AVOS; demissão 28/12/2021 (dia > 20). */
    static void casoDezembroAvos() throws Exception {
        Calculo c = novoCalculo(d(2019, 3, 20), d(2021, 12, 28), d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DEZEMBRO);
        v.setPeriodoInicial(d(2019, 3, 20));
        v.setPeriodoFinal(d(2021, 12, 28));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("12"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.AVOS);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("DEZ_AVOS", v);
    }

    /** 13º com demissão fora de dezembro (15/07/2021): ocorrência extra no dia da demissão. */
    static void casoDezembroDemissaoMeioDoAno() throws Exception {
        Calculo c = novoCalculo(d(2019, 3, 20), d(2021, 7, 15), d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DEZEMBRO);
        v.setPeriodoInicial(d(2019, 3, 20));
        v.setPeriodoFinal(d(2021, 7, 15));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("12"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.AVOS);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("DEZ_DEMISSAO_JUL", v);
    }

    /** Saldo de salário: verba COMUM com pagamento no DESLIGAMENTO (demissão 17/08/2021). */
    static void casoDesligamento() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), d(2021, 8, 17), d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DESLIGAMENTO);
        v.setPeriodoInicial(d(2020, 5, 1));
        v.setPeriodoFinal(d(2021, 8, 17));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.TRUE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("30"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("30"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.TRUE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("DESLIG", v);
    }

    /** Reflexo VALOR_MENSAL sobre a diferença da verba CALC_UR (já liquidada). */
    static void casoReflexoValorMensal(Calculada origem) throws Exception {
        Calculo c = origem.getCalculo();
        ServicoStub serv = new ServicoStub(c);
        Reflexo r = new Reflexo(c);
        configurarFlags(r);
        r.setNome("reflexo-vm");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        r.setPeriodoInicial(d(2021, 1, 10));
        r.setPeriodoFinal(d(2021, 5, 20));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.VALOR_MENSAL);
        r.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo f = (FormulaReflexo) r.getFormula();
        f.getBaseVerba().getItens().add(new ItemBaseVerba(f, origem, LogicoEnum.NAO));
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("6"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(BigDecimal.ONE);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        r.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        r.gerarOcorrencias(false);
        r.liquidar();
        dump("REFLEXO_VM", r);
    }

    /**
     * Reflexo MÉDIA PELO VALOR (ANO_CIVIL) no 13º: origem informada com mês parcial
     * (jan começa dia 15). Um subcaso por tratamento da fração de mês.
     */
    static void casoReflexoMediaPeloValor(TratamentoDaFracaoDeMesDoReflexoEnum tratamento, String caso) throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), null, d(2022, 3, 31));
        ServicoStub serv = new ServicoStub(c);

        Informada origem = new Informada(c);
        configurarFlags(origem);
        origem.setNome("origem-he");
        origem.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        origem.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        origem.setPeriodoInicial(d(2021, 1, 15));
        origem.setPeriodoFinal(d(2021, 12, 31));
        origem.setAplicarProporcionalidade(Boolean.TRUE);
        FormulaInformada fo = (FormulaInformada) origem.getFormula();
        fo.getConstante().setValor(new BigDecimal("1200.00"));
        fo.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        fo.getValorPago().setValorInformado(BigDecimal.ZERO);
        origem.setMaquinaDeCalculorencias(new MaqInformada(origem, serv));
        origem.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        origem.gerarOcorrencias(false);
        origem.liquidar();

        Reflexo r = new Reflexo(c);
        configurarFlags(r);
        r.setNome("reflexo-13-media");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DEZEMBRO);
        r.setPeriodoInicial(d(2021, 1, 1));
        r.setPeriodoFinal(d(2021, 12, 31));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.MEDIA_PELO_VALOR);
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
        r.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        r.gerarOcorrencias(false);
        r.liquidar();
        dump(caso, r);
    }

    /** Pago maior que o devido em meses parciais: com e sem zerar diferença negativa. */
    static void casoPagoMaiorQueDevido(boolean zerar, String caso) throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3300.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setZeraValorNegativo(zerar);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 10));
        v.setPeriodoFinal(d(2021, 3, 31));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.TRUE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("220"));
        f.getMultiplicador().setOutroValor(new BigDecimal("1.5"));
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("20"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.TRUE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(new BigDecimal("300.00"));
        f.getValorPago().setAplicarProporcionalidade(Boolean.FALSE);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump(caso, v);
    }

    /** Divisor OUTRO_VALOR = 0 desativa a ocorrência na geração. */
    static void casoDivisorZero() throws Exception {
        Calculo c = novoCalculo(d(2020, 5, 1), null, d(2022, 3, 31));
        c.setValorUltimaRemuneracao(new BigDecimal("3000.00"));
        ServicoStub serv = new ServicoStub(c);
        Calculada v = new Calculada(c);
        configurarFlags(v);
        v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        v.setPeriodoInicial(d(2021, 1, 1));
        v.setPeriodoFinal(d(2021, 2, 28));
        FormulaCalculada f = (FormulaCalculada) v.getFormula();
        f.setBaseTabelada(new BaseTabelada(BaseDeCalculoDoPrincipalEnum.ULTIMA_REMUNERACAO));
        f.getBaseTabelada().setAplicarProporcionalidade(Boolean.FALSE);
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(BigDecimal.ZERO);
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(BigDecimal.ONE);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        v.setMaquinaDeCalculorencias(new MaqCalculada(v, serv));
        v.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        v.gerarOcorrencias(false);
        v.liquidar();
        dump("DIV0", v);
    }

    /** Reflexo VALOR_MENSAL com dobra na fórmula (devido x2). */
    static void casoReflexoDobra(Calculada origem) throws Exception {
        Calculo c = origem.getCalculo();
        ServicoStub serv = new ServicoStub(c);
        Reflexo r = new Reflexo(c);
        configurarFlags(r);
        r.setNome("reflexo-dobra");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        r.setPeriodoInicial(d(2021, 2, 1));
        r.setPeriodoFinal(d(2021, 3, 31));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.VALOR_MENSAL);
        r.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo f = (FormulaReflexo) r.getFormula();
        f.getBaseVerba().getItens().add(new ItemBaseVerba(f, origem, LogicoEnum.NAO));
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("6"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(BigDecimal.ONE);
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.setDobra(Boolean.TRUE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        r.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        r.gerarOcorrencias(false);
        r.liquidar();
        dump("REFLEXO_VM_DOBRA", r);
    }

    /** Média pelos ÚLTIMOS 12 MESES DO CONTRATO, com demissão em 20/10/2021 (aviso prévio típico). */
    static void casoReflexoMediaUltimosDozeMeses() throws Exception {
        Calculo c = novoCalculo(d(2020, 1, 10), d(2021, 10, 20), d(2022, 3, 31));
        ServicoStub serv = new ServicoStub(c);

        Informada origem = new Informada(c);
        configurarFlags(origem);
        origem.setNome("origem-he");
        origem.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        origem.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.MENSAL);
        origem.setPeriodoInicial(d(2020, 6, 1));
        origem.setPeriodoFinal(d(2021, 10, 20));
        origem.setAplicarProporcionalidade(Boolean.TRUE);
        FormulaInformada fo = (FormulaInformada) origem.getFormula();
        fo.getConstante().setValor(new BigDecimal("900.00"));
        fo.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        fo.getValorPago().setValorInformado(BigDecimal.ZERO);
        origem.setMaquinaDeCalculorencias(new MaqInformada(origem, serv));
        origem.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        origem.gerarOcorrencias(false);
        origem.liquidar();
        dump("ORIGEM_DM", origem);

        Reflexo r = new Reflexo(c);
        configurarFlags(r);
        r.setNome("reflexo-aviso-media");
        r.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        r.setOcorrenciaDePagamento(OcorrenciaDePagamentoEnum.DESLIGAMENTO);
        r.setPeriodoInicial(d(2020, 6, 1));
        r.setPeriodoFinal(d(2021, 10, 20));
        r.setComportamentoDoReflexo(ComportamentoDoReflexoEnum.MEDIA_PELO_VALOR);
        r.setPeriodoMediaReflexo(PeriodoDaMediaDoReflexoEnum.ULTIMOS_DOZE_MESES_DO_CONTRATO);
        r.setTratamentoDaFracaoDeMesDoReflexo(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER);
        FormulaReflexo f = (FormulaReflexo) r.getFormula();
        f.getBaseVerba().getItens().add(new ItemBaseVerba(f, origem, LogicoEnum.NAO));
        f.getDivisor().setTipo(DivisorDeVerbaEnum.OUTRO_VALOR);
        f.getDivisor().setOutroValor(new BigDecimal("30"));
        f.getMultiplicador().setOutroValor(BigDecimal.ONE);
        f.getQuantidade().setTipo(TipoDeQuantidadeEnum.INFORMADA);
        f.getQuantidade().setValorInformado(new BigDecimal("30"));
        f.getQuantidade().setAplicarProporcionalidade(Boolean.FALSE);
        f.getValorPago().setTipo(TipoValorPagoEnum.INFORMADO);
        f.getValorPago().setValorInformado(BigDecimal.ZERO);
        r.setMaquinaDeCalculorencias(new MaqReflexo(r, serv));
        r.setTabelaDeCorrecaoMonetariaTrabalhista(new TabelaStub());
        r.gerarOcorrencias(false);
        r.liquidar();
        dump("REFLEXO_MV_DM", r);
    }

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());

        casoInformadaProporcional();
        casoInformadaCheia();
        Calculada origem = casoCalculadaUltimaRemuneracao();
        casoCalculadaHistorico();
        casoDezembroAvos();
        casoDezembroDemissaoMeioDoAno();
        casoDesligamento();
        casoReflexoValorMensal(origem);
        casoReflexoMediaPeloValor(TratamentoDaFracaoDeMesDoReflexoEnum.MANTER, "REFLEXO_MV_MANTER");
        casoReflexoMediaPeloValor(TratamentoDaFracaoDeMesDoReflexoEnum.DESPREZAR, "REFLEXO_MV_DESPREZAR");
        casoReflexoMediaPeloValor(TratamentoDaFracaoDeMesDoReflexoEnum.DESPREZAR_MENOR_QUE_15_DIAS, "REFLEXO_MV_DMQ15");
        casoReflexoMediaPeloValor(TratamentoDaFracaoDeMesDoReflexoEnum.INTEGRALIZAR, "REFLEXO_MV_INTEGRALIZAR");
        casoPagoMaiorQueDevido(true, "CALC_ZERA");
        casoPagoMaiorQueDevido(false, "CALC_NOZERA");
        casoDivisorZero();
        casoReflexoDobra(origem);
        casoReflexoMediaUltimosDozeMeses();
    }
}
