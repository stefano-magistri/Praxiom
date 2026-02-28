using System.Globalization;
using System.Reflection;

namespace DottoressaIorio.App.Tests;

public class PatientPageTitleFormattingTests
{
    [Fact]
    public void FormatFullNameHelpers_ArePresentInPatientPages()
    {
        var methods = GetPatientFormatMethods();

        Assert.True(methods.Count >= 5);
    }

    [Theory]
    [InlineData("mARIO", "rOSSI", "Mario Rossi")]
    [InlineData("MARIO", "ROSSI", "Mario Rossi")]
    [InlineData("mario", "rossi", "Mario Rossi")]
    [InlineData("Mario", "Rossi", "Mario Rossi")]
    [InlineData(null, null, "")]
    [InlineData("", "", "")]
    public void FormatFullNameHelpers_UseExpectedDisplayNormalization(string? firstName, string? lastName, string expected)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var itCulture = CultureInfo.GetCultureInfo("it-IT");
            CultureInfo.CurrentCulture = itCulture;
            CultureInfo.CurrentUICulture = itCulture;

            foreach (var method in GetPatientFormatMethods())
            {
                var result = method.Invoke(null, [firstName, lastName]);

                Assert.Equal(expected, result);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static List<MethodInfo> GetPatientFormatMethods()
    {
        var appAssembly = typeof(ValidationExceptionMapper).Assembly;
        return appAssembly
            .GetTypes()
            .Where(t => t.FullName?.StartsWith("DottoressaIorio.App.Pages.Patients", StringComparison.Ordinal) == true)
            .Select(t => t.GetMethod("FormatFullName", BindingFlags.NonPublic | BindingFlags.Static))
            .Where(m => m != null)
            .Cast<MethodInfo>()
            .ToList();
    }
}
