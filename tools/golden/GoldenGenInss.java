import br.jus.trt8.pjecalc.negocio.dominio.inss.faixas.*;
import br.jus.trt8.pjecalc.negocio.dominio.inss.seguradoempregado.TabelaPrevidenciariaSeguradoEmpregado;

import java.math.BigDecimal;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden da alíquota previdenciária do segurado (TabelaPrevidenciaria.obterAliquotaParaValor),
 * cobrindo as duas eras: alíquota única por faixa (até 02/2020) e alíquota efetiva
 * progressiva (a partir de 03/2020, Reforma da Previdência).
 * Saída: "competencia;valor;aliquota".
 */
public class GoldenGenInss {
    static final String URL = "jdbc:h2:./.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r";
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }

    static TabelaPrevidenciariaSeguradoEmpregado carregar(Connection cn, String competencia) throws Exception {
        String sql = "SELECT RVLINICIALFAIXAUM, RVLFINALFAIXAUM, RVLALIQUOTAFAIXAUM,"
            + " RVLINICIALFAIXADOIS, RVLFINALFAIXADOIS, RVLALIQUOTAFAIXADOIS,"
            + " RVLINICIALFAIXATRES, RVLFINALFAIXATRES, RVLALIQUOTAFAIXATRES,"
            + " RVLINICIALFAIXAQUATRO, RVLFINALFAIXAQUATRO, RVLALIQUOTAFAIXAQUATRO,"
            + " RVLINICIALFAIXACINCO, RVLFINALFAIXACINCO, RVLALIQUOTAFAIXACINCO,"
            + " RVLTETOMAXIMO, RVLTETOBENEFICIO"
            + " FROM TBTABELAINSSSEGURADOEMPREGADO WHERE DDTCOMPETENCIAREGISTRO = ?";
        try (PreparedStatement ps = cn.prepareStatement(sql)) {
            ps.setDate(1, new java.sql.Date(d(competencia).getTime()));
            try (ResultSet rs = ps.executeQuery()) {
                if (!rs.next()) throw new IllegalStateException("sem tabela para " + competencia);
                TabelaPrevidenciariaSeguradoEmpregado t = new TabelaPrevidenciariaSeguradoEmpregado();
                t.setCompetencia(d(competencia));

                PrimeiraFaixaPrevidenciaria f1 = new PrimeiraFaixaPrevidenciaria();
                f1.setValorInicial(rs.getBigDecimal(1)); f1.setValorFinal(rs.getBigDecimal(2)); f1.setAliquota(rs.getBigDecimal(3));
                t.setPrimeiraFaixaPrevidenciaria(f1);

                if (rs.getBigDecimal(6) != null) {
                    SegundaFaixaPrevidenciaria f2 = new SegundaFaixaPrevidenciaria();
                    f2.setValorInicial(rs.getBigDecimal(4)); f2.setValorFinal(rs.getBigDecimal(5)); f2.setAliquota(rs.getBigDecimal(6));
                    t.setSegundaFaixaPrevidenciaria(f2);
                } else { t.setSegundaFaixaPrevidenciaria(null); }

                if (rs.getBigDecimal(9) != null) {
                    TerceiraFaixaPrevidenciaria f3 = new TerceiraFaixaPrevidenciaria();
                    f3.setValorInicial(rs.getBigDecimal(7)); f3.setValorFinal(rs.getBigDecimal(8)); f3.setAliquota(rs.getBigDecimal(9));
                    t.setTerceiraFaixaPrevidenciaria(f3);
                } else { t.setTerceiraFaixaPrevidenciaria(null); }

                if (rs.getBigDecimal(12) != null) {
                    QuartaFaixaPrevidenciaria f4 = new QuartaFaixaPrevidenciaria();
                    f4.setValorInicial(rs.getBigDecimal(10)); f4.setValorFinal(rs.getBigDecimal(11)); f4.setAliquota(rs.getBigDecimal(12));
                    t.setQuartaFaixaPrevidenciaria(f4);
                }
                if (rs.getBigDecimal(15) != null) {
                    QuintaFaixaPrevidenciaria f5 = new QuintaFaixaPrevidenciaria();
                    f5.setValorInicial(rs.getBigDecimal(13)); f5.setValorFinal(rs.getBigDecimal(14)); f5.setAliquota(rs.getBigDecimal(15));
                    t.setQuintaFaixaPrevidenciaria(f5);
                }
                t.setValorTetoMaximo(rs.getBigDecimal(16));
                t.setValorTetoBeneficio(rs.getBigDecimal(17));
                return t;
            }
        }
    }

    public static void main(String[] args) throws Exception {
        Class.forName("org.h2.Driver");
        // Competências dos dois lados da reforma (01/03/2020) + uma recente.
        String[] competencias = {"2019-12-01", "2020-02-01", "2020-03-01", "2025-01-01"};
        // Valores em torno dos limites de faixa, do teto e abaixo da faixa 1.
        String[] valores = {"500.00", "1045.00", "1045.01", "1518.00", "1751.81", "1800.00",
                            "2089.60", "2793.88", "2919.72", "3134.40", "4190.83", "5000.00",
                            "5839.45", "6101.06", "8157.41", "12000.00"};

        try (Connection cn = DriverManager.getConnection(URL, "pjecalc", "/pjecalc/")) {
            System.out.println("competencia;valor;aliquota");
            for (String comp : competencias) {
                TabelaPrevidenciariaSeguradoEmpregado t = carregar(cn, comp);
                for (String v : valores) {
                    BigDecimal aliq = t.obterAliquotaParaValor(new BigDecimal(v));
                    System.out.println(comp + ";" + v + ";" + (aliq == null ? "NULL" : aliq.toPlainString()));
                }
            }
        }
    }
}
