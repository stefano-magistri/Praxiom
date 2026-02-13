using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.App.Repository.Tests.Repositories;

/// <summary>
/// Contains unit tests for the <see cref="PatientTherapyRepository"/> class.
/// </summary>
public class PatientTherapyRepositoryTests : IDisposable, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly List<Therapy> _data = new()
    {
        new Therapy
        {
            TherapyId = 1,
            PatientId = 1,
            Description = "Therapy 1",
            CreatedDate = DateTime.Now,
            Deleted = false,
            Patient = new Patient
            {
                PatientId = 1,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateTime.Parse("1981-02-10"),
                Email = "s@s.com"
            }
        }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PatientTherapyRepositoryTests"/> class.
    /// Sets up an in-memory database for testing purposes.
    /// </summary>
    public PatientTherapyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
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
    /// Tests that the GetAllAsync method returns all therapies for a specific patient.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ShouldReturnTherapiesForPatient()
    {
        // Arrange
        await SeedDataAsync();

        var patientId = 1;
        var expectedTherapies = _data.Where(t => t.PatientId == patientId && !t.Deleted).OrderBy(t => t.CreatedDate)
            .ToList();

        // Act
        var repository = new PatientTherapyRepository(_context);
        var result = await repository.GetAllAsync(patientId);

        // Assert
        Assert.Equal(expectedTherapies.Count, result.Count);
        Assert.Equal(expectedTherapies.Select(t => t.TherapyId), result.Select(t => t.TherapyId));
    }

    /// <summary>
    /// Seeds the test database with initial data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SeedDataAsync()
    {
        _context.Therapies.AddRange(_data);
        await _context.SaveChangesAsync();
    }
}