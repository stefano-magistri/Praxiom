using DottoressaIorio.Models;
using DottoressaIorio.Services.Therapies;

namespace DottoressaIorio.Services.Tests;

public class TherapyDraftServiceTests
{
    [Fact]
    public void CreateDuplicate_ShouldCopyDescriptionAndResetDateToToday()
    {
        var source = new Therapy
        {
            PatientId = 42,
            Description = "Follow-up therapy",
            CreatedDate = new DateTime(2026, 3, 1, 9, 30, 0)
        };
        var today = new DateTime(2026, 3, 9, 14, 0, 0);

        var duplicate = TherapyDraftService.CreateDuplicate(source, today);

        Assert.NotSame(source, duplicate);
        Assert.Equal(source.PatientId, duplicate.PatientId);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(today.Date, duplicate.CreatedDate);
        Assert.Equal(0, duplicate.TherapyId);
    }
}
