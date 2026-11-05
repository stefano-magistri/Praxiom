using DottoressaIorio.Services.Patients;

namespace DottoressaIorio.Services.Tests;

public class PatientAgeCalculatorTests
{
    [Fact]
    public void CalculateAge_WhenBirthdayAlreadyPassedThisYear_ReturnsExpectedAge()
    {
        var age = PatientAgeCalculator.CalculateAge(new DateTime(1980, 1, 10), new DateTime(2026, 3, 9));

        Assert.Equal(46, age);
    }

    [Fact]
    public void CalculateAge_WhenBirthdayNotYetPassedThisYear_ReturnsExpectedAge()
    {
        var age = PatientAgeCalculator.CalculateAge(new DateTime(1980, 12, 10), new DateTime(2026, 3, 9));

        Assert.Equal(45, age);
    }

    [Fact]
    public void CalculateAge_WhenBirthdayIsToday_ReturnsExpectedAge()
    {
        var age = PatientAgeCalculator.CalculateAge(new DateTime(1980, 3, 9), new DateTime(2026, 3, 9));

        Assert.Equal(46, age);
    }

    [Fact]
    public void CalculateAge_WhenDateOfBirthIsNull_ReturnsNull()
    {
        var age = PatientAgeCalculator.CalculateAge(null, new DateTime(2026, 3, 9));

        Assert.Null(age);
    }
}
