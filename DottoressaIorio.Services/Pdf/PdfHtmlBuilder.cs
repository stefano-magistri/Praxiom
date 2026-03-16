using System.Net;
using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Pdf;

/// <summary>
/// Builds the HTML fragments used by therapy PDF rendering.
/// </summary>
public class PdfHtmlBuilder
{
    /// <summary>
    /// Builds the main therapy document HTML.
    /// </summary>
    public string BuildHtml(Therapy therapy)
    {
        var patientDisplayName = Encode(PatientNameFormatter.FormatDisplayName(therapy.Patient));

        return $@"
<html>
<head>
    <style>
        body {{ font-family: Calibri, sans-serif; }}
        .h2 {{ font-size: 16px; font-weight: bold; margin: 0 0 20px 0; }}
        p, ul, ol {{ font-size: 14px; margin: 0 0 8px 0; }}
        ul, ol {{ padding-left: 20px; }}
        li {{ margin: 0 0 4px 0; }}
    </style>
</head>
<body>
    <p class='h2'>{patientDisplayName},</p>
    {therapy.GetHtmlDescription()}
</body>
</html>";
    }

    /// <summary>
    /// Applies clinic and therapy placeholders to the header template.
    /// </summary>
    public string BuildHeaderHtml(string template, Therapy therapy, ClinicSettings settings)
    {
        return template
            .Replace("{{doctorName}}", Encode(settings.DoctorName))
            .Replace("{{doctorTitleLine}}", BuildLine(settings.DoctorTitle))
            .Replace("{{specializationLine}}", BuildLine(settings.Specialization))
            .Replace("{{clinicNameLine}}", BuildLine(settings.ClinicName))
            .Replace("{{addressLine}}", BuildLine(settings.Address))
            .Replace("{{phoneLine}}", BuildPhoneLine(settings.Phone))
            .Replace("{{emailLine}}", BuildEmailLine(settings.Email))
            .Replace("{{therapyDate}}", therapy.CreatedDate.ToString("dd/MM/yyyy"));
    }

    /// <summary>
    /// Applies clinic, patient, and trial placeholders to the footer template.
    /// </summary>
    public string BuildFooterHtml(string template, Therapy therapy, ClinicSettings settings, bool isTrial = false)
    {
        return template
            .Replace("{{footerText}}", Encode(settings.FooterText))
            .Replace("{{trialLabel}}", isTrial ? BuildTrialLabel() : string.Empty)
            .Replace("{{therapyDate}}", therapy.CreatedDate.ToString("dd/MM/yyyy"))
            .Replace("{{patient}}", Encode(PatientNameFormatter.FormatDisplayName(therapy.Patient)));
    }

    private static string BuildLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"<p style=\"margin:0; font-size: 15px;\">{Encode(value)}</p>";
    }

    private static string BuildPhoneLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"<p style=\"margin:0; font-size: 15px;\">Tel: {Encode(value)}</p>";
    }

    private static string BuildEmailLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"<p style=\"margin:0; font-size: 15px;\"><i>{Encode(value)}</i></p>";
    }

    private static string BuildTrialLabel()
    {
        return "<div style=\"font-size:9px; font-weight:bold; text-align:right;\">VERSIONE DI PROVA</div>";
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
