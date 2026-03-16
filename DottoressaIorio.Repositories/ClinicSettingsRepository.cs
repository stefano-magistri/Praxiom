using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
/// Loads and saves the singleton clinic-settings row used by PDF output and trial tracking.
/// </summary>
public sealed class ClinicSettingsRepository(ApplicationDbContext context) : IClinicSettingsRepository
{
    /// <summary>
    /// Gets the singleton clinic settings row, creating it from defaults if it does not exist yet.
    /// </summary>
    public async Task<ClinicSettings> GetAsync()
    {
        var settings = await context.ClinicSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ClinicSettings.SingletonId);

        if (settings is not null)
        {
            return settings;
        }

        var defaults = ClinicSettings.CreateDefault();
        context.ClinicSettings.Add(defaults);
        await context.SaveChangesAsync();
        return defaults;
    }

    /// <summary>
    /// Saves the singleton clinic settings row after normalizing editable text fields.
    /// </summary>
    public async Task SaveAsync(ClinicSettings settings)
    {
        var target = await context.ClinicSettings
            .SingleOrDefaultAsync(x => x.Id == ClinicSettings.SingletonId);

        if (target is null)
        {
            // The settings table is treated as a singleton row, so saves upsert the known identifier instead of creating multiples.
            target = ClinicSettings.CreateDefault();
            context.ClinicSettings.Add(target);
        }

        target.Id = ClinicSettings.SingletonId;
        target.DoctorName = Normalize(settings.DoctorName);
        target.DoctorTitle = Normalize(settings.DoctorTitle);
        target.Specialization = Normalize(settings.Specialization);
        target.ClinicName = Normalize(settings.ClinicName);
        target.Address = Normalize(settings.Address);
        target.Phone = Normalize(settings.Phone);
        target.Email = Normalize(settings.Email);
        target.FooterText = Normalize(settings.FooterText);
        target.TrialStartUtc = settings.TrialStartUtc;

        await context.SaveChangesAsync();
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
