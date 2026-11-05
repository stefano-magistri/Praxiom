using DottoressaIorio.Services.Backup;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

namespace DottoressaIorio.App.Middleware;

internal static class ProgramWebApplicationExtensions
{
    public static void RegisterDottoressaLifetimeEvents(
        this WebApplication app,
        IOnlineBackupService onlineBackupService,
        OnlineBackupOptions onlineBackupOptions)
    {
        app.Lifetime.ApplicationStarted.Register(() => { Log.Information("Application started."); });

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            Log.Information("Application stopping.");
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                // Shutdown backup is best-effort only so service stop does not hang indefinitely.
                _ = onlineBackupService.TriggerBackupAsync(
                        "application-stopping",
                        onlineBackupOptions.ShutdownMinimumAge,
                        cts.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Application stopping backup timed out.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Application stopping online backup failed.");
            }
        });

        app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
    }

    public static void ConfigureDottoressaPipeline(
        this WebApplication app,
        bool isWindowsService,
        bool useNegotiate)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (app.Environment.IsAzureEnvironment())
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        if (!isWindowsService) app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAntiforgery();

        // When Negotiate is disabled, the current authorization rules intentionally run without authentication middleware.
        if (useNegotiate) app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapDottoressaEndpoints(this WebApplication app)
    {
        app.MapDottoressaPatientExportEndpoints();
        app.MapDottoressaPdfEndpoints();

        app.MapRazorPages();
        app.MapRazorComponents<DottoressaIorio.App.App>()
            .AddInteractiveServerRenderMode();
    }
}
