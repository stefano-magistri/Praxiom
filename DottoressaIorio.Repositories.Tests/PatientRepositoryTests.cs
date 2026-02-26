using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DottoressaIorio.App.Repository.Tests.Repositories;

/// <summary>
/// Contains unit tests for the PatientRepository class, testing patient-specific repository operations.
/// </summary>
public class PatientRepositoryTests : IDisposable, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly List<Patient> data = new()
    {
        new Patient
        {
            PatientId = 1, FirstName = "John", LastName = "Doe", CreatedDate = DateTime.Now,
            DateOfBirth = DateTime.ParseExact("10/02/1981", "dd/MM/yyyy", CultureInfo.InvariantCulture), Email = "s@s.com"
        },
        new Patient
        {
            PatientId = 2, FirstName = "Jane", LastName = "Smith", CreatedDate = DateTime.Now,
            DateOfBirth = DateTime.ParseExact("01/12/2002", "dd/MM/yyyy", CultureInfo.InvariantCulture), Email = "s@s1.com"
        },
        new Patient
        {
            PatientId = 3,
            FirstName = "Deleted",
            LastName = "Patient",
            Deleted = true,
            CreatedDate = DateTime.Now.AddMonths(-1),
            DateOfBirth = DateTime.ParseExact("18/12/1990", "dd/MM/yyyy", CultureInfo.InvariantCulture),
            EditDate = DateTime.Now,
            Email = "s@s2.com"
        }
    };

    /// <summary>
    /// Initializes a new instance of the PatientRepositoryTests class.
    /// Sets up an in-memory database for testing.
    /// </summary>
    public PatientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use a unique database name for each test
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated(); // Ensure database is created
    }

    /// <summary>
    /// Asynchronously disposes of the test resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Tests that the SearchPatientsAsync method returns the expected number of results for various search terms.
    /// </summary>
    /// <param name="searchTerm">The term to search for.</param>
    /// <param name="expectedCount">The expected number of results.</param>
    [Theory]
    [InlineData("John", 1)]
    [InlineData(null, 2)]
    [InlineData("NonExistent", 0)]
    [InlineData("Deleted", 0)]
    [InlineData("jane", 1)]
    [InlineData("jane smith", 1)]
    [InlineData("smith jane", 1)]
    public async Task SearchPatientsAsync_ShouldReturnExpectedResults(string searchTerm, int expectedCount)
    {
        var result = await SearchPatientsAsync(searchTerm);
        Assert.Equal(expectedCount, result.Count);
    }

    /// <summary>
    /// Tests that the GetAllOrderedAsync method returns all non-deleted patients ordered by last name and first name.
    /// </summary>
    [Fact]
    public async Task GetAllOrderedAsync_ShouldReturnAllPatientsOrdered()
    {
        var result = await GetAllOrderedAsync();
        Assert.Equal(data.Count(p => !p.Deleted), result.Count); // Excluding deleted patients
        // Assert that patients are ordered by last name then first name
        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                string.Compare(result[i - 1].LastName, result[i].LastName, StringComparison.OrdinalIgnoreCase) <= 0);
            if (string.Equals(result[i - 1].LastName, result[i].LastName, StringComparison.OrdinalIgnoreCase))
                Assert.True(string.Compare(result[i - 1].FirstName, result[i].FirstName,
                    StringComparison.OrdinalIgnoreCase) <= 0);
        }
    }

    /// <summary>
    /// Tests that the PatientExistsAsync method correctly identifies whether a patient exists based on their details.
    /// </summary>
    /// <param name="firstName">The first name to check.</param>
    /// <param name="lastName">The last name to check.</param>
    /// <param name="dob">The date of birth to check.</param>
    /// <param name="expectedResult">The expected result of the existence check.</param>
    [Theory]
    [InlineData("John", "Doe", "10/02/1981", true)]
    [InlineData("  john ", " doe  ", "10/02/1981", true)]
    [InlineData("John", "Doe", "10/02/1982", false)] // Wrong date of birth
    [InlineData("NonExistent", "Person", "01/01/2000", false)]
    [InlineData("Deleted", "Patient", "18/12/1990", false)]
    [InlineData("Jane", "Smith", "01/12/2002", true)]
    public async Task PatientExistsAsync_ShouldReturnExpectedResult(string firstName, string lastName, string dob,
        bool expectedResult)
    {
        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = DateTime.ParseExact(dob, "dd/MM/yyyy", CultureInfo.InvariantCulture)
        };

        var result = await PatientExistsAsync(patient);

        Assert.Equal(expectedResult, result);
    }

    /// <summary>
    /// Helper method to test patient search functionality.
    /// </summary>
    /// <param name="searchTerm">The term to search for.</param>
    /// <returns>A list of patients matching the search criteria.</returns>
    private async Task<IList<Patient>> SearchPatientsAsync(string searchTerm = null)
    {
        _context.Patients.AddRange(data);
        await _context.SaveChangesAsync();

        var repository = new PatientRepository(_context);
        var result = await repository.SearchPatientsAsync(searchTerm);

        return result;
    }

    /// <summary>
    /// Helper method to test retrieving all ordered patients.
    /// </summary>
    /// <returns>A list of all non-deleted patients ordered by last name and first name.</returns>
    private async Task<IList<Patient>> GetAllOrderedAsync()
    {
        _context.Patients.AddRange(data);
        await _context.SaveChangesAsync();

        var repository = new PatientRepository(_context);
        var result = await repository.GetAllOrderedAsync();

        return result;
    }

    /// <summary>
    /// Helper method to test patient existence check.
    /// </summary>
    /// <param name="patient">The patient to check for existence.</param>
    /// <returns>True if the patient exists, false otherwise.</returns>
    private async Task<bool> PatientExistsAsync(Patient patient)
    {
        _context.Patients.AddRange(data);
        await _context.SaveChangesAsync();

        var repository = new PatientRepository(_context);
        var result = await repository.PatientExistsAsync(patient);

        return result;
    }
}
