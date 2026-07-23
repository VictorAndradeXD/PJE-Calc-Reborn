import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculadorDeIndices;
import br.jus.trt8.pjecalc.negocio.dominio.indices.ipcae.IndiceIPCAE;
import br.jus.trt8.pjecalc.negocio.dominio.indices.api.IndiceDeCalculo;
import br.jus.trt8.pjecalc.base.comum.Utils;
import java.math.BigDecimal;
import java.util.*;

/** Smoke test: confirma que o motor puro de correcao roda headless (sem Seam/JPA). */
public class GoldenSmoke {
    static Date comp(int y, int m) {
        Calendar c = Calendar.getInstance();
        c.clear();
        c.set(y, m - 1, 1, 0, 0, 0);
        return c.getTime();
    }

    public static void main(String[] args) {
        List<IndiceIPCAE> lista = new ArrayList<IndiceIPCAE>();
        lista.add(new IndiceIPCAE(comp(2020, 1), new BigDecimal("10")));
        lista.add(new IndiceIPCAE(comp(2020, 2), new BigDecimal("10")));

        List<IndiceDeCalculo> acum = CalculadorDeIndices.calcularIndiceAcumulado(lista);
        for (IndiceDeCalculo i : acum) {
            System.out.println("comp=" + i.getCompetencia() + " taxa=" + i.getTaxa()
                + " valorIndice=" + i.getValorIndice() + " acumulado=" + i.getValorAcumulado());
        }
        // Lista fica ordenada decrescente (mais novo primeiro); o mais antigo (venc) e o ultimo.
        IndiceDeCalculo maisAntigo = acum.get(acum.size() - 1);
        BigDecimal fator = maisAntigo.getValorAcumulado();
        System.out.println("FATOR(Jan->Fev) = " + fator);
        System.out.println("CORRIGIDO(100)  = " + Utils.aplicarCorrecaoMonetaria(fator, new BigDecimal("100")));
    }
}
