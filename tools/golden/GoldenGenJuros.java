import br.jus.trt8.pjecalc.negocio.comum.PeriodoDeJuros;
import br.jus.trt8.pjecalc.negocio.constantes.JurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeJurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeQuantidadeDeJurosBaseEnum;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;

/**
 * Golden values da matemática por período dos juros (PeriodoDeJuros, POJO puro):
 * getMeses() (fração vs inteiro/regra dos 15 dias) e getTaxa() (diário/simples/composto).
 * Saída: "label;inicio;fim;aliquota;quantidade;tipo;tabela;meses;taxa".
 */
public class GoldenGenJuros {
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");

    static Date d(String s) throws Exception { return ISO.parse(s); }

    static class Caso {
        String label, ini, fim, tabela; BigDecimal aliq;
        TipoDeQuantidadeDeJurosBaseEnum q; TipoDeJurosEnum t; JurosEnum tab;
        Caso(String label, String ini, String fim, String aliq, TipoDeQuantidadeDeJurosBaseEnum q,
             TipoDeJurosEnum t, JurosEnum tab) {
            this.label = label; this.ini = ini; this.fim = fim; this.aliq = new BigDecimal(aliq);
            this.q = q; this.t = t; this.tab = tab;
        }
    }

    public static void main(String[] args) throws Exception {
        java.util.List<Caso> casos = java.util.Arrays.asList(
            new Caso("simples_12meses_cheios", "2020-01-01", "2020-12-31", "1", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_UM_PORCENTO),
            new Caso("simples_fracao_pontas",  "2020-01-15", "2020-06-10", "1", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_UM_PORCENTO),
            new Caso("simples_meio_porcento",  "2019-03-20", "2022-08-05", "0.5", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_MEIO_PORCENTO),
            new Caso("inteiro_15dias_corta",   "2020-01-20", "2020-06-10", "1", TipoDeQuantidadeDeJurosBaseEnum.INTEIRO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_PADRAO),
            new Caso("inteiro_15dias_mantem",  "2020-01-10", "2020-06-20", "1", TipoDeQuantidadeDeJurosBaseEnum.INTEIRO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_PADRAO),
            new Caso("inteiro_exato_15",       "2020-01-17", "2020-06-15", "1", TipoDeQuantidadeDeJurosBaseEnum.INTEIRO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_PADRAO),
            new Caso("diario_zerotrintatres",  "2020-01-01", "2020-01-31", "0.0333333", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_ZERO_TRINTA_TRES),
            new Caso("diario_dois_meses",      "2021-02-10", "2021-04-20", "0.0333333", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.SIMPLES, JurosEnum.JUROS_ZERO_TRINTA_TRES),
            new Caso("composto_12meses",       "2020-01-01", "2020-12-31", "1", TipoDeQuantidadeDeJurosBaseEnum.INTEIRO, TipoDeJurosEnum.COMPOSTOS, JurosEnum.JUROS_PADRAO),
            new Caso("composto_fracao",        "2019-06-10", "2021-03-20", "1", TipoDeQuantidadeDeJurosBaseEnum.FRACAO, TipoDeJurosEnum.COMPOSTOS, JurosEnum.JUROS_PADRAO)
        );

        System.out.println("label;inicio;fim;aliquota;quantidade;tipo;tabela;meses;taxa");
        for (Caso c : casos) {
            PeriodoDeJuros p = new PeriodoDeJuros(d(c.ini), d(c.fim), c.aliq, c.q, c.t, false, c.tab);
            System.out.println(c.label + ";" + c.ini + ";" + c.fim + ";" + c.aliq.toPlainString()
                + ";" + c.q + ";" + c.t + ";" + c.tab
                + ";" + p.getMeses().toPlainString() + ";" + p.getTaxa().toPlainString());
        }
    }
}
