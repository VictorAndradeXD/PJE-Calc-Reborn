using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Feriado cadastrado, no modelo do PJe-Calc: <b>fixo</b> ocorre no dia/mês de
/// <see cref="Data"/> em todo ano da vigência (o ano do cadastro é irrelevante), exceto
/// nas datas listadas em <see cref="Excecoes"/>; <b>móvel</b> ocorre apenas nas datas
/// exatas de <see cref="Excecoes"/> (uma por ano, pré-carregadas — não há cálculo de
/// Páscoa no motor).
/// </summary>
public sealed record FeriadoCadastrado(
    TipoDeFeriadoEnum Tipo,
    AbrangenciaDoFeriadoEnum Abrangencia,
    string? Estado,
    long? Municipio,
    string Nome,
    DateOnly? Data,
    DateOnly InicioVigencia,
    DateOnly? FimVigencia,
    bool Movel,
    IReadOnlySet<DateOnly> Excecoes)
{
    public bool OcorreEm(DateOnly data)
    {
        if (data < InicioVigencia || (FimVigencia is { } fim && data > fim))
            return false;
        if (Movel)
            return Excecoes.Contains(data);
        return Data is { } fixo && fixo.Day == data.Day && fixo.Month == data.Month &&
               !Excecoes.Contains(data);
    }
}

/// <summary>
/// Resolve os feriados de um cálculo: nacionais sempre; estaduais/municipais conforme os
/// flags e a UF/município do processo; pontos facultativos apenas os explicitamente
/// vinculados ao cálculo (bancários nunca contam). Produz o predicado consumido por
/// <see cref="ContextoDeVerbas.EhFeriado"/>.
/// </summary>
public sealed class ProvedorDeFeriados(IReadOnlyList<FeriadoCadastrado> feriados)
{
    private readonly IReadOnlyList<FeriadoCadastrado> _feriados = feriados;

    public Func<DateOnly, bool> ParaCalculo(
        string? estado,
        long? municipio,
        bool consideraFeriadoEstadual = true,
        bool consideraFeriadoMunicipal = true,
        IReadOnlyCollection<FeriadoCadastrado>? pontosFacultativosDoCalculo = null)
    {
        var aplicaveis = _feriados.Where(f =>
                f.Tipo == TipoDeFeriadoEnum.Feriado &&
                AbrangenciaPermitida(f.Abrangencia, consideraFeriadoEstadual, consideraFeriadoMunicipal) &&
                (f.Estado is null || f.Estado == estado) &&
                (f.Municipio is null || f.Municipio == municipio))
            .Concat(pontosFacultativosDoCalculo ?? [])
            .ToList();
        return data => aplicaveis.Any(f => f.OcorreEm(data));
    }

    private static bool AbrangenciaPermitida(AbrangenciaDoFeriadoEnum abrangencia, bool estadual, bool municipal) =>
        abrangencia switch
        {
            AbrangenciaDoFeriadoEnum.Federal => true,
            AbrangenciaDoFeriadoEnum.Estadual => estadual,
            _ => municipal,
        };
}
