using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DottoressaIorio.App.Repository.Tests.Repositories;

/// <summary>
/// Contains unit tests for the GenericRepository class, testing basic CRUD operations.
/// </summary>
public class GenericRepositoryTests : IDisposable, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GenericRepository<Patient> _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the GenericRepositoryTests class.
    /// Sets up an in-memory database for testing.
    /// </summary>
    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repository = new GenericRepository<Patient>(_context);
        _unitOfWork = new EfUnitOfWork(_context);
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
    /// Tests that the GetByIdAsync method correctly retrieves an entity by its ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntityById()
    {
        // Arrange
        var entityToAdd = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-30),
            Email = "example@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        await _repository.AddAsync(entityToAdd);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entityToAdd.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityToAdd.PatientId, result.PatientId);
    }

    /// <summary>
    /// Tests that the GetByIdAsync method returns null when the entity does not exist.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenEntityDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the GetAllAsync method returns all entities in the database.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var entitiesToAdd = new List<Patient>
        {
            new()
            {
                DateOfBirth = DateTime.Now.AddYears(-30),
                Email = "example@example.com",
                FirstName = "John",
                LastName = "Doe"
            },
            new()
            {
                PatientId = 2,
                DateOfBirth = DateTime.Now.AddYears(-25),
                Email = "jane@example.com",
                FirstName = "Jane",
                LastName = "Smith"
            },
            new()
            {
                PatientId = 3,
                DateOfBirth = DateTime.Now.AddYears(-55),
                Email = "jane.doe@example.com",
                FirstName = "Jane",
                LastName = "Doe"
            }
        };
        foreach (var entity in entitiesToAdd) await _repository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(entitiesToAdd.Count, result.Count);
        Assert.Equal(entitiesToAdd.Select(e => e.PatientId), result.Select(e => e.PatientId));
    }

    /// <summary>
    /// Tests that the UpdateAsync method correctly updates an existing entity.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var entityToAdd = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-30),
            Email = "example@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        await _repository.AddAsync(entityToAdd);
        await _unitOfWork.SaveChangesAsync();
        entityToAdd.FirstName = "Updated";

        // Act
        await _repository.UpdateAsync(entityToAdd);
        await _unitOfWork.SaveChangesAsync();
        var result = await _repository.GetByIdAsync(entityToAdd.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
    }

    /// <summary>
    /// Tests that the DeleteAsync method correctly performs a soft delete on an entity.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity()
    {
        // Arrange
        var entityToAdd = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-30),
            Email = "example@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        await _repository.AddAsync(entityToAdd);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entityToAdd);
        await _unitOfWork.SaveChangesAsync();
        var result = await _repository.GetByIdAsync(entityToAdd.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeSoftDeletedEntities()
    {
        var active = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-40),
            Email = "active@example.com",
            FirstName = "Active",
            LastName = "Patient"
        };
        var deleted = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-50),
            Email = "deleted@example.com",
            FirstName = "Deleted",
            LastName = "Patient",
            Deleted = true
        };

        await _repository.AddAsync(active);
        await _repository.AddAsync(deleted);
        await _unitOfWork.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Active", result[0].FirstName);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNormalizePatientData()
    {
        var patient = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-40),
            Email = "  USER@EXAMPLE.COM  ",
            FirstName = "  Mario ",
            LastName = " Rossi  ",
            PhoneNumber = "   ",
            PlaceOfBirth = " Roma "
        };

        await _repository.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(patient.PatientId);
        Assert.NotNull(result);
        Assert.Equal("Mario", result.FirstName);
        Assert.Equal("Rossi", result.LastName);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("Roma", result.PlaceOfBirth);
        Assert.Null(result.PhoneNumber);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowValidationException_ForInvalidEmail()
    {
        var patient = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-40),
            Email = "not-an-email",
            FirstName = "Mario",
            LastName = "Rossi"
        };

        await _repository.AddAsync(patient);

        await Assert.ThrowsAsync<ValidationException>(() => _unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAllowBlankEmail()
    {
        var patient = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-40),
            Email = "",
            FirstName = "Mario",
            LastName = "Rossi"
        };

        await _repository.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(patient.PatientId);
        Assert.NotNull(result);
        Assert.Null(result.Email);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNormalizePhoneNumberToDigits()
    {
        var patient = new Patient
        {
            DateOfBirth = DateTime.Now.AddYears(-40),
            Email = "mario@example.com",
            FirstName = "Mario",
            LastName = "Rossi",
            PhoneNumber = " +39 (333) 12-34 "
        };

        await _repository.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(patient.PatientId);
        Assert.NotNull(result);
        Assert.Equal("+393331234", result.PhoneNumber);
    }

}
