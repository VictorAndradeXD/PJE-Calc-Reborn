using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.Verbas;

namespace PJeCalc.Tests.Services;

/// <summary>
/// Valida o predicado de feriados contra os dados REAIS exportados do banco de
/// referência do PJe-Calc (TBFERIADO + TBEXCECAOFERIADO): fixos por dia/mês com
/// vigência, móveis por lista de datas, abrangência nacional/estadual/municipal e
/// pontos facultativos opt-in por cálculo.
/// </summary>
public sealed class ProvedorDeFeriadosFixture
{
    public ProvedorDeFeriados Provedor { get; }
    public IReadOnlyList<FeriadoCadastrado> Feriados { get; }

    public ProvedorDeFeriadosFixture()
    {
        Feriados = CarregarDoCsv();
        Provedor = new ProvedorDeFeriados(Feriados);
    }

    public static IReadOnlyList<FeriadoCadastrado> CarregarDoCsv()
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Feriados");
        var excecoesPorFeriado = File.ReadAllLines(Path.Combine(pasta, "feriado_excecao.csv"))
            .Skip(1)
            .Select(l => l.Split(','))
            .GroupBy(c => long.Parse(Sem(c[0])))
            .ToDictionary(g => g.Key, g => (IReadOnlySet<DateOnly>)g.Select(c => DateOnly.Parse(Sem(c[1]))).ToHashSet());

        return File.ReadAllLines(Path.Combine(pasta, "feriado.csv"))
            .Skip(1)
            .Select(l => l.Split(','))
            .Select(c =>
            {
                var id = long.Parse(Sem(c[0]));
                return new FeriadoCadastrado(
                    Tipo: Sem(c[1]) switch { "F" => TipoDeFeriadoEnum.Feriado, "P" => TipoDeFeriadoEnum.PontoFacultativo, _ => TipoDeFeriadoEnum.Bancario },
                    Abrangencia: Sem(c[2]) switch { "F" => AbrangenciaDoFeriadoEnum.Federal, "E" => AbrangenciaDoFeriadoEnum.Estadual, _ => AbrangenciaDoFeriadoEnum.Municipal },
                    Estado: Sem(c[3]) is { Length: > 0 } uf ? uf : null,
                    Municipio: Sem(c[4]) is { Length: > 0 } m ? long.Parse(m) : null,
                    Nome: Sem(c[5]),
                    Data: Sem(c[6]) is { Length: > 0 } d ? DateOnly.Parse(d) : null,
                    InicioVigencia: DateOnly.Parse(Sem(c[7])),
                    FimVigencia: Sem(c[8]) is { Length: > 0 } f ? DateOnly.Parse(f) : null,
                    Movel: Sem(c[9]) == "S",
                    Excecoes: excecoesPorFeriado.TryGetValue(id, out var excecoes) ? excecoes : new HashSet<DateOnly>());
            })
            .ToList();
    }

    private static string Sem(string campo) => campo.Trim('"');
}

public class ProvedorDeFeriadosTests(ProvedorDeFeriadosFixture fixture) : IClassFixture<ProvedorDeFeriadosFixture>
{
    private readonly ProvedorDeFeriadosFixture _fixture = fixture;

    [Fact]
    public void Dados_exportados_carregam_completos()
    {
        Assert.Equal(50, _fixture.Feriados.Count);
        Assert.Equal(448, _fixture.Feriados.Sum(f => f.Excecoes.Count));
    }

    [Theory]
    [InlineData(2021, 9, 7)]   // Independência (terça)
    [InlineData(2021, 1, 1)]   // Confraternização
    [InlineData(2021, 5, 1)]   // Dia do Trabalho (sábado)
    [InlineData(2021, 12, 25)] // Natal
    [InlineData(1995, 11, 15)] // Proclamação — fixos valem em qualquer ano da vigência
    public void Feriados_nacionais_fixos_valem_em_qualquer_calculo(int ano, int mes, int dia)
    {
        var ehFeriado = _fixture.Provedor.ParaCalculo(estado: null, municipio: null);
        Assert.True(ehFeriado(new DateOnly(ano, mes, dia)));
    }

    [Fact]
    public void Dia_comum_nao_eh_feriado()
    {
        var ehFeriado = _fixture.Provedor.ParaCalculo(estado: null, municipio: null);
        Assert.False(ehFeriado(new DateOnly(2021, 3, 10)));
    }

    [Fact]
    public void Moveis_sao_pontos_facultativos_e_so_contam_quando_vinculados_ao_calculo()
    {
        // Corpus Christi 2021: 03/06 — tipo "P" no banco, opt-in por cálculo.
        var corpusChristi = new DateOnly(2021, 6, 3);
        var semVinculo = _fixture.Provedor.ParaCalculo(null, null);
        Assert.False(semVinculo(corpusChristi));

        var corpus = _fixture.Feriados.Single(f => f.Nome == "CORPUS CHRISTI");
        Assert.True(corpus.Movel);
        Assert.Contains(corpusChristi, corpus.Excecoes);

        var comVinculo = _fixture.Provedor.ParaCalculo(null, null, pontosFacultativosDoCalculo: [corpus]);
        Assert.True(comVinculo(corpusChristi));
        Assert.False(comVinculo(new DateOnly(2021, 6, 4)));
    }

    [Fact]
    public void Estadual_do_Para_depende_da_UF_e_do_flag()
    {
        var adesaoDoPara = new DateOnly(2021, 8, 15);
        Assert.True(_fixture.Provedor.ParaCalculo("PA", null)(adesaoDoPara));
        Assert.False(_fixture.Provedor.ParaCalculo("SP", null)(adesaoDoPara));
        Assert.False(_fixture.Provedor.ParaCalculo("PA", null, consideraFeriadoEstadual: false)(adesaoDoPara));
    }

    [Fact]
    public void Municipal_depende_do_municipio_e_respeita_a_vigencia()
    {
        // SÃO JOSÉ — município 447 (Belém/PA), 19/03, vigente desde 1949-03-19.
        var saoJose = _fixture.Feriados.First(f => f.Nome == "SÃO JOSÉ" && f.Municipio == 447);
        var ehFeriadoEmBelem = _fixture.Provedor.ParaCalculo("PA", 447);
        Assert.True(ehFeriadoEmBelem(new DateOnly(2021, 3, 19)));
        Assert.False(ehFeriadoEmBelem(new DateOnly(1948, 3, 19))); // antes da vigência
        Assert.False(_fixture.Provedor.ParaCalculo("PA", 535)(new DateOnly(2021, 3, 19)));
        Assert.False(_fixture.Provedor.ParaCalculo("PA", 447, consideraFeriadoMunicipal: false)(new DateOnly(2021, 3, 19)));
        Assert.False(saoJose.OcorreEm(new DateOnly(1948, 3, 19)));
    }

    [Fact]
    public void Bancarios_nunca_contam()
    {
        // Segunda-feira de Carnaval é tipo "B": 15/02/2021.
        var segundaDeCarnaval = _fixture.Feriados.Single(f => f.Tipo == TipoDeFeriadoEnum.Bancario);
        Assert.True(segundaDeCarnaval.Movel);
        var ehFeriado = _fixture.Provedor.ParaCalculo(null, null);
        foreach (var data in segundaDeCarnaval.Excecoes)
            Assert.False(ehFeriado(data));
    }
}
