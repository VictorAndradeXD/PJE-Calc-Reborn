using System.Globalization;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Fgts;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Validação dos totais agregados do FGTS e da multa (20%/40%, art. 467 da CLT e 10% da
/// LC 110/2001) contra o motor oficial do PJe-Calc (Java). Usa o mesmo conjunto de três
/// competências que gerou os golden.
/// </summary>
public class FgtsMultaGoldenTests
{
    private const decimal IndiceMulta = 1.25m;
    private const decimal IndiceMulta467 = 1.30m;

    private static TotaisDeFgts Totais() => new(
    [
        Ocorrencia("2015-03-01", 1000m, 200m, 100m, 0m,  1.50m, 1.20m, 12m),
        Ocorrencia("2015-04-01", 1000m, 0m,   0m,   20m, 1.45m, 1.18m, 11.5m),
        Ocorrencia("2015-05-01", 1500m, 300m, 150m, 0m,  1.40m, 1.15m, 11m),
    ]);

    private static ApuracaoMensalDeFgts Ocorrencia(
        string competencia, decimal baseHistorico, decimal baseVerba, decimal baseSemAviso,
        decimal depositado, decimal indiceAcumulado, decimal indiceMulta, decimal taxaJuros) => new()
        {
            Competencia = DateOnly.ParseExact(competencia, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Aliquota = AliquotaDoFgtsEnum.OitoPorCento,
            BaseHistorico = baseHistorico,
            BaseVerba = baseVerba,
            BaseVerbaSemAvisoPrevio = baseSemAviso,
            Depositado = depositado,
            IndiceAcumulado = indiceAcumulado,
            IndiceAcumuladoDaMulta = indiceMulta,
            TaxaDeJuros = taxaJuros,
        };

    [Fact]
    public void Totais_agregados_batem_com_o_motor_oficial()
    {
        var t = Totais();
        const TipoDeCorrecaoDoFgtsEnum liq = TipoDeCorrecaoDoFgtsEnum.PelaDataDeLiquidacao;
        const TipoDeCorrecaoDoFgtsEnum dem = TipoDeCorrecaoDoFgtsEnum.PelaDataDeDemissao;

        Assert.Equal(320.00m, t.Devido);
        Assert.Equal(461.60m, t.DevidoCorrigido(liq));
        Assert.Equal(432.80m, t.DevidoSemAvisoCorrigido(liq));
        Assert.Equal(300.00m, t.Diferenca);
        Assert.Equal(432.60m, t.DiferencaCorrigida(liq));
        Assert.Equal(403.80m, t.DiferencaSemAvisoCorrigida(liq));
        Assert.Equal(49.46m, t.Juros(liq));
        Assert.Equal(375.20m, t.DevidoCorrigido(dem));
    }

    public static IEnumerable<object[]> Golden()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_fgts_multa.csv");
        foreach (var linha in File.ReadAllLines(caminho).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            var c = linha.Split(';');
            yield return
            [
                Enum.Parse<IncidenciaDeMultaDoFgtsEnum>(c[0]),
                Enum.Parse<ValorDaMultaDoFgtsEnum>(c[1]),
                bool.Parse(c[2]), bool.Parse(c[3]), bool.Parse(c[4]),
                Num(c[5]), Num(c[6]), Num(c[7]), Num(c[8]), Num(c[9]),
            ];
        }
    }

    private static decimal Num(string s) => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    [Theory]
    [MemberData(nameof(Golden))]
    public void Multa_bate_com_o_motor_oficial(
        IncidenciaDeMultaDoFgtsEnum incidencia, ValorDaMultaDoFgtsEnum percentual,
        bool excluirAviso, bool multa467, bool multa10,
        decimal baseEsperada, decimal valorEsperado, decimal corrigidaEsperada,
        decimal valor467Esperado, decimal valor10Esperado)
    {
        var r = MultaDoFgts.Calcular(Totais(), new ParametrosDaMultaDoFgts
        {
            Incidencia = incidencia,
            Percentual = percentual,
            ExcluirAvisoPrevio = excluirAviso,
            MultaDoArtigo467 = multa467,
            Multa10 = multa10,
            IndiceMulta = IndiceMulta,
            IndiceMulta467 = IndiceMulta467,
        });

        Assert.Equal(baseEsperada, r.Base);
        Assert.Equal(valorEsperado, r.Valor);
        Assert.Equal(corrigidaEsperada, r.ValorCorrigido);
        Assert.Equal(valor467Esperado, r.ValorDoArtigo467);
        Assert.Equal(valor10Esperado, r.ValorDaMulta10);
    }

    [Fact]
    public void Multa_informada_ignora_a_base_calculada()
    {
        var r = MultaDoFgts.Calcular(Totais(), new ParametrosDaMultaDoFgts
        {
            TipoDoValor = TipoDeBaseDoFgtsEnum.Informada,
            ValorInformado = 1000m,
            IndiceMulta = IndiceMulta,
        });

        Assert.Equal(0m, r.Base);
        Assert.Equal(1000m, r.Valor);
        Assert.Equal(1250.00m, r.ValorCorrigido);
    }

    [Fact]
    public void Sem_multa_zera_tudo()
    {
        var r = MultaDoFgts.Calcular(Totais(), new ParametrosDaMultaDoFgts
        {
            Aplicar = false,
            MultaDoArtigo467 = true,
            IndiceMulta = IndiceMulta,
        });

        Assert.Equal(0m, r.Valor);
        Assert.Equal(0m, r.ValorDoArtigo467);
    }
}
