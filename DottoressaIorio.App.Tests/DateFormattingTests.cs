using DottoressaIorio.App.Helpers;

namespace DottoressaIorio.App.Tests;

public class DateFormattingTests
{
    [Fact]
    public void ToRelativeItalian_Today_ReturnsOggi()
    {
        var result = DateFormatting.ToRelativeItalian(DateTime.Today);

        Assert.Equal("oggi", result);
    }

    [Fact]
    public void ToRelativeItalian_Yesterday_ReturnsIeri()
    {
        var result = DateFormatting.ToRelativeItalian(DateTime.Today.AddDays(-1));

        Assert.Equal("ieri", result);
    }

    [Fact]
    public void ToRelativeItalian_MonthsAgo_ReturnsMonths()
    {
        var result = DateFormatting.ToRelativeItalian(DateTime.Today.AddDays(-90));

        Assert.Equal("3 mesi fa", result);
    }

    [Fact]
    public void ToRelativeItalian_YearsAgo_ReturnsYears()
    {
        var result = DateFormatting.ToRelativeItalian(DateTime.Today.AddDays(-730));

        Assert.Equal("2 anni fa", result);
    }
}
