import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCorrecaoDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.salariofamilia.SalarioFamilia;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.SeguroDesemprego;

import java.math.BigDecimal;

/**
 * Golden da composição do BRUTO DEVIDO AO RECLAMANTE com os acessórios (FGTS que compõe o
 * principal, salário-família e seguro-desemprego), replicando Calculo.calcularBrutoDevidoAoReclamante.
 * Os totais são fixados por overrides no Calculo (o método apenas os acumula, parcela a parcela).
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenBrutoComAcessorios {

    static BigDecimal b(String s) { return new BigDecimal(s); }

    static class CalcStub extends Calculo {
        boolean deduzirFgts;

        @Override public BigDecimal getTotalDeValorCorrigidoDaApuracaoDeJuros() { return b("5000.00"); }
        @Override public BigDecimal getTotalDeJurosDaApuracaoDeJuros() { return b("1000.00"); }
        @Override public BigDecimal getValorTotalMultasDoTipoReclamanteReclamado() { return b("400.00"); }
        @Override public BigDecimal getValorTotalMultasDoTipoReclamadoReclamante() { return b("200.00"); }

        @Override public Fgts getFgts() {
            return new Fgts() {
                @Override public boolean isComporOPrincipal() { return true; }
                @Override public BigDecimal getTotalDoFgts(TipoDeCorrecaoDoFgtsEnum t) { return b("3000.00"); }
                @Override public BigDecimal getTotalDaMultaDoFgts() { return b("1200.00"); }
                @Override public BigDecimal getTotalDaMultaDoArtigo467() { return b("600.00"); }
                @Override public Boolean getDeduzirDoFGTS() { return Boolean.valueOf(deduzirFgts); }
                @Override public BigDecimal getTotalGeralDoDepositadoOuSacado(TipoDeCorrecaoDoFgtsEnum t) { return b("2500.00"); }
            };
        }

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
    }

    public static void main(String[] args) {
        CalcStub semDeducao = new CalcStub();
        semDeducao.deduzirFgts = false;
        System.out.println("SEM_DEDUCAO;bruto;" + semDeducao.calcularBrutoDevidoAoReclamante().toPlainString());

        CalcStub comDeducao = new CalcStub();
        comDeducao.deduzirFgts = true;
        System.out.println("COM_DEDUCAO_FGTS;bruto;" + comDeducao.calcularBrutoDevidoAoReclamante().toPlainString());
    }
}
