using DottoressaIorio.Models;

namespace DottoressaIorio.Repositories;

/// <summary>
/// Provides access to the singleton clinic-settings record.
/// </summary>
public interface IClinicSettingsRepository
{
    /// <summary>
    /// Gets the current clinic settings.
    /// </summary>
    Task<ClinicSettings> GetAsync();

    /// <summary>
    /// Saves the supplied clinic settings.
    /// </summary>
    Task SaveAsync(ClinicSettings settings);
}
