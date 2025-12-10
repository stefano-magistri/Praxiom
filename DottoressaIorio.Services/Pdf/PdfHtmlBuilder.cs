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
        .h1 {{ font-size: 20px; font-weight:bold; }}
        p {{ font-size:18px; }}
    </style>
</head>
<body>
    <p class='h1'>{therapy.Patient.Title} {therapy.Patient.FirstName} {therapy.Patient.LastName},</p>
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