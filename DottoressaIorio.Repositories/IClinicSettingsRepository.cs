using DottoressaIorio.Models;

namespace DottoressaIorio.Repositories;

public interface IClinicSettingsRepository
{
    Task<ClinicSettings> GetAsync();
    Task SaveAsync(ClinicSettings settings);
}
