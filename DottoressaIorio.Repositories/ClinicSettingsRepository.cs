using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

public sealed class ClinicSettingsRepository(ApplicationDbContext context) : IClinicSettingsRepository
{
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

    public async Task SaveAsync(ClinicSettings settings)
    {
        var target = await context.ClinicSettings
            .SingleOrDefaultAsync(x => x.Id == ClinicSettings.SingletonId);

        if (target is null)
        {
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
