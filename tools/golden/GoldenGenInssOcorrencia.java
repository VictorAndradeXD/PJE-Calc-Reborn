import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.atualizacao.ParametrosDeAtualizacao;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.inss.Inss;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.inss.sobresalarios.InssSobreSalariosDevidos;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.inss.sobresalarios.OcorrenciaDeInssSobreSalariosDevidos;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden da aplicação por ocorrência do INSS (OcorrenciaDeInss — getters puros):
 * correção (índice trabalhista x previdenciário), juros (truncados a 2 casas DOWN quando
 * previdenciário), multa (sem truncar) e total, para as 4 cotas.
 */
public class GoldenGenInssOcorrencia {
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    static OcorrenciaDeInssSobreSalariosDevidos montar(
            boolean previdenciario, String indTrab, String indPrev, String taxaJuros, String taxaMulta,
            String seg, String emp, String sat, String ter) throws Exception {
        ParametrosDeAtualizacao params = new ParametrosDeAtualizacao();
        params.setCorrecaoPrevidenciariaDosSalariosDevidosDoINSS(previdenciario);
        params.setCorrecaoTrabalhistaDosSalariosDevidosDoINSS(!previdenciario); // prev=true exige trab=false
        params.setLei11941(Boolean.FALSE);

        Calculo calc = new Calculo();
        calc.setParametrosDeAtualizacao(params);

        Inss inss = new Inss();
        inss.setCalculo(calc);

        InssSobreSalariosDevidos pai = new InssSobreSalariosDevidos();
        pai.setInss(inss);

        OcorrenciaDeInssSobreSalariosDevidos o = new OcorrenciaDeInssSobreSalariosDevidos();
        o.setInssSobreSalariosDevidos(pai);
        o.setDataOcorrenciaInss(d("2016-05-01"));
        o.setIndiceDeCorrecaoTrabalhistaUtilizado(b(indTrab));
        o.setIndiceDeCorrecaoPrevidenciariaUtilizado(b(indPrev));
        o.setTaxaDeJuros(b(taxaJuros));
        o.setTaxaDeMulta(b(taxaMulta));
        o.setValorDevidoSeguradoFinal(b(seg));
        o.setValorDevidoEmpresaFinal(b(emp));
        o.setValorDevidoSAT(b(sat));
        o.setValorDevidoTerceiros(b(ter));
        return o;
    }

    static void imprimir(String cenario, boolean prev, OcorrenciaDeInssSobreSalariosDevidos o) {
        linha(cenario, prev, "segurado", o.getValorDevidoSeguradoFinalCorrigido(), o.getJurosValorDevidoSeguradoFinal(), o.getMultaValorDevidoSeguradoFinal(), o.getTotalValorDevidoSeguradoFinal());
        linha(cenario, prev, "empresa",  o.getValorDevidoEmpresaFinalCorrigido(),  o.getJurosValorDevidoEmpresaFinal(),  o.getMultaValorDevidoEmpresaFinal(),  o.getTotalValorDevidoEmpresaFinal());
        linha(cenario, prev, "sat",       o.getValorDevidoSATCorrigido(),           o.getJurosValorDevidoSAT(),           o.getMultaValorDevidoSAT(),           o.getTotalValorDevidoSAT());
        linha(cenario, prev, "terceiros", o.getValorDevidoTerceirosCorrigido(),     o.getJurosValorDevidoTerceiros(),     o.getMultaValorDevidoTerceiros(),     o.getTotalValorDevidoTerceiros());
    }

    static void linha(String cenario, boolean prev, String cota, BigDecimal corr, BigDecimal juros, BigDecimal multa, BigDecimal total) {
        System.out.println(cenario + ";" + prev + ";" + cota + ";" + corr.toPlainString() + ";" + juros.toPlainString() + ";" + multa.toPlainString() + ";" + total.toPlainString());
    }

    public static void main(String[] args) throws Exception {
        System.out.println("cenario;previdenciario;cota;corrigido;juros;multa;total");
        // Previdenciário: correção prev 1.35 (trab 1), juros truncam DOWN, multa 8%.
        imprimir("previdenciario", true, montar(true, "1", "1.35", "12.5", "8", "200.00", "300.33", "15.07", "57.19"));
        // Não previdenciário: correção trab 1.2 (prev 1), juros SEM truncamento, multa 0.
        imprimir("trabalhista", false, montar(false, "1.2", "1", "12.5", "0", "200.00", "300.33", "15.07", "57.19"));
    }
}
