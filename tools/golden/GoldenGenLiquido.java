import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCorrecaoDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.OcorrenciaDeFgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.salariofamilia.SalarioFamilia;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.SeguroDesemprego;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.Set;

/**
 * Golden dos dois agregados de crédito do reclamante que formam o líquido:
 * calculaValorVerbaParaCreditoDoReclamante (principal corrigido + juros + salário-família +
 * seguro-desemprego + multa do art. 467) e calculaValorFgtsParaCreditoDoReclamante (FGTS + multa
 * − depositado/sacado quando "deduzir do FGTS"). Totais fixados por override no Calculo.
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenLiquido {

    static BigDecimal b(String s) { return new BigDecimal(s); }

    static class CalcStub extends Calculo {
        boolean deduzirFgts;

        @Override public BigDecimal getTotalDeValorCorrigidoDaApuracaoDeJuros() { return b("5000.00"); }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJuros() { return b("1000.00"); }

        @Override public SalarioFamilia getSalarioFamilia() {
            return new SalarioFamilia() {
                @Override public Boolean getApurarSalarioFamilia() { return Boolean.TRUE; }
                @Override public boolean isComporOPrincipal() { return true; }
                @Override public BigDecimal getTotalGeral() { return b("300.00"); }
            };
        }

        @Override public SeguroDesemprego getSeguroDesemprego() {
            return new SeguroDesemprego() {
                @Override public Boolean getApurarSeguroDesemprego() { return Boolean.TRUE; }
                @Override public boolean isComporOPrincipal() { return true; }
                @Override public BigDecimal getTotal() { return b("800.00"); }
            };
        }

        @Override public Fgts getFgts() {
            return new Fgts() {
                @Override public boolean isComporOPrincipal() { return true; }
                @Override public Boolean getMulta() { return Boolean.TRUE; }
                @Override public Boolean getMultaDoArtigo467() { return Boolean.TRUE; }
                @Override public Boolean getDeduzirDoFGTS() { return Boolean.valueOf(deduzirFgts); }
                @Override public BigDecimal getTotalDoFgts(TipoDeCorrecaoDoFgtsEnum t) { return b("3000.00"); }
                @Override public BigDecimal getTotalDaMultaDoFgts() { return b("1200.00"); }
                @Override public BigDecimal getTotalDaMultaDoArtigo467() { return b("600.00"); }
                @Override public BigDecimal getTotalGeralDoDepositadoOuSacado(TipoDeCorrecaoDoFgtsEnum t) { return b("2500.00"); }
                @Override public Set<OcorrenciaDeFgts> getOcorrenciasVisiveisRelatorio() {
                    return Collections.singleton((OcorrenciaDeFgts) null);
                }
            };
        }
    }

    public static void main(String[] args) {
        CalcStub c = new CalcStub();
        c.deduzirFgts = false;
        System.out.println("SEM_DEDUCAO;creditoVerbas;" + c.calculaValorVerbaParaCreditoDoReclamante(Boolean.TRUE).toPlainString());
        System.out.println("SEM_DEDUCAO;creditoFgts;" + c.calculaValorFgtsParaCreditoDoReclamante().toPlainString());

        CalcStub d = new CalcStub();
        d.deduzirFgts = true;
        System.out.println("COM_DEDUCAO_FGTS;creditoFgts;" + d.calculaValorFgtsParaCreditoDoReclamante().toPlainString());
    }
}
