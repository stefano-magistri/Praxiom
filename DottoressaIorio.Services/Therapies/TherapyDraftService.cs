using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Therapies;

public static class TherapyDraftService
{
    public static Therapy CreateDuplicate(Therapy source, DateTime today)
    {
        return new Therapy
        {
            PatientId = source.PatientId,
            Description = source.Description,
            CreatedDate = today.Date
        };
    }

    public static string ApplyTemplate(string? existingDescription, string? templateDescription, TherapyTemplateApplyMode mode)
    {
        var normalizedExisting = existingDescription ?? string.Empty;
        var normalizedTemplate = templateDescription ?? string.Empty;

        return mode switch
        {
            TherapyTemplateApplyMode.Replace => normalizedTemplate,
            TherapyTemplateApplyMode.Append when string.IsNullOrWhiteSpace(normalizedExisting) => normalizedTemplate,
            TherapyTemplateApplyMode.Append when string.IsNullOrWhiteSpace(normalizedTemplate) => normalizedExisting,
            TherapyTemplateApplyMode.Append => $"{normalizedExisting}{Environment.NewLine}{Environment.NewLine}{normalizedTemplate}",
            _ => normalizedExisting
        };
    }
}

public enum TherapyTemplateApplyMode
{
    Replace = 0,
    Append = 1
}
