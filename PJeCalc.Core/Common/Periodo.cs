namespace PJeCalc.Core.Common;

public class Periodo
{
    public DateTime DataInicial { get; set; }
    public DateTime DataFinal { get; set; }

    public Periodo() { }

    public Periodo(DateTime dataInicial, DateTime dataFinal)
    {
        DataInicial = dataInicial;
        DataFinal = dataFinal;
    }

    public int TotalDeDias()
    {
        return (DataFinal.Date - DataInicial.Date).Days + 1;
    }

    public int TotalDeDiasUteis(bool incluirSabados = false, Func<DateTime, bool>? isFeriado = null)
    {
        int count = 0;
        for (var d = DataInicial.Date; d <= DataFinal.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Sunday)
                continue;
            if (d.DayOfWeek == DayOfWeek.Saturday && !incluirSabados)
                continue;
            if (isFeriado != null && isFeriado(d))
                continue;
            count++;
        }
        return count;
    }

    public bool Intersecta(Periodo outro)
    {
        return DataInicial <= outro.DataFinal && DataFinal >= outro.DataInicial;
    }

    public Periodo? Interseccao(Periodo outro)
    {
        if (!Intersecta(outro))
            return null;

        var inicio = DataInicial > outro.DataInicial ? DataInicial : outro.DataInicial;
        var fim = DataFinal < outro.DataFinal ? DataFinal : outro.DataFinal;
        return new Periodo(inicio, fim);
    }

    public bool ContemData(DateTime data)
    {
        return data.Date >= DataInicial.Date && data.Date <= DataFinal.Date;
    }

    public bool IsCompleto()
    {
        return DataInicial != default && DataFinal != default;
    }

    public bool IsMesmoPeriodo(Periodo outro)
    {
        return DataInicial.Date == outro.DataInicial.Date && DataFinal.Date == outro.DataFinal.Date;
    }

    public bool IsDatasDoMesmoMes()
    {
        return DataInicial.Year == DataFinal.Year && DataInicial.Month == DataFinal.Month;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Periodo outro) return false;
        return DataInicial.Date == outro.DataInicial.Date && DataFinal.Date == outro.DataFinal.Date;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DataInicial.Date, DataFinal.Date);
    }

    public override string ToString()
    {
        return $"{DataInicial:dd/MM/yyyy} - {DataFinal:dd/MM/yyyy}";
    }
}
