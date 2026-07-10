using PJeCalc.Core.Common;

namespace PJeCalc.Tests.Common;

public class HelperDateTests
{
    [Fact]
    public void Of_CreatesCorrectDate()
    {
        // Arrange & Act
        var date = HelperDate.Of(2024, 3, 15);

        // Assert
        Assert.Equal(2024, date.Year);
        Assert.Equal(3, date.Month);
        Assert.Equal(15, date.Day);
    }

    [Fact]
    public void Of_FromDateTime_CreatesCorrectDate()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 20, 14, 30, 0);

        // Act
        var date = HelperDate.Of(dateTime);

        // Assert
        Assert.Equal(2024, date.Year);
        Assert.Equal(6, date.Month);
        Assert.Equal(20, date.Day);
    }

    [Fact]
    public void AddDays_Works()
    {
        // Arrange
        var date = HelperDate.Of(2024, 1, 1);

        // Act
        var result = date.AddDays(10);

        // Assert
        Assert.Equal(HelperDate.Of(2024, 1, 11), result);
    }

    [Fact]
    public void AddMonths_Works()
    {
        // Arrange
        var date = HelperDate.Of(2024, 1, 15);

        // Act
        var result = date.AddMonths(3);

        // Assert
        Assert.Equal(HelperDate.Of(2024, 4, 15), result);
    }

    [Fact]
    public void AddYears_Works()
    {
        // Arrange
        var date = HelperDate.Of(2024, 6, 1);

        // Act
        var result = date.AddYears(2);

        // Assert
        Assert.Equal(HelperDate.Of(2026, 6, 1), result);
    }

    [Fact]
    public void SubtractDays_Works()
    {
        // Arrange
        var date = HelperDate.Of(2024, 1, 15);

        // Act
        var result = date.SubtractDays(5);

        // Assert
        Assert.Equal(HelperDate.Of(2024, 1, 10), result);
    }

    [Fact]
    public void IsSaturday_ReturnsTrueForSaturday()
    {
        // 2024-01-06 is a Saturday
        var date = HelperDate.Of(2024, 1, 6);

        Assert.True(date.IsSaturday);
    }

    [Fact]
    public void IsSaturday_ReturnsFalseForNonSaturday()
    {
        // 2024-01-08 is a Monday
        var date = HelperDate.Of(2024, 1, 8);

        Assert.False(date.IsSaturday);
    }

    [Fact]
    public void IsSunday_ReturnsTrueForSunday()
    {
        // 2024-01-07 is a Sunday
        var date = HelperDate.Of(2024, 1, 7);

        Assert.True(date.IsSunday);
    }

    [Fact]
    public void IsSunday_ReturnsFalseForNonSunday()
    {
        // 2024-01-08 is a Monday
        var date = HelperDate.Of(2024, 1, 8);

        Assert.False(date.IsSunday);
    }

    [Fact]
    public void IsWeekend_ReturnsTrueForWeekend()
    {
        var saturday = HelperDate.Of(2024, 1, 6);
        var sunday = HelperDate.Of(2024, 1, 7);

        Assert.True(saturday.IsWeekend);
        Assert.True(sunday.IsWeekend);
    }

    [Fact]
    public void IsWorkDay_ExcludesSunday()
    {
        // 2024-01-07 is a Sunday
        var sunday = HelperDate.Of(2024, 1, 7);

        Assert.False(sunday.IsWorkDay(includeSaturdays: true));
        Assert.False(sunday.IsWorkDay(includeSaturdays: false));
    }

    [Fact]
    public void IsWorkDay_IncludesSaturdayWhenFlagSet()
    {
        // 2024-01-06 is a Saturday (and not a holiday)
        var saturday = HelperDate.Of(2024, 1, 6);

        Assert.True(saturday.IsWorkDay(includeSaturdays: true));
        Assert.False(saturday.IsWorkDay(includeSaturdays: false));
    }

    [Fact]
    public void IsWorkDay_ExcludesHoliday()
    {
        // December 25 is Christmas (national holiday)
        var christmas = HelperDate.Of(2024, 12, 25);

        Assert.False(christmas.IsWorkDay(includeSaturdays: true));
    }

    [Fact]
    public void IsHoliday_NationalHoliday_ReturnsTrue()
    {
        // January 1 is Confraternizacao Universal
        var newYear = HelperDate.Of(2024, 1, 1);
        // September 7 is Independence Day
        var independenceDay = HelperDate.Of(2024, 9, 7);

        Assert.True(newYear.IsHoliday);
        Assert.True(independenceDay.IsHoliday);
    }

    [Fact]
    public void IsHoliday_RegularDay_ReturnsFalse()
    {
        // 2024-01-08 is a Monday, no holiday
        var regularDay = HelperDate.Of(2024, 1, 8);

        Assert.False(regularDay.IsHoliday);
    }

    [Fact]
    public void TotalWorkDays_CountsCorrectly()
    {
        // 2024-01-08 (Monday) to 2024-01-12 (Friday) = 5 work days (no holidays)
        var start = new DateTime(2024, 1, 8);
        var end = new DateTime(2024, 1, 12);

        var result = HelperDate.TotalWorkDays(start, end, includeSaturdays: false);

        Assert.Equal(5, result);
    }

    [Fact]
    public void TotalWorkDays_IncludingSaturdays_CountsCorrectly()
    {
        // 2024-01-08 (Monday) to 2024-01-13 (Saturday) = 6 work days with Saturday included
        var start = new DateTime(2024, 1, 8);
        var end = new DateTime(2024, 1, 13);

        var result = HelperDate.TotalWorkDays(start, end, includeSaturdays: true);

        Assert.Equal(6, result);
    }

    [Fact]
    public void TotalWorkDays_ExcludesSundayAndHoliday()
    {
        // 2024-12-23 (Mon) to 2024-12-29 (Sun)
        // Mon(23), Tue(24), Wed(25=Christmas), Thu(26), Fri(27), Sat(28), Sun(29)
        // Excluding Sat and Sun and Christmas: Mon, Tue, Thu, Fri = 4
        var start = new DateTime(2024, 12, 23);
        var end = new DateTime(2024, 12, 29);

        var result = HelperDate.TotalWorkDays(start, end, includeSaturdays: false);

        Assert.Equal(4, result);
    }

    [Fact]
    public void BreakInMonths_SplitsCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 15);
        var end = new DateTime(2024, 3, 10);

        // Act
        var result = HelperDate.BreakInMonths(start, end);

        // Assert
        Assert.Equal(3, result.Count);

        // First period: Jan 15 - Jan 31
        Assert.Equal(new DateTime(2024, 1, 15), result[0].DataInicial);
        Assert.Equal(new DateTime(2024, 1, 31), result[0].DataFinal);

        // Second period: Feb 1 - Feb 29 (2024 is leap year)
        Assert.Equal(new DateTime(2024, 2, 1), result[1].DataInicial);
        Assert.Equal(new DateTime(2024, 2, 29), result[1].DataFinal);

        // Third period: Mar 1 - Mar 10
        Assert.Equal(new DateTime(2024, 3, 1), result[2].DataInicial);
        Assert.Equal(new DateTime(2024, 3, 10), result[2].DataFinal);
    }

    [Fact]
    public void BreakInMonths_SingleMonth_ReturnsSinglePeriod()
    {
        // Arrange
        var start = new DateTime(2024, 5, 1);
        var end = new DateTime(2024, 5, 31);

        // Act
        var result = HelperDate.BreakInMonths(start, end);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2024, 5, 1), result[0].DataInicial);
        Assert.Equal(new DateTime(2024, 5, 31), result[0].DataFinal);
    }

    [Fact]
    public void CountDays_ReturnsCorrectDifference()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 31);

        var result = HelperDate.CountDays(start, end);

        Assert.Equal(30, result);
    }

    [Fact]
    public void CountMonths_ReturnsCorrectDifference()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 6, 1);

        var result = HelperDate.CountMonths(start, end);

        Assert.Equal(5, result);
    }

    [Fact]
    public void IsBefore_ReturnsCorrectly()
    {
        var earlier = HelperDate.Of(2024, 1, 1);
        var later = HelperDate.Of(2024, 12, 31);

        Assert.True(earlier.IsBefore(later));
        Assert.False(later.IsBefore(earlier));
    }

    [Fact]
    public void IsAfter_ReturnsCorrectly()
    {
        var earlier = HelperDate.Of(2024, 1, 1);
        var later = HelperDate.Of(2024, 12, 31);

        Assert.True(later.IsAfter(earlier));
        Assert.False(earlier.IsAfter(later));
    }

    [Fact]
    public void IsBetween_DateInRange_ReturnsTrue()
    {
        var start = HelperDate.Of(2024, 1, 1);
        var middle = HelperDate.Of(2024, 6, 15);
        var end = HelperDate.Of(2024, 12, 31);

        Assert.True(middle.IsBetween(start, end));
    }

    [Fact]
    public void LastDayOfMonth_ReturnsCorrectDate()
    {
        var date = HelperDate.Of(2024, 2, 10);

        var result = date.LastDayOfMonth();

        Assert.Equal(HelperDate.Of(2024, 2, 29), result); // Leap year
    }

    [Fact]
    public void FirstDayOfMonth_ReturnsCorrectDate()
    {
        var date = HelperDate.Of(2024, 7, 20);

        var result = date.FirstDayOfMonth();

        Assert.Equal(HelperDate.Of(2024, 7, 1), result);
    }

    [Fact]
    public void DaysInMonth_ReturnsCorrectCount()
    {
        var feb2024 = HelperDate.Of(2024, 2, 1);
        var feb2023 = HelperDate.Of(2023, 2, 1);

        Assert.Equal(29, feb2024.DaysInMonth); // Leap year
        Assert.Equal(28, feb2023.DaysInMonth); // Non-leap year
    }

    [Fact]
    public void OperatorEquals_SameDates_ReturnsTrue()
    {
        var a = HelperDate.Of(2024, 5, 1);
        var b = HelperDate.Of(2024, 5, 1);

        Assert.True(a == b);
    }

    [Fact]
    public void OperatorLessThan_EarlierDate_ReturnsTrue()
    {
        var a = HelperDate.Of(2024, 1, 1);
        var b = HelperDate.Of(2024, 12, 31);

        Assert.True(a < b);
        Assert.False(b < a);
    }

    [Fact]
    public void ToString_ReturnsFormattedDate()
    {
        var date = HelperDate.Of(2024, 3, 5);

        Assert.Equal("05/03/2024", date.ToString());
    }
}
