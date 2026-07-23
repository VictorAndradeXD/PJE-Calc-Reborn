import br.jus.trt8.pjecalc.negocio.constantes.AliquotaDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.constantes.IncidenciaDeMultaDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeBaseDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCorrecaoDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.constantes.ValorDaMultaDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.OcorrenciaDeFgts;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

/** Golden dos totais agregados do FGTS e da multa (20%/40%, art. 467, multa 10%). */
public class GoldenGenFgtsMulta {
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    static OcorrenciaDeFgts oc(String comp, String baseHist, String baseVerba, String baseSemAviso,
                               String depositado, String indAcum, String indMulta, String taxa) throws Exception {
        OcorrenciaDeFgts o = new OcorrenciaDeFgts();
        o.setOcorrencia(d(comp));
        o.setBaseHistorico(b(baseHist));
        o.setBaseVerba(b(baseVerba));
        o.setBaseVerbaSemAvisoPrevio(b(baseSemAviso));
        o.setAliquotaDoFgtsEnum(AliquotaDoFgtsEnum.OITO_POR_CENTO);
        o.setDepositado(b(depositado));
        o.setIndiceAcumulado(b(indAcum));
        o.setIndiceAcumuladoDaMulta(b(indMulta));
        o.setTaxaDeJuros(b(taxa));
        return o;
    }

    static java.util.Set<OcorrenciaDeFgts> ocorrencias() throws Exception {
        java.util.Set<OcorrenciaDeFgts> l = new java.util.LinkedHashSet<OcorrenciaDeFgts>();
        l.add(oc("2015-03-01", "1000.00", "200.00", "100.00", "0.00",  "1.50", "1.20", "12"));
        l.add(oc("2015-04-01", "1000.00", "0.00",   "0.00",   "20.00", "1.45", "1.18", "11.5"));
        l.add(oc("2015-05-01", "1500.00", "300.00", "150.00", "0.00",  "1.40", "1.15", "11"));
        return l;
    }

    static Fgts montar(IncidenciaDeMultaDoFgtsEnum inc, ValorDaMultaDoFgtsEnum pct,
                       boolean excluirAviso, boolean m467, boolean m10) throws Exception {
        Fgts f = new Fgts();
        f.setOcorrencias(ocorrencias());
        f.setMulta(Boolean.TRUE);
        f.setTipoDoValorDaMulta(TipoDeBaseDoFgtsEnum.CALCULADA);
        f.setMultaDoFgts(pct);
        f.setIncidenciaDoFgts(inc);
        f.setExcluirAvisoDaMulta(excluirAviso);
        f.setIndiceMulta(b("1.25"));
        f.setIndiceMulta467(b("1.30"));
        f.setMultaDoArtigo467(m467);
        f.setMulta10(m10);
        return f;
    }

    public static void main(String[] args) throws Exception {
        TipoDeCorrecaoDoFgtsEnum liq = TipoDeCorrecaoDoFgtsEnum.PELA_DATA_DE_LIQUIDACAO;
        TipoDeCorrecaoDoFgtsEnum dem = TipoDeCorrecaoDoFgtsEnum.PELA_DATA_DE_DEMISSAO;

        // Totais agregados (independentes da incidência)
        Fgts base = montar(IncidenciaDeMultaDoFgtsEnum.SOBRE_O_TOTAL_DEVIDO, ValorDaMultaDoFgtsEnum.QUARENTA_POR_CENTO, true, false, false);
        System.out.println("### TOTAIS");
        System.out.println("campo;valor");
        System.out.println("totalDevido;" + base.getTotalDevido().toPlainString());
        System.out.println("totalDevidoCorrigidoLiq;" + base.getTotalDevidoCorrigido(liq).toPlainString());
        System.out.println("totalDevidoSemAvisoCorrigidoLiq;" + base.getTotalDevidoSemAvisoCorrigido(liq).toPlainString());
        System.out.println("totalDiferenca;" + base.getTotalDaDiferenca().toPlainString());
        System.out.println("totalDiferencaCorrigidaLiq;" + base.getTotalDaDiferencaCorrigida(liq).toPlainString());
        System.out.println("totalDiferencaSemAvisoCorrigidaLiq;" + base.getTotalDaDiferencaSemAvisoCorrigida(liq).toPlainString());
        System.out.println("totalJurosLiq;" + base.getTotalDeJurosDoFgts(liq).toPlainString());
        System.out.println("totalDevidoCorrigidoDem;" + base.getTotalDevidoCorrigido(dem).toPlainString());

        // Multa por incidência
        System.out.println("### MULTA");
        System.out.println("incidencia;percentual;excluirAviso;multa467;multa10;baseMulta;valorMulta;multaCorrigida;valor467;valor10");
        Object[][] cenarios = {
            {IncidenciaDeMultaDoFgtsEnum.SOBRE_O_TOTAL_DEVIDO, ValorDaMultaDoFgtsEnum.QUARENTA_POR_CENTO, Boolean.TRUE,  Boolean.TRUE,  Boolean.TRUE},
            {IncidenciaDeMultaDoFgtsEnum.SOBRE_O_TOTAL_DEVIDO, ValorDaMultaDoFgtsEnum.QUARENTA_POR_CENTO, Boolean.FALSE, Boolean.FALSE, Boolean.FALSE},
            {IncidenciaDeMultaDoFgtsEnum.SOBRE_O_TOTAL_DEVIDO, ValorDaMultaDoFgtsEnum.VINTE_POR_CENTO,    Boolean.TRUE,  Boolean.FALSE, Boolean.FALSE},
            {IncidenciaDeMultaDoFgtsEnum.SOBRE_DIFERENCA,      ValorDaMultaDoFgtsEnum.QUARENTA_POR_CENTO, Boolean.TRUE,  Boolean.FALSE, Boolean.FALSE},
            {IncidenciaDeMultaDoFgtsEnum.SOBRE_DIFERENCA,      ValorDaMultaDoFgtsEnum.QUARENTA_POR_CENTO, Boolean.FALSE, Boolean.FALSE, Boolean.FALSE},
        };
        for (Object[] c : cenarios) {
            IncidenciaDeMultaDoFgtsEnum inc = (IncidenciaDeMultaDoFgtsEnum) c[0];
            ValorDaMultaDoFgtsEnum pct = (ValorDaMultaDoFgtsEnum) c[1];
            boolean exc = (Boolean) c[2], m467 = (Boolean) c[3], m10 = (Boolean) c[4];
            Fgts f = montar(inc, pct, exc, m467, m10);
            System.out.println(inc.name() + ";" + pct.name() + ";" + exc + ";" + m467 + ";" + m10
                + ";" + f.getValorBaseParaMultaDoFgts().toPlainString()
                + ";" + f.getValorDaMultaDoFgts().toPlainString()
                + ";" + f.getValorDaMultaDoFgtsCorrigido().toPlainString()
                + ";" + f.getValorDaMultaDoArtigo467().toPlainString()
                + ";" + f.getValorDaMulta10().toPlainString());
        }
    }
}
