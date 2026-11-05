namespace DottoressaIorio.App.Helpers;

public static class DateFormatting
{
    public static string ToRelativeItalian(DateTime? date)
    {
        if (date == null)
        {
            return string.Empty;
        }

        var today = DateTime.Today;
        var diff = today - date.Value.Date;

        if (diff.TotalDays <= 0)
        {
            return "oggi";
        }

        if (diff.TotalDays == 1)
        {
            return "ieri";
        }

        if (diff.TotalDays < 30)
        {
            return $"{(int)diff.TotalDays} giorni fa";
        }

        if (diff.TotalDays < 365)
        {
            var months = (int)(diff.TotalDays / 30);
            return months == 1
                ? "1 mese fa"
                : $"{months} mesi fa";
        }

        var years = (int)(diff.TotalDays / 365);
        return years == 1
            ? "1 anno fa"
            : $"{years} anni fa";
    }
}
