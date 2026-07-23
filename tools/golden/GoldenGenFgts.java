import br.jus.trt8.pjecalc.negocio.constantes.AliquotaDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.constantes.TipoDeCorrecaoDoFgtsEnum;
import br.jus.trt8.pjecalc.negocio.dominio.calculo.fgts.OcorrenciaDeFgts;

import java.math.BigDecimal;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Golden da matemática por competência do FGTS (OcorrenciaDeFgts — getters puros):
 * valor devido, diferença, correção pelos dois índices, juros, total e a contribuição
 * social de 0,5%.
 */
public class GoldenGenFgts {
    static final SimpleDateFormat ISO = new SimpleDateFormat("yyyy-MM-dd");
    static Date d(String s) throws Exception { return ISO.parse(s); }
    static BigDecimal b(String s) { return new BigDecimal(s); }

    static class Caso {
        String label, comp, aliq;
        String baseHist, baseVerba, baseSemAviso, depositado, indiceAcum, indiceMulta, taxaJuros;
        Caso(String label, String comp, String aliq, String baseHist, String baseVerba,
             String baseSemAviso, String depositado, String indiceAcum, String indiceMulta, String taxaJuros) {
            this.label = label; this.comp = comp; this.aliq = aliq;
            this.baseHist = baseHist; this.baseVerba = baseVerba; this.baseSemAviso = baseSemAviso;
            this.depositado = depositado; this.indiceAcum = indiceAcum;
            this.indiceMulta = indiceMulta; this.taxaJuros = taxaJuros;
        }
    }

    public static void main(String[] args) throws Exception {
        Caso[] casos = {
            new Caso("simples_8pc",        "2015-03-01", "8", "1000.00", "0.00",   "0.00",   "0.00",  "1.5",  "1.2",  "12"),
            new Caso("com_verbas_e_aviso", "2016-07-01", "8", "1000.00", "500.00", "200.00", "50.00", "1.35", "1.15", "18.5"),
            new Caso("depositado_maior",   "2017-01-01", "8", "1000.00", "0.00",   "0.00",   "500.00","1.5",  "1.2",  "12"),
            new Caso("aliquota_2pc",       "2018-02-01", "2", "3000.00", "250.00", "100.00", "10.00", "1.25", "1.1",  "9"),
            new Caso("contrib_social_05",  "2004-06-01", "8", "2000.00", "300.00", "300.00", "0.00",  "2.4",  "1.8",  "24"),
            new Caso("fora_contrib_social","2010-06-01", "8", "2000.00", "300.00", "300.00", "0.00",  "1.8",  "1.4",  "20"),
            new Caso("sem_juros",          "2019-05-01", "8", "1200.50", "99.50",  "50.00",  "0.00",  "1.1",  "1.05", "0"),
        };

        System.out.println("label;competencia;aliquota;baseHistorico;baseVerba;baseVerbaSemAviso;depositado;indiceAcumulado;indiceAcumuladoDaMulta;taxaDeJuros;valorDevido;valorDevidoSemAviso;diferenca;difCorrigidaLiquidacao;difCorrigidaDemissao;juros;total;contribSocial05");
        for (Caso c : casos) {
            OcorrenciaDeFgts o = new OcorrenciaDeFgts();
            o.setOcorrencia(d(c.comp));
            o.setBaseHistorico(b(c.baseHist));
            o.setBaseVerba(b(c.baseVerba));
            o.setBaseVerbaSemAvisoPrevio(b(c.baseSemAviso));
            o.setAliquotaDoFgtsEnum("2".equals(c.aliq) ? AliquotaDoFgtsEnum.DOIS_POR_CENTO : AliquotaDoFgtsEnum.OITO_POR_CENTO);
            o.setDepositado(b(c.depositado));
            o.setIndiceAcumulado(b(c.indiceAcum));
            o.setIndiceAcumuladoDaMulta(b(c.indiceMulta));
            o.setTaxaDeJuros(b(c.taxaJuros));

            TipoDeCorrecaoDoFgtsEnum liq = TipoDeCorrecaoDoFgtsEnum.PELA_DATA_DE_LIQUIDACAO;
            TipoDeCorrecaoDoFgtsEnum dem = TipoDeCorrecaoDoFgtsEnum.PELA_DATA_DE_DEMISSAO;

            System.out.println(c.label + ";" + c.comp + ";" + c.aliq + ";" + c.baseHist + ";" + c.baseVerba
                + ";" + c.baseSemAviso + ";" + c.depositado + ";" + c.indiceAcum + ";" + c.indiceMulta + ";" + c.taxaJuros
                + ";" + o.getValorDevido().toPlainString()
                + ";" + o.getValorDevidoSemAviso().toPlainString()
                + ";" + o.getDiferenca().toPlainString()
                + ";" + o.getDiferencaCorrigida(liq).toPlainString()
                + ";" + o.getDiferencaCorrigida(dem).toPlainString()
                + ";" + o.getJuros(liq).toPlainString()
                + ";" + o.getTotal(liq).toPlainString()
                + ";" + o.getValorDaContribuicaoSocialDe05().toPlainString());
        }
    }
}
