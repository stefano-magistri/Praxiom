using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Validation;

namespace DottoressaIorio.Models;

public class ClinicSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string DoctorName { get; set; } = "Dott.ssa Alessandra Iorio";

    public string DoctorTitle { get; set; } = "Medico Chirurgo";

    public string Specialization { get; set; } = "Specialista in Dermatologia e Venereologia";

    public string ClinicName { get; set; } = "Studio Medico";

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = "320.1871136";

    [OptionalEmailAddress(ErrorMessage = "Il formato della email non é valido.")]
    public string Email { get; set; } = "alessandraiorio8@gmail.com";

    public string FooterText { get; set; } = string.Empty;

    public static ClinicSettings CreateDefault()
    {
        return new ClinicSettings();
    }
}
