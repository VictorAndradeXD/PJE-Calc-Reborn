import br.jus.trt8.pjecalc.base.comum.Periodo;
import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculoDoIntegralizar;
import br.jus.trt8.pjecalc.negocio.comum.rotinasdecalculo.CalculoDoProporcionalizar;
import br.jus.trt8.pjecalc.negocio.constantes.ValorDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden das primitivas de verbas: proporcionalização/integralização por dias
 * (D=30, fevereiro 28/29, piso 0, teto 30) e a fórmula da ocorrência
 * (devido = base/divisor x multiplicador x quantidade, x2 se dobra; diferença; corrigida).
 */
public class GoldenGenVerbas {
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    public static void main(String[] args) throws Exception {
        System.out.println("### PROPORCIONALIZAR (tipo;inicio;fim;valor;exclusoes;resultado)");
        String[][] props = {
            {"2021-01-01", "2021-01-31", "3000.00", "0"},   // mês cheio 31 dias
            {"2021-04-01", "2021-04-30", "3000.00", "0"},   // mês cheio 30 dias
            {"2021-02-01", "2021-02-28", "3000.00", "0"},   // fevereiro
            {"2020-02-01", "2020-02-29", "3000.00", "0"},   // fevereiro bissexto
            {"2021-01-15", "2021-01-31", "3000.00", "0"},   // parcial 17 dias
            {"2021-01-01", "2021-01-31", "3000.00", "5"},   // 5 exclusões
            {"2021-02-10", "2021-02-28", "2800.00", "0"},   // fevereiro parcial 19 dias
            {"2021-01-10", "2021-01-20", "3000.00", "15"},  // exclusões maiores que os dias (piso 0)
        };
        for (String[] p : props) {
            Periodo periodo = new Periodo(d(p[0]), d(p[1]));
            CalculoDoProporcionalizar prop = new CalculoDoProporcionalizar(periodo, b(p[2]), Integer.parseInt(p[3]));
            prop.executar();
            System.out.println("P;" + p[0] + ";" + p[1] + ";" + p[2] + ";" + p[3] + ";" + prop.getResultado().toPlainString());
            CalculoDoIntegralizar integ = new CalculoDoIntegralizar(periodo, b(p[2]), Integer.parseInt(p[3]));
            integ.executar();
            System.out.println("I;" + p[0] + ";" + p[1] + ";" + p[2] + ";" + p[3] + ";" + integ.getResultado().toPlainString());
        }

        System.out.println("### OCORRENCIA (base;divisor;mult;qtd;dobra;pago;indice;zeraNeg;devido;diferenca;difCorrigida)");
        String[][] ocs = {
            // hora extra 50%: 2200/220*1.5*20 = 300
            {"2200.00", "220", "1.5", "20", "false", "0",      "1.3",  "true"},
            // com pago parcial
            {"2200.00", "220", "1.5", "20", "false", "120.50", "1.3",  "true"},
            // dobra
            {"1500.00", "30",  "1",   "5",  "true",  "0",      "1.15", "true"},
            // pago maior que devido, zeraNeg=true -> diferenca 0
            {"1000.00", "220", "1.5", "10", "false", "500.00", "1.2",  "true"},
            // pago maior que devido, zeraNeg=false -> diferenca negativa
            {"1000.00", "220", "1.5", "10", "false", "500.00", "1.2",  "false"},
            // divisor quebrado
            {"3333.33", "200", "1.6", "17.5", "false", "0",    "1.0",  "true"},
        };
        for (String[] o : ocs) {
            VerbaDeCalculo verba = new Informada();
            verba.setZeraValorNegativo(Boolean.parseBoolean(o[7]));

            OcorrenciaDeVerba oc = new OcorrenciaDeVerba();
            oc.setVerbaDeCalculo(verba);
            oc.setValor(ValorDaVerbaEnum.CALCULADO);
            oc.setBase(b(o[0]));
            oc.setDivisor(b(o[1]));
            oc.setMultiplicador(b(o[2]));
            oc.setQuantidade(b(o[3]));
            oc.setDobra(Boolean.parseBoolean(o[4]));
            oc.setPago(b(o[5]));
            oc.setIndiceAcumulado(b(o[6]));
            // Preencher os campos *Integral evita a integralização lazy (que consulta repositório).
            oc.setBaseIntegral(b(o[0]));
            oc.setQuantidadeIntegral(b(o[3]));
            oc.setPagoIntegral(b(o[5]));
            oc.calcularValorDevido();

            System.out.println(o[0] + ";" + o[1] + ";" + o[2] + ";" + o[3] + ";" + o[4] + ";" + o[5] + ";" + o[6] + ";" + o[7]
                + ";" + oc.getDevido().toPlainString()
                + ";" + oc.getDiferenca().toPlainString()
                + ";" + oc.getDiferencaCorrigida().toPlainString());
        }
    }
}
