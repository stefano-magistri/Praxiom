using DottoressaIorio.Models;
using DottoressaIorio.Services.Therapies;

namespace DottoressaIorio.Services.Tests;

public class TherapyDraftServiceTests
{
    [Fact]
    public void CreateDuplicate_ShouldCopyDescriptionAndResetDateToToday()
    {
        var source = new Therapy
        {
            PatientId = 42,
            Description = "Follow-up therapy",
            CreatedDate = new DateTime(2026, 3, 1, 9, 30, 0)
        };
        var today = new DateTime(2026, 3, 9, 14, 0, 0);

        var duplicate = TherapyDraftService.CreateDuplicate(source, today);

        Assert.NotSame(source, duplicate);
        Assert.Equal(source.PatientId, duplicate.PatientId);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(today.Date, duplicate.CreatedDate);
        Assert.Equal(0, duplicate.TherapyId);
    }

    [Fact]
    public void ApplyTemplate_ShouldKeepTemplateHtmlStableWhenReplacingPatientDescription()
    {
        const string templateDescription = "<p><strong>Mattina</strong></p><ul><li>Acqua</li></ul>";

        var result = TherapyDraftService.ApplyTemplate("Legacy text", templateDescription, TherapyTemplateApplyMode.Replace);

        Assert.Equal(templateDescription, result);
        Assert.Equal(templateDescription, RichTextFormatting.NormalizeStoredValue(result));
    }

    [Fact]
    public void ApplyTemplate_ShouldAppendTemplateHtmlWithoutDoubleEncoding()
    {
        const string existingDescription = "<p>Prima terapia</p>";
        const string templateDescription = "<p>test0&amp;amp;nbsp;test0</p>";

        var result = TherapyDraftService.ApplyTemplate(existingDescription, templateDescription, TherapyTemplateApplyMode.Append);
        var normalized = RichTextFormatting.NormalizeStoredValue(result);

        Assert.Equal("<p>Prima terapia</p><p>test0 test0</p>", normalized);
        Assert.Equal("Prima terapia\ntest0 test0", RichTextFormatting.ToPlainText(normalized));
    }

    [Fact]
    public void ApplyTemplate_ShouldConvertLegacyPlainTextWhenAppendingHtmlTemplate()
    {
        const string existingDescription = "Prima terapia";
        const string templateDescription = "<p><strong>Mattina</strong></p>";

        var result = TherapyDraftService.ApplyTemplate(existingDescription, templateDescription, TherapyTemplateApplyMode.Append);
        var normalized = RichTextFormatting.NormalizeStoredValue(result);

        Assert.Equal("<p>Prima terapia</p><p><strong>Mattina</strong></p>", normalized);
        Assert.Equal("Prima terapia\nMattina", RichTextFormatting.ToPlainText(normalized));
    }
}
