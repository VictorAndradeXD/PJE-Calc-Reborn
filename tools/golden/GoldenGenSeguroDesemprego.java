import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.MaquinaDeCalculoDeSeguroDesemprego;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.SeguroDesemprego;

import java.lang.reflect.Method;
import java.math.BigDecimal;

/**
 * Golden do valor da parcela do seguro-desemprego (fórmula de duas faixas + piso/teto). Dirige o
 * método privado MaquinaDeCalculoDeSeguroDesemprego.encontraOValorDoSeguroDesemprego via
 * reflection — ele lê apenas os campos de faixa e a remuneração do SeguroDesemprego, dispensando
 * demissão/correção/verbas. Alíquotas da tabela real de 01/2021.
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenSeguroDesemprego {

    // Tabela de 01/2021: faixa1 até 1686,79 a 80%; faixa2 a 50% + 1349,43; piso 1100; teto 1911,84.
    static BigDecimal parcela(String remuneracao) throws Exception {
        SeguroDesemprego sd = new SeguroDesemprego();
        sd.setRemuneracaoMensal(new BigDecimal(remuneracao));
        sd.setLimiteFaixa1(new BigDecimal("1686.79"));
        sd.setValorPercentualFaixa1(new BigDecimal("80.00"));
        sd.setValorPercentualFaixa2(new BigDecimal("50.00"));
        sd.setSomaFaixa2(new BigDecimal("1349.43"));
        sd.setValorPiso(new BigDecimal("1100.00"));
        sd.setValorTeto(new BigDecimal("1911.84"));

        MaquinaDeCalculoDeSeguroDesemprego maquina = new MaquinaDeCalculoDeSeguroDesemprego(sd);
        Method m = MaquinaDeCalculoDeSeguroDesemprego.class.getDeclaredMethod("encontraOValorDoSeguroDesemprego");
        m.setAccessible(true);
        return (BigDecimal) m.invoke(maquina);
    }

    public static void main(String[] args) throws Exception {
        String[] remuneracoes = {"800.00", "1000.00", "1375.00", "1500.00", "1686.79", "2000.00", "3000.00"};
        for (String rem : remuneracoes) {
            System.out.println("SD_" + rem + ";parcela;" + parcela(rem).toPlainString());
        }
    }
}
