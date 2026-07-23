import br.jus.trt8.pjecalc.base.comum.Utils;
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

/**
 * Golden da taxa acumulada de juros (JurosPadrão) dirigindo o motor REAL
 * (TabelaDeJurosDoCalculo) em modo teste, com stub de repositório via JDBC.
 * Saída: "label;dataInicioJuros;dataLiquidacao;taxa".
 */
public class GoldenGenJurosTabela {
    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Connection cn;

    static Date d(String s) throws Exception { return ISO.parse(s); }

    /** Stub que carrega as faixas de TBJUROSPADRAO via JDBC (sem Seam/JPA). */
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
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
            return faixas;
        }
    }

    static class Caso {
        String label, ini, liq;
        Caso(String label, String ini, String liq) { this.label = label; this.ini = ini; this.liq = liq; }
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/");

        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeJurosPadrao.class, new StubRepoJurosPadrao());

        List<Caso> casos = new ArrayList<Caso>();
        casos.add(new Caso("padrao_faixa3_simples", "2015-02-01", "2020-12-01"));
        casos.add(new Caso("padrao_faixa3_curto",   "2018-05-10", "2024-05-10"));
        casos.add(new Caso("padrao_comp_e_simples",  "1990-01-01", "2000-01-01"));
        casos.add(new Caso("padrao_tres_faixas",     "1985-06-01", "1995-06-01"));

        System.out.println("label;dataInicioJuros;dataLiquidacao;taxa");
        for (Caso c : casos) {
            Date ini = d(c.ini), liq = d(c.liq);

            ParametrosDeAtualizacao pa = new ParametrosDeAtualizacao();
            pa.setJuros(JurosEnum.JUROS_PADRAO);
            pa.setAplicarJurosFasePreJudicial(Boolean.FALSE);
            pa.setCombinarOutroJuros(Boolean.FALSE);
            pa.setListaDeCombinacaoDeJuros(new java.util.LinkedHashSet());

            Calculo calc = new Calculo();
            calc.setParametrosDeAtualizacao(pa);
            calc.setDataDeLiquidacao(liq);
            calc.setDataAjuizamento(ini);
            calc.setDataAdmissao(ini);

            TabelaDeJurosDoCalculo tabela = new TabelaDeJurosDoCalculo(calc, ini, liq);
            BigDecimal taxa = tabela.calcularTaxaDeJuros(ini, null, false, false);

            System.out.println(c.label + ";" + c.ini + ";" + c.liq + ";" + taxa.toPlainString());
        }
        cn.close();
    }
}
