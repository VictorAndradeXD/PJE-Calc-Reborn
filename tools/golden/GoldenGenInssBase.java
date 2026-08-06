import br.jus.trt8.pjecalc.base.comum.Competencia;
import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.comum.OptimizerListSearch;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.RegimeDoContratoEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.inss.Inss;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.inss.sobresalarios.MaquinaDeCalculoDoInss;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.List;

/**
 * Golden da geração da base do INSS sobre as verbas por competência: dirige o método privado
 * MaquinaDeCalculoDoInss.calcularValorBaseVerbas via reflection (soma das diferenças para cálculo
 * das incidências, separando verbas comuns das de 13º).
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenInssBase {

    static Date d(int a, int m, int dia) { return new GregorianCalendar(a, m - 1, dia).getTime(); }

    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    static void oc(VerbaDeCalculo v, Date ini, Date fim, String devido, String pago) {
        OcorrenciaDeVerba o = new OcorrenciaDeVerba();
        o.setDataInicial(ini); o.setDataFinal(fim);
        o.setDevido(new BigDecimal(devido)); o.setPago(new BigDecimal(pago));
        o.setAtivo(Boolean.TRUE);
        v.adicionarEmOcorrencias(o);
    }

    static VerbaDeCalculo verba(Calculo calc, CaracteristicaDaVerbaEnum car) {
        Informada v = new Informada();
        v.setCalculo(calc); v.setAtivo(Boolean.TRUE); v.setCaracteristica(car);
        v.setZeraValorNegativo(Boolean.TRUE);
        return v;
    }

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());

        Calculo calc = new Calculo();
        calc.setRegimeDoContrato(RegimeDoContratoEnum.INTEGRAL);

        VerbaDeCalculo a = verba(calc, CaracteristicaDaVerbaEnum.COMUM);
        oc(a, d(2020, 3, 1), d(2020, 3, 31), "3000.00", "0");
        oc(a, d(2020, 12, 1), d(2020, 12, 31), "2000.00", "0");
        VerbaDeCalculo b = verba(calc, CaracteristicaDaVerbaEnum.COMUM);
        oc(b, d(2020, 3, 1), d(2020, 3, 31), "500.00", "200.00");
        VerbaDeCalculo c = verba(calc, CaracteristicaDaVerbaEnum.DECIMO_TERCEIRO_SALARIO);
        oc(c, d(2020, 12, 1), d(2020, 12, 31), "2500.00", "0");

        List<OptimizerListSearch<Competencia, OcorrenciaDeVerba>> lista = new ArrayList<OptimizerListSearch<Competencia, OcorrenciaDeVerba>>();
        for (VerbaDeCalculo v : new VerbaDeCalculo[]{a, b, c}) {
            lista.add(v.getOcorrenciasOptimizerListSearch());
        }

        MaquinaDeCalculoDoInss maquina = new MaquinaDeCalculoDoInss(new Inss());
        Method m = MaquinaDeCalculoDoInss.class.getDeclaredMethod(
            "calcularValorBaseVerbas", List.class, Competencia.class, boolean.class);
        m.setAccessible(true);

        base(m, maquina, lista, "2020-03_comum", d(2020, 3, 1), false);
        base(m, maquina, lista, "2020-03_decimoterceiro", d(2020, 3, 1), true);
        base(m, maquina, lista, "2020-12_comum", d(2020, 12, 1), false);
        base(m, maquina, lista, "2020-12_decimoterceiro", d(2020, 12, 1), true);
    }

    static void base(Method m, MaquinaDeCalculoDoInss maquina,
                     List<OptimizerListSearch<Competencia, OcorrenciaDeVerba>> lista,
                     String cenario, Date competencia, boolean decimoTerceiro) throws Exception {
        BigDecimal base = (BigDecimal) m.invoke(maquina, lista, new Competencia(competencia), decimoTerceiro);
        System.out.println(cenario + ";base;" + (base == null ? "" : base.toPlainString()));
    }
}
