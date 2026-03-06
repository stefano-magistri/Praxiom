using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace DottoressaIorio.App.Repository.Tests.Repositories;

public sealed class EfUnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    static EfUnitOfWorkTests()
    {
        raw.SetProvider(new SQLite3Provider_e_sqlcipher());
        Batteries_V2.Init();
    }

    public EfUnitOfWorkTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _unitOfWork = new EfUnitOfWork(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_AfterUniqueConstraintFailure_ShouldAllowSubsequentUnrelatedSave()
    {
        var templateRepository = new GenericRepository<TherapyTemplate>(_context);

        await templateRepository.AddAsync(new TherapyTemplate
        {
            Title = "Template duplicate",
            Description = "First"
        });
        await _unitOfWork.SaveChangesAsync();

        await templateRepository.AddAsync(new TherapyTemplate
        {
            Title = "Template duplicate",
            Description = "Second"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => _unitOfWork.SaveChangesAsync());

        var patientRepository = new GenericRepository<Patient>(_context);
        await patientRepository.AddAsync(new Patient
        {
            FirstName = "Mario",
            LastName = "Rossi",
            DateOfBirth = new DateTime(1980, 1, 1),
            Email = "mario@example.com"
        });

        var ex = await Record.ExceptionAsync(() => _unitOfWork.SaveChangesAsync());
        Assert.Null(ex);
    }
}
