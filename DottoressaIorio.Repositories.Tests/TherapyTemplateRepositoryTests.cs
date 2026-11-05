using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.App.Repository.Tests.Repositories;

/// <summary>
///     Contains unit tests for the <see cref="TherapyTemplateRepository" /> class.
/// </summary>
public class TherapyTemplateRepositoryTests : IDisposable, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;

    private readonly List<TherapyTemplate> _data = new()
    {
        new TherapyTemplate
        {
            Id = 1,
            Title = "Therapy Template 1",
            Description = "Description 1",
            CreatedDate = DateTime.Now,
            Deleted = false
        },
        new TherapyTemplate
        {
            Id = 2,
            Title = "Therapy Template 2",
            Description = "Description 2",
            CreatedDate = DateTime.Now.AddMonths(-1),
            Deleted = false
        },
        new TherapyTemplate
        {
            Id = 3,
            Title = "Deleted Template",
            Description = "Deleted Description",
            CreatedDate = DateTime.Now.AddMonths(-2),
            Deleted = true
        }
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="TherapyTemplateRepositoryTests" /> class.
    ///     Sets up an in-memory database for testing purposes.
    /// </summary>
    public TherapyTemplateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    /// <summary>
    ///     Asynchronously disposes of the test resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    ///     Disposes of the test resources.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    ///     Tests that the GetAllOrderedAsync method returns all non-deleted therapy templates ordered by title.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllOrderedAsync_ShouldReturnAllTherapyTemplatesOrdered()
    {
        // Arrange
        await SeedDataAsync();

        var expectedTemplates = _data.Where(t => !t.Deleted).OrderBy(t => t.Title).ToList();

        // Act
        var repository = new TherapyTemplateRepository(_context);
        var result = await repository.GetAllOrderedAsync();

        // Assert
        Assert.Equal(expectedTemplates.Count, result.Count);
        Assert.Equal(expectedTemplates.Select(t => t.Id), result.Select(t => t.Id));
    }

    /// <summary>
    ///     Tests that the TitleExistsAsync method correctly identifies whether a therapy template with a given title exists.
    /// </summary>
    /// <param name="title">The title to check for existence.</param>
    /// <param name="expectedResult">The expected result of the existence check.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData("Therapy Template 1", true)]
    [InlineData("  therapy template 1 ", true)]
    [InlineData("Therapy Template 2", true)]
    [InlineData("Deleted Template", false)]
    [InlineData("NonExistent Template", false)]
    public async Task TitleExistsAsync_ShouldReturnExpectedResult(string title, bool expectedResult)
    {
        // Arrange
        await SeedDataAsync();

        // Act
        var repository = new TherapyTemplateRepository(_context);
        var result = await repository.TitleExistsAsync(title);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    /// <summary>
    ///     Seeds the test database with initial data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SeedDataAsync()
    {
        _context.TherapyTemplates.AddRange(_data);
        await _context.SaveChangesAsync();
    }
}