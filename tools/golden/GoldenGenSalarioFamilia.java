import br.jus.trt8.pjecalc.negocio.dominio.salariofamilia.TabelaSalarioFamilia;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden da seleção de faixa do salário-família: getValorSalarioFamiliaParaO(remuneração) sobre a
 * TabelaSalarioFamilia real (TBTABELASALARIOFAMILIA), carregada por JDBC.
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenSalarioFamilia {
    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }

    static TabelaSalarioFamilia carregar(Connection cn, String competencia) throws Exception {
        String sql = "SELECT RVLINICIALFAIXAUM, RVLFINALFAIXAUM, RVLSALARIOFAIXAUM,"
            + " RVLINICIALFAIXADOIS, RVLFINALFAIXADOIS, RVLSALARIOFAIXADOIS"
            + " FROM TBTABELASALARIOFAMILIA WHERE DDTCOMPETENCIAREGISTRO = ?";
        try (PreparedStatement ps = cn.prepareStatement(sql)) {
            ps.setDate(1, new java.sql.Date(d(competencia).getTime()));
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalStateException("sem tabela p/ " + competencia);
                TabelaSalarioFamilia t = new TabelaSalarioFamilia();
                t.setCompetencia(d(competencia));
                t.setValorInicialFaixa1(rs.getBigDecimal(1));
                t.setValorFinalFaixa1(rs.getBigDecimal(2));
                t.setValorSalarioFamiliaFaixa1(rs.getBigDecimal(3));
                t.setValorInicialFaixa2(rs.getBigDecimal(4));
                t.setValorFinalFaixa2(rs.getBigDecimal(5));
                t.setValorSalarioFamiliaFaixa2(rs.getBigDecimal(6));
                return t;
            }
        }
    }

    static void cota(Connection cn, String competencia, String[] remuneracoes) throws Exception {
        TabelaSalarioFamilia t = carregar(cn, competencia);
        for (String rem : remuneracoes) {
            BigDecimal cota = t.getValorSalarioFamiliaParaO(new BigDecimal(rem));
            System.out.println(competencia + "_" + rem + ";cota;" + (cota == null ? "" : cota.toPlainString()));
        }
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/")) {
            // 2019: faixa1 [0, 907.77]=46.54; faixa2 [907.78, 1364.43]=32.80; acima -> nulo.
            cota(cn, "2019-01-01", new String[]{"500.00", "907.77", "907.78", "1000.00", "1364.43", "1364.44", "5000.00"});
            // 2021: faixa1 [0, 1000.14]=51.27; faixa2 [1000.15, 1503.25]=51.27; acima -> nulo.
            cota(cn, "2021-01-01", new String[]{"500.00", "1000.14", "1200.00", "1503.25", "1503.26"});
            // 2022: faixa1 [0, 1101.75]=56.47; faixa2 [1101.76, 1655.98]=56.47; acima -> nulo.
            cota(cn, "2022-01-01", new String[]{"1101.75", "1655.98", "1656.00"});
        }
    }
}
