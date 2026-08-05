import br.jus.trt8.pjecalc.base.comum.Utils;
import br.jus.trt8.pjecalc.negocio.constantes.BaseDeJurosDasVerbasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaApuracaoDeMultaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CaracteristicaDaVerbaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.CredorDevedorMultaEnum;
import br.jus.trt8.pjecalc.negocio.constantes.JurosDoAjuizamentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.JurosEnum;
import br.jus.trt8.pjecalc.negocio.constantes.LogicoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoValorEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.Calculo;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.atualizacao.ParametrosDeAtualizacao;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.Fgts;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaApuracaoDeHonorarioEnum;
import br.jus.trt8.pjecalc.negocio.constantes.BaseParaCustasCalculadasEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCustasDeConhecimentoEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeDevedorDoHonorarioEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.CustasJudiciais;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.custas.MaquinaDeCalculoDeCustas;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.honorarios.Honorario;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.honorarios.MaquinaDeCalculoDeHonorarios;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.multa.MaquinaDeCalculoDeMulta;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.multa.Multa;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.salariofamilia.SalarioFamilia;
import br.jus.trt8.pjecalc.negocio.dominio.custas.ParametrosDeCustasFixas;
import br.jus.trt8.pjecalc.negocio.dominio.custas.RepositorioDeParametrosDeCustasFixas;
import br.jus.trt8.pjecalc.negocio.dominio.inss.seguradoempregado.RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado;
import br.jus.trt8.pjecalc.negocio.dominio.inss.seguradoempregado.TabelaPrevidenciariaSeguradoEmpregado;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.segurodesemprego.SeguroDesemprego;
import br.jus.trt8.pjecalc.negocio.dominio.ocorrenciaverba.OcorrenciaDeVerba;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.Informada;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.RepositorioDeVerbaCalculo;
import br.jus.trt8.pjecalc.negocio.dominio.verbacalculo.VerbaDeCalculo;

import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Date;
import java.util.GregorianCalendar;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * Golden do orquestrador (caminho mínimo): verbas -> apuração de juros -> multas -> bruto devido
 * ao reclamante. Reúne verbas/juros (via apurarJurosDasVerbas por reflection) e multas que
 * consomem o principal apurado, e lê Calculo.calcularBrutoDevidoAoReclamante().
 * Saída: "cenario;chave;valor".
 */
public class GoldenGenOrquestrador {

    static Date d(int ano, int mes, int dia) { return new GregorianCalendar(ano, mes - 1, dia).getTime(); }
    static String p(BigDecimal v) { return v == null ? "" : v.toPlainString(); }
    static void row(String c, String k, BigDecimal v) { System.out.println(c + ";" + k + ";" + p(v)); }

    static class CalcStub extends Calculo {
        List<VerbaDeCalculo> ativas = new ArrayList<VerbaDeCalculo>();
        Set<Multa> multas = new LinkedHashSet<Multa>();
        Set<Honorario> honorarios = new LinkedHashSet<Honorario>();
        CustasJudiciais custas;
        Fgts fgtsStub = new Fgts() { @Override public boolean isComporOPrincipal() { return false; } };
        SalarioFamilia salarioFamiliaStub = new SalarioFamilia();
        SeguroDesemprego seguroDesempregoStub = new SeguroDesemprego();

        @Override public List<VerbaDeCalculo> getVerbasAtivas() { return ativas; }
        @Override public Set<Multa> getMultasDoCalculo() { return multas; }
        @Override public Set<Honorario> getHonorariosDoCalculo() { return honorarios; }
        @Override public Fgts getFgts() { return fgtsStub; }
        @Override public SalarioFamilia getSalarioFamilia() { return salarioFamiliaStub; }
        @Override public SeguroDesemprego getSeguroDesemprego() { return seguroDesempregoStub; }
        @Override public CustasJudiciais getCustasJudiciais() { return custas; }
        @Override public Boolean isCalculoExterno() { return Boolean.FALSE; }
    }

    static ParametrosDeCustasFixas novoParametro() {
        ParametrosDeCustasFixas x = new ParametrosDeCustasFixas();
        x.setDataInicio(d(2002, 9, 27)); x.setDataFim(null);
        x.setValorPisoCustasConhecimento(new BigDecimal("10.64"));
        x.setValorTetoCustasLiquidacao(new BigDecimal("638.46"));
        x.setValorTetoCustasDeAutos(new BigDecimal("1915.38"));
        x.setValorAtosUrbanosOficialJustica(new BigDecimal("11.06"));
        x.setValorAtosRuraisOficialJustica(new BigDecimal("22.13"));
        x.setValorAgravoDeInstrumento(new BigDecimal("44.26"));
        x.setValorAgravoDePeticao(new BigDecimal("44.26"));
        x.setValorImpugnacaoSentencaDeLiquidacao(new BigDecimal("55.35"));
        x.setValorEmbargosAArrematacao(new BigDecimal("44.26"));
        x.setValorEmbargosAExecucao(new BigDecimal("44.26"));
        x.setValorEmbargosDeTerceiros(new BigDecimal("44.26"));
        x.setValorRecursoDeRevista(new BigDecimal("55.35"));
        return x;
    }

