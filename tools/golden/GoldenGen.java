import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculadorDeIndices;
import br.jus.trt8.pjecalc.negocio.dominio.indices.api.IndiceDeCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.indices.ipcae.IndiceIPCAE;
import br.jus.trt8.pjecalc.negocio.dominio.indices.ipca.IndiceIPCA;
import br.jus.trt8.pjecalc.negocio.dominio.indices.inpc.IndiceINPC;
import br.jus.trt8.pjecalc.negocio.dominio.indices.igpm.IndiceIGPM;
import br.jus.trt8.pjecalc.negocio.dominio.indices.tr.IndiceTR;
import br.jus.trt8.pjecalc.negocio.dominio.indices.selic.IndiceSelicFazenda;
import br.jus.trt8.pjecalc.base.comum.Utils;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.*;

/**
 * Gera golden values de correção monetária usando o motor oficial do PJe-Calc.
 * Regime: mês do vencimento. Modos:
 *   NORMAL      -> calcularIndiceAcumulado (multiplicativo, com conversão de moeda)
 *   SOMAS       -> calcularIndiceAcumuladoComSomas (aditivo, SELIC)
 *   IGNORA_NEG  -> obterTabelaDeIndicesIgnorandoTaxasNegativas
 * Saída: "indice;valor;vencimento;liquidacao;ignorarNegativa;fator;corrigido".
 */
public class GoldenGen {

    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");

    enum Modo { NORMAL, SOMAS, IGNORA_NEG }

    static class Caso {
        String indice, tabela, venc, liq; BigDecimal valor; Modo modo;
        Caso(String indice, String tabela, String valor, String venc, String liq, Modo modo) {
            this.indice = indice; this.tabela = tabela; this.valor = new BigDecimal(valor);
            this.venc = venc; this.liq = liq; this.modo = modo;
        }
    }

    static Date parse(String s) throws Exception { return ISO.parse(s); }

    static Date firstDayOfMonth(Date d) {
        Calendar c = Calendar.getInstance();
        c.setTime(d);
        c.set(c.get(Calendar.YEAR), c.get(Calendar.MONTH), 1, 0, 0, 0);
        c.set(Calendar.MILLISECOND, 0);
        return c.getTime();
    }

