namespace DottoressaIorio.Services.Patients;

public static class PatientAgeCalculator
{
    public static int? CalculateAge(DateTime? dateOfBirth, DateTime? today = null)
    {
        if (!dateOfBirth.HasValue)
        {
            return null;
        }

        var referenceDate = (today ?? DateTime.Today).Date;
        var birthDate = dateOfBirth.Value.Date;
        var age = referenceDate.Year - birthDate.Year;

        if (birthDate > referenceDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