    static class RepoParametroStub extends RepositorioDeParametrosDeCustasFixas {
        @Override public ParametrosDeCustasFixas obterPorData(Date data) { return novoParametro(); }
        @Override public ParametrosDeCustasFixas obterRegistroMaisAntigo() { return novoParametro(); }
    }

    static class RepoTetoStub extends RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado {
        @Override public TabelaPrevidenciariaSeguradoEmpregado obter(Date competencia) {
            return new TabelaPrevidenciariaSeguradoEmpregado() {
                @Override public BigDecimal getValorTetoBeneficio() { return new BigDecimal("6433.57"); }
            };
        }
        @Override public TabelaPrevidenciariaSeguradoEmpregado obterAtual() { return obter(null); }
    }

    static class RepoVerbaStub extends RepositorioDeVerbaCalculo {
        @Override public void adicionarEmOcorrencias(VerbaDeCalculo verba, OcorrenciaDeVerba filho) {
            filho.setVerbaDeCalculo(verba);
            verba.getOcorrencias().add(filho);
        }
        @Override public void marcarComoAlterada(VerbaDeCalculo v) { }
        @Override public void desmarcarComoAlterada(VerbaDeCalculo v) { }
    }

    static void oc(VerbaDeCalculo v, Date ini, Date fim, String devido, String pago, String indice) {
        OcorrenciaDeVerba o = new OcorrenciaDeVerba();
        o.setDataInicial(ini); o.setDataFinal(fim);
        o.setDevido(new BigDecimal(devido)); o.setPago(new BigDecimal(pago));
        o.setIndiceAcumulado(new BigDecimal(indice)); o.setAtivo(Boolean.TRUE);
        v.adicionarEmOcorrencias(o);
    }

    static VerbaDeCalculo verba(Calculo calc, CaracteristicaDaVerbaEnum car, JurosDoAjuizamentoEnum jaz) {
        Informada v = new Informada();
        v.setCalculo(calc); v.setAtivo(Boolean.TRUE); v.setComporPrincipal(LogicoEnum.SIM);
        v.setJurosDoAjuizamento(jaz); v.setCaracteristica(car);
        v.setIncidenciaINSS(Boolean.FALSE); v.setIncidenciaIRPF(Boolean.FALSE);
        v.setIncidenciaPrevidenciaPrivada(Boolean.FALSE);
        return v;
    }

