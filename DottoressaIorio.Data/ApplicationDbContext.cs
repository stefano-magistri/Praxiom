using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Data;

/// <summary>
/// Entity Framework Core database context for the application.
/// Manages entity sets and database access.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    /// <summary>
    /// Gets or sets the collection of <see cref="Patient"/> entities.
    /// </summary>
    public DbSet<Patient> Patients { get; set; }
    /// <summary>
    /// Gets or sets the collection of <see cref="Therapy"/> entities.
    /// </summary>
    public DbSet<Therapy> Therapies { get; set; }
    /// <summary>
    /// Gets or sets the collection of <see cref="TherapyTemplate"/> entities.
    /// </summary>
    public DbSet<TherapyTemplate> TherapyTemplates { get; set; }
}