using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Validation;

namespace DottoressaIorio.Models;

/// <summary>
/// Stores the singleton clinic settings used by PDF output and trial tracking.
/// </summary>
public class ClinicSettings
{
    /// <summary>
    /// Gets the fixed identifier of the singleton clinic-settings row.
    /// </summary>
    public const int SingletonId = 1;

    /// <summary>
    /// Gets or sets the singleton row identifier.
    /// </summary>
    public int Id { get; set; } = SingletonId;

    /// <summary>
    /// Gets or sets the doctor name shown in generated PDFs.
    /// </summary>
    public string DoctorName { get; set; } = "Dott.ssa Alessandra Iorio";

    /// <summary>
    /// Gets or sets the doctor title shown in generated PDFs.
    /// </summary>
    public string DoctorTitle { get; set; } = "Medico Chirurgo";

    /// <summary>
    /// Gets or sets the specialization line shown in generated PDFs.
    /// </summary>
    public string Specialization { get; set; } = "Specialista in Dermatologia e Venereologia";

    /// <summary>
    /// Gets or sets the clinic name shown in generated PDFs.
    /// </summary>
    public string ClinicName { get; set; } = "Studio Medico";

    /// <summary>
    /// Gets or sets the address shown in generated PDFs.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number shown in generated PDFs.
    /// </summary>
    public string Phone { get; set; } = "320.1871136";

    /// <summary>
    /// Gets or sets the email address shown in generated PDFs.
    /// </summary>
    [OptionalEmailAddress(ErrorMessage = "Il formato della email non é valido.")]
    public string Email { get; set; } = "alessandraiorio8@gmail.com";

    /// <summary>
    /// Gets or sets the footer text shown in generated PDFs.
    /// </summary>
    public string FooterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp used to start the trial period when no valid license is present.
    /// </summary>
    public DateTime? TrialStartUtc { get; set; }

    /// <summary>
    /// Creates the default singleton clinic-settings instance.
    /// </summary>
    public static ClinicSettings CreateDefault()
    {
        // The defaults are also seeded through EF migrations, so the app can rely on a single well-known row.
        return new ClinicSettings();
    }
}
