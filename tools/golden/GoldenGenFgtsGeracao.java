import br.jus.trt8.pjecalc.base.comum.Periodo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.regras.PeriodoDoFgtsValidRule;

import java.util.Date;
import java.util.GregorianCalendar;

/**
 * Golden da prescrição do FGTS (STF ARE 709212: 30 anos, 5 anos na transição/pós-2019) e da
 * janela sugerida de geração das ocorrências (max(admissão, prescrição) → demissão/término).
 * Saída: "cenario;chave;valor" (datas em yyyy-MM-dd).
 */
public class GoldenGenFgtsGeracao {

    static Date d(int a, int m, int dia) { return new GregorianCalendar(a, m - 1, dia).getTime(); }

    static String f(Date data) {
        if (data == null) return "";
        GregorianCalendar c = new GregorianCalendar();
        c.setTime(data);
        return String.format("%04d-%02d-%02d", c.get(GregorianCalendar.YEAR), c.get(GregorianCalendar.MONTH) + 1, c.get(GregorianCalendar.DAY_OF_MONTH));
    }

    static void row(String cenario, String chave, Date valor) {
        System.out.println(cenario + ";" + chave + ";" + f(valor));
    }

    static Calculo calc(Date admissao, Date demissao, Date ajuizamento, Date termino, boolean prescricaoFgts) {
        Calculo c = new Calculo();
        c.setDataAdmissao(admissao);
        c.setDataDemissao(demissao);
        c.setDataAjuizamento(ajuizamento);
        c.setDataTerminoCalculo(termino);
        c.setPrescricaoFgts(Boolean.valueOf(prescricaoFgts));
        return c;
    }

    static void prescricao(String cenario, Date admissao, Date ajuizamento) {
        Calculo c = calc(admissao, d(2021, 6, 1), ajuizamento, d(2021, 6, 1), true);
        row(cenario, "prescricao", c.getDataPrescricaoFgts());
    }

    static void janela(String cenario, Date admissao, Date demissao, Date ajuizamento, Date termino, boolean presc) {
        Calculo c = calc(admissao, demissao, ajuizamento, termino, presc);
        Fgts fgts = new Fgts();
        fgts.setCalculo(c);
        Periodo p = new PeriodoDoFgtsValidRule().getPeriodoSugerido(fgts);
        row(cenario, "inicial", p.getInicial());
        row(cenario, "final", p.getFinal());
    }

    public static void main(String[] args) {
        // ---- Prescrição (STF ARE 709212) ----
        prescricao("P1_ANTES_2014", d(2010, 1, 1), d(2010, 1, 1));                 // -30 anos
        prescricao("P2_TRANSICAO_ADM_APOS_1989", d(2005, 1, 1), d(2016, 6, 1));    // -5 anos
        prescricao("P3_TRANSICAO_ADM_ANTES_1989", d(1985, 1, 1), d(2016, 6, 1));   // -30 anos (admissão <= 13/11/1989)
        prescricao("P4_POS_2019", d(2005, 1, 1), d(2020, 3, 1));                    // -5 anos
        prescricao("P5_LIMITE_2014", d(2000, 1, 1), d(2014, 11, 13));               // -5 anos (>= 13/11/2014)
        prescricao("P6_LIMITE_2019", d(2000, 1, 1), d(2019, 11, 13));               // -5 anos (>= 13/11/2019)
        prescricao("P7_ADM_LIMITE_1989", d(1989, 11, 13), d(2016, 6, 1));           // -30 anos (admissão = 13/11/1989, não > )

        // ---- Janela sugerida da geração ----
        // Prescrição desligada: início = admissão.
        janela("J1_SEM_PRESCRICAO", d(2015, 1, 10), d(2021, 3, 20), d(2020, 1, 1), d(2021, 6, 1), false);
        // Prescrição ligada e posterior à admissão: início = prescrição (ajuiz 2020 -> -5 = 2015-06-01 > adm 2010).
        janela("J2_PRESCRICAO_APOS_ADM", d(2010, 1, 10), d(2021, 3, 20), d(2020, 6, 1), d(2021, 6, 1), true);
        // Prescrição ligada mas anterior à admissão: início = admissão.
        janela("J3_PRESCRICAO_ANTES_ADM", d(2018, 1, 10), d(2021, 3, 20), d(2020, 6, 1), d(2021, 6, 1), true);
        // Sem demissão: final = término do cálculo.
        janela("J4_SEM_DEMISSAO", d(2015, 1, 10), null, d(2020, 1, 1), d(2021, 6, 1), false);
    }
}
