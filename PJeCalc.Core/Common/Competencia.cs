namespace PJeCalc.Core.Common;

public class Competencia
{
    public int Mes { get; set; }
    public int Ano { get; set; }

    public Competencia() { }

    public Competencia(int mes, int ano)
    {
        Mes = mes;
        Ano = ano;
    }

    public Periodo CriarPeriodoDaCompetencia()
    {
        var dataInicial = new DateTime(Ano, Mes, 1);
        var dataFinal = new DateTime(Ano, Mes, DateTime.DaysInMonth(Ano, Mes));
        return new Periodo(dataInicial, dataFinal);
    }

    public bool IsAnteriorA(DateTime data)
    {
        var ultimoDia = new DateTime(Ano, Mes, DateTime.DaysInMonth(Ano, Mes));
        return ultimoDia < data.Date;
    }

    public bool IsPosteriorA(DateTime data)
    {
        var primeiroDia = new DateTime(Ano, Mes, 1);
        return primeiroDia > data.Date;
    }

    public bool ContemData(DateTime data)
    {
        return data.Year == Ano && data.Month == Mes;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Competencia outra) return false;
        return Mes == outra.Mes && Ano == outra.Ano;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Mes, Ano);
    }

    public override string ToString()
    {
        return $"{Mes:D2}/{Ano:D4}";
    }
}
