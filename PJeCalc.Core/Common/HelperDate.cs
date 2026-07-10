namespace PJeCalc.Core.Common;

public class HelperDate : IComparable<HelperDate>, IEquatable<HelperDate>
{
    public DateTime Date { get; private set; }

    public int Day => Date.Day;
    public int Month => Date.Month;
    public int Year => Date.Year;
    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    private HelperDate(DateTime date)
    {
        Date = date.Date;
    }

    public static HelperDate Of(int year, int month, int day)
    {
        return new HelperDate(new DateTime(year, month, day));
    }

    public static HelperDate Of(DateTime date)
    {
        return new HelperDate(date);
    }

    public HelperDate AddDays(int days) => new(Date.AddDays(days));
    public HelperDate AddMonths(int months) => new(Date.AddMonths(months));
    public HelperDate AddYears(int years) => new(Date.AddYears(years));
    public HelperDate SubtractDays(int days) => new(Date.AddDays(-days));

    public bool IsBefore(HelperDate other) => Date < other.Date;
    public bool IsBeforeOrEqual(HelperDate other) => Date <= other.Date;
    public bool IsAfter(HelperDate other) => Date > other.Date;
    public bool IsAfterOrEqual(HelperDate other) => Date >= other.Date;
    public bool IsBetween(HelperDate start, HelperDate end) => Date >= start.Date && Date <= end.Date;

    public bool IsSaturday => DayOfWeek == DayOfWeek.Saturday;
    public bool IsSunday => DayOfWeek == DayOfWeek.Sunday;
    public bool IsWeekend => IsSaturday || IsSunday;

    public bool IsHoliday => Feriados.IsFeriadoNacional(Date);

    public bool IsWorkDay(bool includeSaturdays)
    {
        if (IsSunday) return false;
        if (IsHoliday) return false;
        if (IsSaturday && !includeSaturdays) return false;
        return true;
    }

    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

    public HelperDate LastDayOfMonth() => Of(Year, Month, DaysInMonth);

    public HelperDate FirstDayOfMonth() => Of(Year, Month, 1);

    public int SubtractDays(HelperDate other) => (Date - other.Date).Days;

    public int SubtractMonths(HelperDate other)
    {
        return (Year - other.Year) * 12 + (Month - other.Month);
    }

    public static int TotalWorkDays(DateTime start, DateTime end, bool includeSaturdays, Func<DateTime, bool>? isHoliday = null)
    {
        int count = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Sunday)
                continue;
            if (d.DayOfWeek == DayOfWeek.Saturday && !includeSaturdays)
                continue;
            if (isHoliday != null && isHoliday(d))
                continue;
            if (Feriados.IsFeriadoNacional(d))
                continue;
            count++;
        }
        return count;
    }

    public static int CountDays(DateTime start, DateTime end)
    {
        return (end.Date - start.Date).Days;
    }

    public static int CountMonths(DateTime start, DateTime end)
    {
        return (end.Year - start.Year) * 12 + (end.Month - start.Month);
    }

    public static int CountYears(DateTime start, DateTime end)
    {
        return end.Year - start.Year;
    }

    public static List<Periodo> BreakInMonths(DateTime start, DateTime end)
    {
        var periodos = new List<Periodo>();
        var current = start.Date;

        while (current <= end.Date)
        {
            var lastDayOfMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));
            var periodEnd = lastDayOfMonth < end.Date ? lastDayOfMonth : end.Date;
            periodos.Add(new Periodo(current, periodEnd));
            current = periodEnd.AddDays(1);
        }

        return periodos;
    }

    public static bool DateEquals(DateTime a, DateTime b) => a.Date == b.Date;
    public static bool DateBefore(DateTime a, DateTime b) => a.Date < b.Date;
    public static bool DateBeforeOrEquals(DateTime a, DateTime b) => a.Date <= b.Date;
    public static bool DateAfter(DateTime a, DateTime b) => a.Date > b.Date;
    public static bool DateAfterOrEquals(DateTime a, DateTime b) => a.Date >= b.Date;

    public string Format(string format) => Date.ToString(format);

    public int CompareTo(HelperDate? other)
    {
        if (other is null) return 1;
        return Date.CompareTo(other.Date);
    }

    public bool Equals(HelperDate? other)
    {
        if (other is null) return false;
        return Date == other.Date;
    }

    public override bool Equals(object? obj) => Equals(obj as HelperDate);

    public override int GetHashCode() => Date.GetHashCode();

    public override string ToString() => Date.ToString("dd/MM/yyyy");

    public static bool operator ==(HelperDate? left, HelperDate? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(HelperDate? left, HelperDate? right) => !(left == right);
    public static bool operator <(HelperDate left, HelperDate right) => left.Date < right.Date;
    public static bool operator >(HelperDate left, HelperDate right) => left.Date > right.Date;
    public static bool operator <=(HelperDate left, HelperDate right) => left.Date <= right.Date;
    public static bool operator >=(HelperDate left, HelperDate right) => left.Date >= right.Date;

    private static class Feriados
    {
        private static readonly HashSet<(int Month, int Day)> FeriadosFixos = new()
        {
            (1, 1),   // Confraternizacao Universal
            (4, 21),  // Tiradentes
            (5, 1),   // Dia do Trabalho
            (9, 7),   // Independencia do Brasil
            (10, 12), // Nossa Senhora Aparecida
            (11, 2),  // Finados
            (11, 15), // Proclamacao da Republica
            (12, 25), // Natal
        };

        public static bool IsFeriadoNacional(DateTime date)
        {
            if (FeriadosFixos.Contains((date.Month, date.Day)))
                return true;

            return IsPascoa(date) || IsCarnaval(date) || IsSextaFeiraSanta(date) || IsCorpusChristi(date);
        }

        private static DateTime CalcularPascoa(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(year, month, day);
        }

        private static bool IsPascoa(DateTime date)
        {
            var pascoa = CalcularPascoa(date.Year);
            return date.Date == pascoa.Date;
        }

        private static bool IsCarnaval(DateTime date)
        {
            var pascoa = CalcularPascoa(date.Year);
            var terCarnaval = pascoa.AddDays(-47);
            var segCarnaval = pascoa.AddDays(-48);
            return date.Date == terCarnaval.Date || date.Date == segCarnaval.Date;
        }

        private static bool IsSextaFeiraSanta(DateTime date)
        {
            var pascoa = CalcularPascoa(date.Year);
            return date.Date == pascoa.AddDays(-2).Date;
        }

        private static bool IsCorpusChristi(DateTime date)
        {
            var pascoa = CalcularPascoa(date.Year);
            return date.Date == pascoa.AddDays(60).Date;
        }
    }
}
