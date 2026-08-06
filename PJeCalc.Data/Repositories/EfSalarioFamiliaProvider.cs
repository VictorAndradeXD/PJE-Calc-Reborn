using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Services.SalarioFamilia;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>
/// Carrega a tabela do salário-família do banco de referência, por competência.
/// </summary>
public sealed class EfSalarioFamiliaProvider(ReferenciaDbContext contexto)
{
    private readonly ReferenciaDbContext _contexto = contexto;

    /// <summary>Tabela vigente na competência (dia 1 do mês), ou nula quando não houver.</summary>
    public TabelaSalarioFamilia? ObterPorCompetencia(DateOnly competencia)
    {
        var mes = new DateTime(competencia.Year, competencia.Month, 1);
        var registro = _contexto.SalarioFamilia.AsNoTracking()
            .FirstOrDefault(t => t.Competencia == mes);

        return registro is null
            ? null
            : new TabelaSalarioFamilia
            {
                Competencia = DateOnly.FromDateTime(registro.Competencia),
                FinalFaixa1 = registro.FinalFaixa1,
                CotaFaixa1 = registro.CotaFaixa1,
                FinalFaixa2 = registro.FinalFaixa2,
                CotaFaixa2 = registro.CotaFaixa2,
            };
    }
}
