import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.BaseDeJurosDasVerbasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.JurosDoAjuizamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.JurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.atualizacao.ParametrosDeAtualizacao;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.juros.ApuracaoDeJuros;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * Golden da apuração de juros de mora sobre as verbas (caso padrão BaseDeJurosDasVerbas=VERBAS,
 * juros habilitado). Dirige o método privado Calculo.apurarJurosDasVerbas via reflection sobre um
 * Calculo com verbas/ocorrências construídas à mão e regime fixo de 1% a.m. (dispensa faixas).
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenApuracaoJuros {

    static Date d(int ano, int mes, int dia) {
        return new GregorianCalendar(ano, mes - 1, dia).getTime();
    }

    static String comp(Date data) {
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        return String.format("%04d%02d", c.get(GregorianCalendar.YEAR), c.get(GregorianCalendar.MONTH) + 1);
    }

    static String fmtData(Date data) {
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        return String.format("%04d%02d%02d", c.get(GregorianCalendar.YEAR), c.get(GregorianCalendar.MONTH) + 1, c.get(GregorianCalendar.DAY_OF_MONTH));
    }

    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }

    static OcorrenciaDeVerba oc(VerbaDeCalculo verba, Date ini, Date fim, String devido, String pago, String indice) {
        OcorrenciaDeVerba o = new OcorrenciaDeVerba();
        o.setDataInicial(ini);
        o.setDataFinal(fim);
        o.setDevido(new BigDecimal(devido));
        o.setPago(new BigDecimal(pago));
        o.setIndiceAcumulado(new BigDecimal(indice));
        o.setAtivo(Boolean.TRUE);
        verba.adicionarEmOcorrencias(o);
        return o;
    }

    static VerbaDeCalculo verba(Calculo calc, CaracteristicaDaVerbaEnum caracteristica, JurosDoAjuizamentoEnum jaz) {
        Informada v = new Informada();
        v.setCalculo(calc);
        v.setAtivo(Boolean.TRUE);
        v.setComporPrincipal(LogicoEnum.SIM);
        v.setJurosDoAjuizamento(jaz);
        v.setCaracteristica(caracteristica);
        v.setIncidenciaINSS(Boolean.FALSE);
        v.setIncidenciaIRPF(Boolean.FALSE);
        v.setIncidenciaPrevidenciaPrivada(Boolean.FALSE);
        return v;
    }

    static class CalcStub extends Calculo {
        List<VerbaDeCalculo> ativas = new ArrayList<VerbaDeCalculo>();
        @Override public List<VerbaDeCalculo> getVerbasAtivas() { return ativas; }
    }

    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());

        ParametrosDeAtualizacao pa = new ParametrosDeAtualizacao();
        pa.setJuros(JurosEnum.JUROS_UM_PORCENTO);
        pa.setAplicarJurosFasePreJudicial(Boolean.FALSE);
        pa.setCombinarOutroJuros(Boolean.FALSE);
        pa.setListaDeCombinacaoDeJuros(new LinkedHashSet());
        pa.setBaseDeJurosDasVerbas(BaseDeJurosDasVerbasEnum.VERBAS);

        Date ajuizamento = d(2019, 6, 1);
        Date liquidacao = d(2021, 6, 1);

        CalcStub calc = new CalcStub();
        calc.setParametrosDeAtualizacao(pa);
        calc.setDataAjuizamento(ajuizamento);
        calc.setDataDeLiquidacao(liquidacao);
        calc.setDataAdmissao(d(2018, 1, 1));

        Set<VerbaDeCalculo> verbas = new LinkedHashSet<VerbaDeCalculo>();

        // Verba A (comum, vencidas): 03/2019 (venc antes do ajuizamento) e 08/2019 (venc depois).
        VerbaDeCalculo a = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(a, d(2019, 3, 1), d(2019, 3, 31), "1000.00", "0", "1.2");
        oc(a, d(2019, 8, 1), d(2019, 8, 31), "500.00", "0", "1.1");
        verbas.add(a);

        // Verba B (comum, vencidas): 03/2019 — mesma competência e mesmo pivô que A -> MESMO balde.
        VerbaDeCalculo b = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(b, d(2019, 3, 1), d(2019, 3, 31), "200.00", "0", "1.0");
        verbas.add(b);

        // Verba C (férias): venc = dataInicial (01/2020).
        VerbaDeCalculo c = verba(calc, CaracteristicaDaVerbaEnum.FERIAS, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(c, d(2020, 1, 1), d(2020, 1, 31), "3000.00", "500.00", "1.05");
        verbas.add(c);

        // Verba D (vincendas): pivô = ajuizamento, independentemente do vencimento.
        VerbaDeCalculo dd = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS_E_VINCENDAS);
        oc(dd, d(2020, 5, 1), d(2020, 5, 31), "800.00", "0", "1.0");
        verbas.add(dd);

        // Verba E (não compõe principal): deve ser ignorada.
        VerbaDeCalculo e = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        e.setComporPrincipal(LogicoEnum.NAO);
        oc(e, d(2019, 3, 1), d(2019, 3, 31), "9999.00", "0", "1.0");
        verbas.add(e);

        calc.setVerbas(verbas);
        calc.ativas.addAll(verbas);

        Method m = Calculo.class.getDeclaredMethod("apurarJurosDasVerbas");
        m.setAccessible(true);
        m.invoke(calc);

        for (ApuracaoDeJuros ap : calc.getApuracoesDeJuros()) {
            String balde = "balde_" + comp(ap.getCompetencia()) + "_" + fmtData(ap.getDataInicial());
            row(balde, "taxa", ap.getTaxaDeJuros());
            row(balde, "corrigido", ap.getValorCorrigido());
            row(balde, "juros", ap.getJuros());
        }
        row("TOTAIS", "corrigido", calc.getTotalDeValorCorrigidoDaApuracaoDeJuros());
        row("TOTAIS", "juros", calc.getTotalDeJurosDaApuracaoDeJuros());
    }

    static void row(String cenario, String chave, BigDecimal valor) {
        System.out.println(cenario + ";" + chave + ";" + p(valor));
    }
}
