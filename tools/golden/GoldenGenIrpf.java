import br.jus.trt8.pjecalc.negocio.dominio.calculo.irpf.OcorrenciaDeIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.TabelaIrpf;
import br.jus.trt8.pjecalc.negocio.dominio.irpf.faixas.*;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden do IRPF (matemática pura): seleção de faixa (normal e RRA com tabela x NM),
 * base (verbas + juros - deduções, piso 0) e imposto devido (base x alíquota - parcela a
 * deduzir, piso 0). Usa TabelaIrpf e OcorrenciaDeIrpf reais.
 * Saída: "competencia;base;nm;verbas;juros;inss;pensao;honorarios;dependentes;imposto".
 */
public class GoldenGenIrpf {
    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    static TabelaIrpf carregar(Connection cn, String competencia) throws Exception {
        String sql = "SELECT RVLINICIALFAIXAUM, RVLFINALFAIXAUM, RVLALIQUOTAFAIXAUM, RVLDEDUCAOFAIXAUM,"
            + " RVLINICIALFAIXADOIS, RVLFINALFAIXADOIS, RVLALIQUOTAFAIXADOIS, RVLDEDUCAOFAIXADOIS,"
            + " RVLINICIALFAIXATRES, RVLFINALFAIXATRES, RVLALIQUOTAFAIXATRES, RVLDEDUCAOFAIXATRES,"
            + " RVLINICIALFAIXAQUATRO, RVLFINALFAIXAQUATRO, RVLALIQUOTAFAIXAQUATRO, RVLDEDUCAOFAIXAQUATRO,"
            + " RVLINICIALFAIXACINCO, RVLFINALFAIXACINCO, RVLALIQUOTAFAIXACINCO, RVLDEDUCAOFAIXACINCO,"
            + " RVLDEDUCAOPORDEPENDENTE, RVLDEDUCAOAPOSENTADOMEIACINCO"
            + " FROM TBTABELAIMPOSTORENDA WHERE DDTCOMPETENCIAREGISTRO = ?";
        try (PreparedStatement ps = cn.prepareStatement(sql)) {
            ps.setDate(1, new java.sql.Date(d(competencia).getTime()));
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalStateException("sem tabela para " + competencia);
                TabelaIrpf t = new TabelaIrpf();
                t.setCompetencia(d(competencia));
                t.setPrimeiraFaixaFiscal(new PrimeiraFaixaFiscal(rs.getBigDecimal(1), rs.getBigDecimal(2), rs.getBigDecimal(3), rs.getBigDecimal(4)));
                t.setSegundaFaixaFiscal(new SegundaFaixaFiscal(rs.getBigDecimal(5), rs.getBigDecimal(6), rs.getBigDecimal(7), rs.getBigDecimal(8)));
                t.setTerceiraFaixaFiscal(new TerceiraFaixaFiscal(rs.getBigDecimal(9), rs.getBigDecimal(10), rs.getBigDecimal(11), rs.getBigDecimal(12)));
                t.setQuartaFaixaFiscal(new QuartaFaixaFiscal(rs.getBigDecimal(13), rs.getBigDecimal(14), rs.getBigDecimal(15), rs.getBigDecimal(16)));
                t.setQuintaFaixaFiscal(new QuintaFaixaFiscal(rs.getBigDecimal(17), rs.getBigDecimal(18), rs.getBigDecimal(19), rs.getBigDecimal(20)));
                t.setValorDeducaoPorDependente(rs.getBigDecimal(21));
                t.setValorDeducaoParaAposentadoMaiorQue65Anos(rs.getBigDecimal(22));
                return t;
            }
        }
    }

    static class Caso {
        String comp; int nm;
        String verbas, juros, inss, pensao, honorarios, dependentes;
        Caso(String comp, int nm, String verbas, String juros, String inss, String pensao, String honorarios, String dependentes) {
            this.comp = comp; this.nm = nm; this.verbas = verbas; this.juros = juros; this.inss = inss;
            this.pensao = pensao; this.honorarios = honorarios; this.dependentes = dependentes;
        }
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        Caso[] casos = {
            // normais (NM=1) em torno dos limites de faixa (comp 2024-02)
            new Caso("2024-02-01", 1, "2000.00", "0",     "0",      "0", "0", "0"),
            new Caso("2024-02-01", 1, "3000.00", "0",     "200.00", "0", "0", "0"),
            new Caso("2024-02-01", 1, "5000.00", "0",     "400.00", "0", "0", "189.59"),
            new Caso("2024-02-01", 1, "10000.00","500.00","800.00", "300.00", "1000.00", "379.18"),
            new Caso("2024-02-01", 1, "2259.20", "0",     "0",      "0", "0", "0"),   // limite exato faixa1
            new Caso("2024-02-01", 1, "2259.21", "0",     "0",      "0", "0", "0"),   // primeiro centavo faixa2
            // RRA (tabela x NM)
            new Caso("2024-02-01", 12, "60000.00","0",    "6600.00","0", "0", "0"),
            new Caso("2024-02-01", 24, "120000.00","0",   "13200.00","0","0", "0"),
            new Caso("2024-02-01", 6,  "18000.00","0",    "1980.00","0", "0", "0")
        };

        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/")) {
            System.out.println("competencia;base;nm;verbas;juros;inss;pensao;honorarios;dependentes;faixaInicial;faixaFinal;aliquota;deducao;imposto");
            for (Caso c : casos) {
                TabelaIrpf t = carregar(cn, c.comp);
                BigDecimal nm = new BigDecimal(c.nm);

                OcorrenciaDeIrpf o = new OcorrenciaDeIrpf();
                o.setValorVerbas(b(c.verbas));
                o.setValorJuros(b(c.juros));
                o.setValorContribuicaoSocial(b(c.inss));
                o.setValorPensaoAlimenticia(b(c.pensao));
                o.setValorHonorarios(b(c.honorarios));
                o.setValorDependentes(b(c.dependentes));
                o.atualizaBase();
                BigDecimal base = o.getValorBase();

                FaixaFiscal faixa = c.nm == 1 ? t.obterFaixaParaValor(base) : t.obterFaixaParaValor(base, nm);
                BigDecimal deducao = faixa.getDeducao().multiply(nm);
                o.setValorAliquota(faixa.getAliquota());
                o.setValorDeducao(deducao);
                o.atualizaValorDevido();

                BigDecimal faixaFinal = faixa.getValorFinal() == null ? null : faixa.getValorFinal().multiply(nm);
                System.out.println(c.comp + ";" + base.toPlainString() + ";" + c.nm + ";" + c.verbas + ";" + c.juros
                    + ";" + c.inss + ";" + c.pensao + ";" + c.honorarios + ";" + c.dependentes
                    + ";" + faixa.getValorInicial().toPlainString() + ";" + (faixaFinal == null ? "" : faixaFinal.toPlainString())
                    + ";" + faixa.getAliquota().toPlainString() + ";" + deducao.toPlainString()
                    + ";" + o.getValorDevido().toPlainString());
            }
        }
    }
}