    static IndiceDeCalculo novoIndice(String tabela, Date comp, BigDecimal taxa) {
        if ("TBIPCAE".equals(tabela)) return new IndiceIPCAE(comp, taxa);
        if ("TBIPCA".equals(tabela))  return new IndiceIPCA(comp, taxa);
        if ("TBINPC".equals(tabela))  return new IndiceINPC(comp, taxa);
        if ("TBIGPM".equals(tabela))  return new IndiceIGPM(comp, taxa);
        if ("TBTR".equals(tabela))    return new IndiceTR(comp, taxa);
        if ("TBSELICFAZENDA".equals(tabela)) return new IndiceSelicFazenda(comp, taxa);
        throw new IllegalArgumentException("tabela desconhecida: " + tabela);
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");

        List<Caso> casos = Arrays.asList(
            // Base (regra padrão, pós-1994)
            new Caso("IPCAE", "TBIPCAE", "1000.00", "2015-01-15", "2020-12-20", Modo.NORMAL),
            new Caso("IPCAE", "TBIPCAE", "5000.00", "2010-06-10", "2023-06-10", Modo.NORMAL),
            new Caso("IPCAE", "TBIPCAE", "100.00",  "2019-11-15", "2019-12-15", Modo.NORMAL),
            new Caso("IPCA",  "TBIPCA",  "3333.33", "2018-05-10", "2024-05-10", Modo.NORMAL),
            new Caso("INPC",  "TBINPC",  "1000.00", "2012-01-10", "2022-01-10", Modo.NORMAL),
            new Caso("IGPM",  "TBIGPM",  "1000.00", "2016-01-10", "2021-01-10", Modo.NORMAL),
            new Caso("TR",    "TBTR",    "2500.00", "2000-03-01", "2010-03-01", Modo.NORMAL),
            new Caso("TR",    "TBTR",    "1000.00", "2015-01-15", "2020-12-20", Modo.NORMAL),
            // Conversão de moeda (atravessa cortes históricos)
            new Caso("TR",    "TBTR",    "1000.00", "1985-01-15", "1995-06-20", Modo.NORMAL),
            new Caso("TR",    "TBTR",    "1000.00", "1990-06-10", "2000-06-10", Modo.NORMAL),
            new Caso("TR",    "TBTR",    "1000.00", "1993-06-10", "1996-06-10", Modo.NORMAL),
            new Caso("IPCAE", "TBIPCAE", "1000.00", "1992-03-15", "2000-03-20", Modo.NORMAL),
            // SELIC (acumulação aditiva)
            new Caso("SelicFazenda", "TBSELICFAZENDA", "1000.00", "2010-01-10", "2020-01-10", Modo.SOMAS),
            new Caso("SelicFazenda", "TBSELICFAZENDA", "7777.77", "2015-06-15", "2023-06-15", Modo.SOMAS),
            // Ignorar taxa negativa (período com meses deflacionários)
            new Caso("IGPM",  "TBIGPM",  "1000.00", "1998-01-10", "2003-01-10", Modo.IGNORA_NEG),
            new Caso("IPCA",  "TBIPCA",  "1000.00", "1997-01-10", "2002-01-10", Modo.IGNORA_NEG)
        );

        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/")) {
            System.out.println("indice;valor;vencimento;liquidacao;ignorarNegativa;fator;corrigido");
            for (Caso caso : casos) {
                Date vencMes = firstDayOfMonth(parse(caso.venc));
                Date liqMes  = firstDayOfMonth(parse(caso.liq));

                List<IndiceDeCalculo> lista = new ArrayList<IndiceDeCalculo>();
                String sql = "SELECT DDTCOMPETENCIAINDICE, RVLINDICE FROM " + caso.tabela
                    + " WHERE DDTCOMPETENCIAINDICE BETWEEN ? AND ? ORDER BY DDTCOMPETENCIAINDICE DESC";
                try (PreparedStatement ps = cn.prepareStatement(sql)) {
                    ps.setDate(1, new java.sql.Date(vencMes.getTime()));
                    ps.setDate(2, new java.sql.Date(liqMes.getTime()));
                    try (ResultSet rs = ps.executeQuery()) {
                        while (rs.next()) {
                            Date comp = new Date(rs.getDate(1).getTime());
                            lista.add(novoIndice(caso.tabela, comp, rs.getBigDecimal(2)));
                        }
                    }
                }

                List<IndiceDeCalculo> acum;
                boolean ignorarNeg = false;
                switch (caso.modo) {
                    case SOMAS:      acum = CalculadorDeIndices.calcularIndiceAcumuladoComSomas(lista, Boolean.FALSE); break;
                    case IGNORA_NEG: acum = CalculadorDeIndices.obterTabelaDeIndicesIgnorandoTaxasNegativas(lista); ignorarNeg = true; break;
                    default:         acum = CalculadorDeIndices.calcularIndiceAcumulado(lista);
                }

                BigDecimal fator = null;
                for (IndiceDeCalculo i : acum) {
                    if (i.getCompetencia().equals(vencMes)) { fator = i.getValorAcumulado(); break; }
                }
                BigDecimal corrigido = Utils.aplicarCorrecaoMonetaria(fator, caso.valor);

                System.out.println(caso.indice + ";" + caso.valor.toPlainString() + ";" + caso.venc + ";" + caso.liq
                    + ";" + ignorarNeg + ";" + (fator == null ? "NULL" : fator.toPlainString())
                    + ";" + corrigido.toPlainString());
            }
        }
    }
}
