import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCorrecaoDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.pensaoalimenticia.MaquinaDeCalculoDePensaoAlimenticia;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.pensaoalimenticia.PensaoAlimenticia;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.List;

/**
 * Golden da pensão alimentícia: dirige MaquinaDeCalculoDePensaoAlimenticia.liquidar (monta as
 * bases a partir das verbas com incidência de pensão e do FGTS) e lê getValorDevido
 * (totalDasBases × alíquota/100). Sem incidência sobre juros (dispensa a apuração de juros).
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenPensao {

    static Date d(int a, int m, int dia) { return new GregorianCalendar(a, m - 1, dia).getTime(); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    static class CalcStub extends Calculo {
        List<VerbaDeCalculo> ativas = new ArrayList<VerbaDeCalculo>();
        @Override public java.util.List<VerbaDeCalculo> getVerbasAtivas() { return ativas; }
        @Override public Boolean isCalculoExterno() { return Boolean.FALSE; }
        @Override public Fgts getFgts() {
            return new Fgts() {
                @Override public Boolean getIncidenciaPensaoAlimenticia() { return Boolean.TRUE; }
                @Override public Boolean getIncidenciaPensaoAlimenticiaSobreMulta() { return Boolean.TRUE; }
                @Override public Boolean getDeduzirDoFGTS() { return Boolean.FALSE; }
                @Override public BigDecimal getTotalDaDiferencaCorrigida(TipoDeCorrecaoDoFgtsEnum t) { return b("5000.00"); }
                @Override public BigDecimal getValorDaMultaDoFgtsCorrigido() { return b("2000.00"); }
            };
        }
    }

    static VerbaDeCalculo verba(Calculo calc, String devido, boolean pensao, boolean irpf) {
        Informada v = new Informada();
        v.setCalculo(calc); v.setAtivo(Boolean.TRUE); v.setCaracteristica(CaracteristicaDaVerbaEnum.COMUM);
        v.setIncidenciaPensaoAlimenticia(Boolean.valueOf(pensao));
        v.setIncidenciaIRPF(Boolean.valueOf(irpf));
        OcorrenciaDeVerba o = new OcorrenciaDeVerba();
        o.setDataInicial(d(2020, 3, 1)); o.setDataFinal(d(2020, 3, 31));
        o.setDevido(new BigDecimal(devido)); o.setPago(BigDecimal.ZERO);
        o.setIndiceAcumulado(BigDecimal.ONE); o.setAtivo(Boolean.TRUE);
        v.adicionarEmOcorrencias(o);
        return v;
    }

    public static void main(String[] args) {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());

        CalcStub calc = new CalcStub();
        calc.ativas.add(verba(calc, "2000.00", true, true));   // pensão + IRPF (tributável)
        calc.ativas.add(verba(calc, "1000.00", true, false));  // pensão, não tributável
        calc.ativas.add(verba(calc, "9999.00", false, true));  // sem incidência de pensão -> ignorada

        PensaoAlimenticia pensao = new PensaoAlimenticia();
        pensao.setCalculo(calc);
        pensao.setIncidirSobreJuros(Boolean.FALSE);
        pensao.setAliquota(b("30.00"));

        new MaquinaDeCalculoDePensaoAlimenticia(pensao).liquidar();

        row("PENSAO", "baseVerbas", pensao.getValorBaseVerbas());
        row("PENSAO", "baseVerbasTributaveis", pensao.getValorBaseVerbasTributaveis());
        row("PENSAO", "baseFgts", pensao.getValorBaseFgts());
        row("PENSAO", "baseMultaFgts", pensao.getValorBaseMultaDoFgts());
        row("PENSAO", "valorDevido", pensao.getValorDevido());
    }

    static void row(String c, String k, BigDecimal v) {
        System.out.println(c + ";" + k + ";" + (v == null ? "" : v.toPlainString()));
    }
}