    public static void main(String[] args) throws Exception {
        Utils.iniciarTeste();
        Utils.adicionarRepositorioParaTeste(RepositorioDeVerbaCalculo.class, new RepoVerbaStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeParametrosDeCustasFixas.class, new RepoParametroStub());
        Utils.adicionarRepositorioParaTeste(RepositorioDeTabelaPrevidenciariaDoSeguradoEmpregado.class, new RepoTetoStub());

        ParametrosDeAtualizacao pa = new ParametrosDeAtualizacao();
        pa.setJuros(JurosEnum.JUROS_UM_PORCENTO);
        pa.setAplicarJurosFasePreJudicial(Boolean.FALSE);
        pa.setCombinarOutroJuros(Boolean.FALSE);
        pa.setListaDeCombinacaoDeJuros(new LinkedHashSet());
        pa.setBaseDeJurosDasVerbas(BaseDeJurosDasVerbasEnum.VERBAS);
        pa.setCorrecaoDasCustas(Boolean.FALSE);

        Date ajuizamento = d(2019, 6, 1);
        Date liquidacao = d(2021, 6, 1);

        CalcStub calc = new CalcStub();
        calc.setParametrosDeAtualizacao(pa);
        calc.setDataAjuizamento(ajuizamento);
        calc.setDataDeLiquidacao(liquidacao);
        calc.setDataAdmissao(d(2018, 1, 1));

        // Verbas (mesmas do golden de apuração de juros).
        VerbaDeCalculo a = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(a, d(2019, 3, 1), d(2019, 3, 31), "1000.00", "0", "1.2");
        oc(a, d(2019, 8, 1), d(2019, 8, 31), "500.00", "0", "1.1");
        VerbaDeCalculo b = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(b, d(2019, 3, 1), d(2019, 3, 31), "200.00", "0", "1.0");
        VerbaDeCalculo cc = verba(calc, CaracteristicaDaVerbaEnum.FERIAS, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS);
        oc(cc, d(2020, 1, 1), d(2020, 1, 31), "3000.00", "500.00", "1.05");
        VerbaDeCalculo dd = verba(calc, CaracteristicaDaVerbaEnum.COMUM, JurosDoAjuizamentoEnum.OCORRENCIAS_VENCIDAS_E_VINCENDAS);
        oc(dd, d(2020, 5, 1), d(2020, 5, 31), "800.00", "0", "1.0");
        calc.ativas.add(a); calc.ativas.add(b); calc.ativas.add(cc); calc.ativas.add(dd);
        calc.setVerbas(new LinkedHashSet<VerbaDeCalculo>(calc.ativas));

        // Apuração de juros.
        Method m = Calculo.class.getDeclaredMethod("apurarJurosDasVerbas");
        m.setAccessible(true);
        m.invoke(calc);

        BigDecimal totalCorrigido = calc.getTotalDeValorCorrigidoDaApuracaoDeJuros();
        BigDecimal totalJuros = calc.getTotalDeJurosDaApuracaoDeJuros();
        row("APURACAO", "totalCorrigido", totalCorrigido);
        row("APURACAO", "totalJuros", totalJuros);

        // Multas que consomem o principal apurado.
        Multa rtrd = new Multa();
        rtrd.setCalculo(calc);
        rtrd.setTipoCredorDevedor(CredorDevedorMultaEnum.RECLAMANTE_RECLAMADO);
        rtrd.setTipoValorDaMulta(TipoValorEnum.CALCULADO);
        rtrd.setTipoBaseMulta(BaseParaApuracaoDeMultaEnum.PRINCIPAL);
        rtrd.setAliquotaMulta(new BigDecimal("10"));
        new MaquinaDeCalculoDeMulta(rtrd).liquidar();

        Multa rdrt = new Multa();
        rdrt.setCalculo(calc);
        rdrt.setTipoCredorDevedor(CredorDevedorMultaEnum.RECLAMADO_RECLAMANTE);
        rdrt.setTipoValorDaMulta(TipoValorEnum.INFORMADO);
        rdrt.setValorMulta(new BigDecimal("500.00"));
        rdrt.setDataVencimentoMulta(liquidacao);
        new MaquinaDeCalculoDeMulta(rdrt).liquidar();

        calc.multas.add(rtrd);
        calc.multas.add(rdrt);

        row("MULTAS", "reclamanteReclamado", calc.getValorTotalMultasDoTipoReclamanteReclamado());
        row("MULTAS", "reclamadoReclamante", calc.getValorTotalMultasDoTipoReclamadoReclamante());

        // Bruto devido ao reclamante.
        BigDecimal bruto = calc.calcularBrutoDevidoAoReclamante();
        row("BRUTO", "brutoDevidoAoReclamante", bruto);

        // Honorário sucumbencial 10% sobre o bruto (devido pelo reclamado), sem IRRF.
        Honorario hon = new Honorario();
        hon.setCalculo(calc);
        hon.setApurarIRRF(Boolean.FALSE);
        hon.setApurarIRPFSobreJuros(Boolean.FALSE);
        hon.setTipoDeDevedor(TipoDeDevedorDoHonorarioEnum.RECLAMADO);
        hon.setTipoValor(TipoValorEnum.CALCULADO);
        hon.setBaseParaApuracao(BaseParaApuracaoDeHonorarioEnum.BRUTO);
        hon.setAliquota(new BigDecimal("10"));
        new MaquinaDeCalculoDeHonorarios(hon).liquidar();
        calc.honorarios.add(hon);
        row("HONORARIO", "valorTotal", hon.getValorTotal());
        row("HONORARIO", "devidoPeloReclamado", calc.getValorTotalHonorariosDevidosPeloReclamado());

        // Custas de conhecimento do reclamado (2%) sobre o bruto (base BR).
        CustasJudiciais cj = new CustasJudiciais();
        cj.setCalculo(calc);
        cj.setBaseParaCustasCalculadas(BaseParaCustasCalculadasEnum.BRUTO_DEVIDO_AO_RECLAMANTE);
        cj.setTipoDeCustasDeConhecimentoDoReclamante(TipoDeCustasDeConhecimentoEnum.NAO_SE_APLICA);
        cj.setTipoDeCustasDeConhecimentoDoReclamado(TipoDeCustasDeConhecimentoEnum.CALCULADA_2_POR_CENTO);
        calc.custas = cj;
        new MaquinaDeCalculoDeCustas(cj).liquidar();
        row("CUSTAS", "base", cj.getValorBaseCustasCalculadas());
        row("CUSTAS", "consolidado", cj.encontrarValorConsolidadoDoReclamado());
    }
}
