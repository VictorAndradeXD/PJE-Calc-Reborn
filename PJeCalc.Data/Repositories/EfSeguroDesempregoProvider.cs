using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Services.SeguroDesemprego;
using PJeCalc.Data.Context;

namespace PJeCalc.Data.Repositories;

/// <summary>Carrega a tabela do seguro-desemprego do banco de referência, por competência.</summary>
public sealed class EfSeguroDesempregoProvider(ReferenciaDbContext contexto)
{
    private readonly ReferenciaDbContext _contexto = contexto;

    /// <summary>
    /// Tabela vigente na data da demissão: o registro mais recente cuja competência não a
    /// ultrapassa (como <c>obterTabelaDa</c> do original).
    /// </summary>
    public TabelaSeguroDesemprego? ObterPara(DateOnly dataDemissao)
    {
        var limite = new DateTime(dataDemissao.Year, dataDemissao.Month, 1);
        var registro = _contexto.SeguroDesemprego.AsNoTracking()
            .Where(t => t.Competencia <= limite)
            .OrderByDescending(t => t.Competencia)
            .FirstOrDefault();

        return registro is null
            ? null
            : new TabelaSeguroDesemprego
            {
                Competencia = DateOnly.FromDateTime(registro.Competencia),
                FinalFaixa1 = registro.FinalFaixa1,
                PercentualFaixa1 = registro.PercentualFaixa1,
                PercentualFaixa2 = registro.PercentualFaixa2,
                SomaFaixa2 = registro.SomaFaixa2,
                Piso = registro.Piso,
                Teto = registro.Teto,
            };
    }
}
