using DottoressaIorio.Services.Backup;

namespace DottoressaIorio.Services.Tests;

public class OnlineBackupOptionsTests
{
    [Fact]
    public void DefaultRetentionCount_IsThirty()
    {
        var options = new OnlineBackupOptions();

        Assert.Equal(30, options.RetentionCount);
    }
}
