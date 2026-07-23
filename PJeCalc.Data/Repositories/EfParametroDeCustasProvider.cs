using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Services.Custas;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>
/// Carrega os parâmetros tabelados das custas do banco de referência e os expõe como
/// <see cref="ParametrosDeCustasFixas"/> para a apuração das custas.
/// </summary>
public sealed class EfParametroDeCustasProvider(ReferenciaDbContext contexto)
{
    private readonly ReferenciaDbContext _contexto = contexto;

    /// <summary>Parâmetro vigente na data informada (o registro cuja vigência a contém).</summary>
    public ParametrosDeCustasFixas ObterPorData(DateOnly data)
    {
        var dataHora = data.ToDateTime(TimeOnly.MinValue);
        var registro = _contexto.ParametrosDeCustas
            .AsNoTracking()
            .Where(p => p.InicioVigencia <= dataHora && (p.FimVigencia == null || dataHora <= p.FimVigencia))
            .OrderByDescending(p => p.InicioVigencia)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Sem parâmetros de custas vigentes em {data:yyyy-MM-dd}.");

        return new ParametrosDeCustasFixas
        {
            PisoConhecimento = registro.PisoConhecimento,
            TetoLiquidacao = registro.TetoLiquidacao,
            TetoAutos = registro.TetoAutos,
            AtosUrbanos = registro.AtosUrbanos,
            AtosRurais = registro.AtosRurais,
            AgravoInstrumento = registro.AgravoInstrumento,
            AgravoPeticao = registro.AgravoPeticao,
            ImpugnacaoSentenca = registro.ImpugnacaoSentenca,
            EmbargosArrematacao = registro.EmbargosArrematacao,
            EmbargosExecucao = registro.EmbargosExecucao,
            EmbargosTerceiros = registro.EmbargosTerceiros,
            RecursoRevista = registro.RecursoRevista,
        };
    }
}
