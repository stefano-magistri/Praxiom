using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models;
using DottoressaIorio.Models.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DottoressaIorio.Data;

/// <summary>
///     Entity Framework Core database context for the application.
///     Manages entity sets and database access.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationDbContext" /> class.
    /// </summary>
    /// <param name="options">The database context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    ///     Gets or sets the collection of <see cref="Patient" /> entities.
    /// </summary>
    public DbSet<Patient> Patients { get; set; }

    /// <summary>
    ///     Gets or sets the collection of <see cref="Therapy" /> entities.
    /// </summary>
    public DbSet<Therapy> Therapies { get; set; }

    /// <summary>
    ///     Gets or sets the collection of <see cref="TherapyTemplate" /> entities.
    /// </summary>
    public DbSet<TherapyTemplate> TherapyTemplates { get; set; }

    public DbSet<ClinicSettings> ClinicSettings { get; set; }

    public override int SaveChanges()
    {
        NormalizeAndValidateTrackedEntities();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeAndValidateTrackedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeAndValidateTrackedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        NormalizeAndValidateTrackedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply soft-delete behavior globally to keep query semantics consistent.
        modelBuilder.Entity<Patient>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<Therapy>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<TherapyTemplate>().HasQueryFilter(x => !x.Deleted);

        modelBuilder.Entity<TherapyTemplate>()
            .HasIndex(x => x.Title)
            .HasFilter("\"Deleted\" = 0")
            .IsUnique();

        modelBuilder.Entity<ClinicSettings>()
            .HasData(DottoressaIorio.Models.ClinicSettings.CreateDefault());
    }

    private void NormalizeAndValidateTrackedEntities()
    {
        var trackedEntries = ChangeTracker.Entries<IDataHandler>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in trackedEntries)
        {
            if (IsSoftDeleteOnlyUpdate(entry)) continue;

            var entity = entry.Entity;
            NormalizeEntity(entity);
            Validator.ValidateObject(entity, new ValidationContext(entity), true);
        }
    }

    private static bool IsSoftDeleteOnlyUpdate(EntityEntry<IDataHandler> entry)
    {
        if (entry.State != EntityState.Modified || !entry.Entity.Deleted) return false;

        var modifiedPropertyNames = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (modifiedPropertyNames.Count == 0) return false;

        return modifiedPropertyNames.All(name =>
            name is nameof(IDataHandler.Deleted) or nameof(IDataHandler.EditDate));
    }

    private static void NormalizeEntity(IDataHandler entity)
    {
        switch (entity)
        {
            case Patient patient:
                patient.Title = NormalizeOptional(patient.Title);
                patient.FirstName = NormalizeRequired(patient.FirstName);
                patient.LastName = NormalizeRequired(patient.LastName);
                patient.PlaceOfBirth = NormalizeOptional(patient.PlaceOfBirth);
                patient.Gender = NormalizeOptional(patient.Gender);
                patient.PhoneNumber = NormalizePhoneNumber(patient.PhoneNumber);
                patient.Email = NormalizeOptional(patient.Email)?.ToLowerInvariant();
                patient.Notes = NormalizeOptional(patient.Notes);
                break;
            case Therapy therapy:
                therapy.Description = NormalizeRequired(therapy.Description);
                break;
            case TherapyTemplate template:
                template.Title = NormalizeRequired(template.Title);
                template.Description = NormalizeRequired(template.Description);
                break;
        }
    }

    private static string NormalizeRequired(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return value.Trim();
    }

    private static string? NormalizePhoneNumber(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null) return null;

        var hasLeadingPlus = normalized.StartsWith('+');
        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        return hasLeadingPlus ? $"+{digits}" : digits;
    }
}
