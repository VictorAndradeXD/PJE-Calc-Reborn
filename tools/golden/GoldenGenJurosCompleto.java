import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.JurosDoAjuizamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.JurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeJurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeDeJurosBaseEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.TabelaDeJurosDoCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.atualizacao.ParametrosDeAtualizacao;
import br.jus.trt8.pjecalc.negocio.dominio.juros.JurosBase;
import br.jus.trt8.pjecalc.negocio.dominio.juros.padrao.JurosPadrao;
import br.jus.trt8.pjecalc.negocio.dominio.juros.padrao.RepositorioDeJurosPadrao;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

/** Golden dos regimes de alíquota fixa e da projeção da data inicial dos juros. */
public class GoldenGenJurosCompleto {
    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Connection cn;

    static Date d(String s) throws Exception { return ISO.parse(s); }
    static String f(Date d) { return ISO.format(d); }

    static class StubRepoJurosPadrao extends RepositorioDeJurosPadrao {
        @Override
        public List<? extends JurosBase> obterPeriodoDeJurosBase(Date dataInicio, Date dataFim) {
            List<JurosPadrao> faixas = new ArrayList<JurosPadrao>();
            String sql = "SELECT DDTINICIO, DDTFIM, RVLTAXA, STPJUROS, STPQUANTIDADE FROM TBJUROSPADRAO "
                + "WHERE DDTINICIO <= ? AND (DDTFIM IS NULL OR DDTFIM >= ?) ORDER BY DDTINICIO ASC";
            try (PreparedStatement ps = cn.prepareStatement(sql)) {
                ps.setDate(1, new java.sql.Date(dataFim.getTime()));
                ps.setDate(2, new java.sql.Date(dataInicio.getTime()));
                try (ResultSet rs = ps.executeQuery()) {
                    while (rs.next()) {
                        JurosPadrao j = new JurosPadrao();
                        j.setDataInicio(new Date(rs.getDate(1).getTime()));
                        j.setDataFim(rs.getDate(2) == null ? null : new Date(rs.getDate(2).getTime()));
                        j.setAliquota(rs.getBigDecimal(3));
                        j.setTipoDeJuros("C".equals(rs.getString(4)) ? TipoDeJurosEnum.COMPOSTOS : TipoDeJurosEnum.SIMPLES);
                        j.setTipoDeQuantidade("I".equals(rs.getString(5)) ? TipoDeQuantidadeDeJurosBaseEnum.INTEIRO : TipoDeQuantidadeDeJurosBaseEnum.FRACAO);
                        faixas.add(j);
                    }
                }
            } catch (Exception e) { throw new RuntimeException(e); }
            return faixas;
        }
    }

    static ParametrosDeAtualizacao params(JurosEnum regime, boolean preJudicial) {
        ParametrosDeAtualizacao pa = new ParametrosDeAtualizacao();
        pa.setJuros(regime);
        pa.setAplicarJurosFasePreJudicial(preJudicial);
        pa.setCombinarOutroJuros(Boolean.FALSE);
        pa.setListaDeCombinacaoDeJuros(new java.util.LinkedHashSet());
        return pa;
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/");
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeJurosPadrao.class, new StubRepoJurosPadrao());

        // ===== Seção A: regimes fixos (projetarData=false, janela [D, L]) =====
        System.out.println("### FIXO");
        System.out.println("regime;dataInicioJuros;dataLiquidacao;taxa");
        String[][] fixos = {
            {"JUROS_UM_PORCENTO",   "2015-02-01", "2020-12-01"},
            {"JUROS_UM_PORCENTO",   "2018-05-10", "2024-05-10"},
            {"JUROS_MEIO_PORCENTO", "2015-01-01", "2019-06-15"},
            {"JUROS_ZERO_TRINTA_TRES", "2020-01-01", "2020-03-31"},
        };
        for (String[] c : fixos) {
            JurosEnum regime = JurosEnum.valueOf(c[0]);
            Date ini = d(c[1]), liq = d(c[2]);
            Calculo calc = new Calculo();
            calc.setParametrosDeAtualizacao(params(regime, false));
            calc.setDataDeLiquidacao(liq);
            calc.setDataAjuizamento(ini);
            calc.setDataAdmissao(ini);
            TabelaDeJurosDoCalculo t = new TabelaDeJurosDoCalculo(calc, ini, liq);
            BigDecimal taxa = t.calcularTaxaDeJuros(ini, null, false, false);
            System.out.println(c[0] + ";" + c[1] + ";" + c[2] + ";" + taxa.toPlainString());
        }

        // ===== Seção B: projeção da data inicial (projetarData=true) =====
        System.out.println("### PROJ");
        System.out.println("regime;vencimento;ajuizamento;dataLiquidacao;inicioProjetado;taxa");
        // {regime, vencimento, ajuizamento, liquidacao}
        String[][] projs = {
            {"JUROS_UM_PORCENTO", "2016-03-15", "2016-01-10", "2020-01-01"}, // venc apos ajuiz -> mes seguinte
            {"JUROS_UM_PORCENTO", "2015-05-20", "2016-01-10", "2020-01-01"}, // venc antes ajuiz -> ajuiz+1
            {"JUROS_PADRAO",      "2016-03-15", "2016-01-10", "2020-01-01"},
            {"JUROS_PADRAO",      "2015-05-20", "2016-01-10", "2020-01-01"},
        };
        for (String[] c : projs) {
            JurosEnum regime = JurosEnum.valueOf(c[0]);
            Date venc = d(c[1]), ajuiz = d(c[2]), liq = d(c[3]);
            Calculo calc = new Calculo();
            calc.setParametrosDeAtualizacao(params(regime, false));
            calc.setDataDeLiquidacao(liq);
            calc.setDataAjuizamento(ajuiz);
            calc.setDataAdmissao(ajuiz);
            TabelaDeJurosDoCalculo t = new TabelaDeJurosDoCalculo(calc);
            BigDecimal taxa = t.calcularTaxaDeJuros(venc, null, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS, true, false);
            Date inicioProjetado = new Date(t.getDataInicialDeJuros().getTime());
            java.util.Calendar cal = java.util.Calendar.getInstance();
            cal.setTime(inicioProjetado); cal.add(java.util.Calendar.DAY_OF_MONTH, 1);
            System.out.println(c[0] + ";" + c[1] + ";" + c[2] + ";" + c[3] + ";" + f(cal.getTime()) + ";" + taxa.toPlainString());
        }
        cn.close();
    }
}
