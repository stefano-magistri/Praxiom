using DottoressaIorio.Models;
using DottoressaIorio.Repositories;

namespace DottoressaIorio.Shared.State;

public sealed class ClinicSettingsState(IClinicSettingsRepository repository)
{
    private ClinicSettings? current;

    public ClinicSettings? Current => current is null ? null : Clone(current);

    public event Action? Changed;

    public async Task<ClinicSettings> GetAsync(bool forceRefresh = false)
    {
        if (current is null || forceRefresh)
        {
            current = await repository.GetAsync();
        }

        return Clone(current);
    }

    public void SetCurrent(ClinicSettings settings)
    {
        current = Clone(settings);
        Changed?.Invoke();
    }

    private static ClinicSettings Clone(ClinicSettings settings)
    {
        return new ClinicSettings
        {
            Id = settings.Id,
            DoctorName = settings.DoctorName,
            DoctorTitle = settings.DoctorTitle,
            Specialization = settings.Specialization,
            ClinicName = settings.ClinicName,
            Address = settings.Address,
            Phone = settings.Phone,
            Email = settings.Email,
            FooterText = settings.FooterText,
            TrialStartUtc = settings.TrialStartUtc
        };
    }
}
