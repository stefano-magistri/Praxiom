using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Pdf;

public class PdfHtmlBuilder
{
    public string BuildHtml(Therapy therapy)
    {
        return $@"
<html>
<head>
    <style>
        body {{ font-family: Calibri, sans-serif; }}
        .h2 {{ font-size: 16px; font-weight: bold; margin: 0 0 20px 0; }}
        p {{ font-size: 14px; margin: 0; }}
    </style>
</head>
<body>
    <p class='h2'>{therapy.Patient.Title} {therapy.Patient.FirstName} {therapy.Patient.LastName},</p>
    {therapy.GetHtmlDescription()}
</body>
</html>";
    }

    public string BuildHeaderHtml(string template, Therapy therapy)
    {
        return template.Replace("{{therapyDate}}", therapy.CreatedDate.ToString("dd/MM/yyyy"));
    }

    public string BuildFooterHtml(string template, Therapy therapy)
    {
        return template
            .Replace("{{therapyDate}}", therapy.CreatedDate.ToString("dd/MM/yyyy"))
            .Replace("{{patient}}", $"{therapy.Patient.Title} {therapy.Patient.FirstName} {therapy.Patient.LastName}");
    }
}
