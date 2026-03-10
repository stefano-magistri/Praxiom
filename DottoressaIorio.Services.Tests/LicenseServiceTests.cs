using DottoressaIorio.Data;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Licensing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace DottoressaIorio.Services.Tests;

public class LicenseServiceTests
{
    [Fact]
    public async Task TrialIsCreatedAutomaticallyWhenNoLicenseExists()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var service = CreateLicenseService(fixture, timeProvider, LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await service.InitializeAsync();

        Assert.True(service.IsTrial);
        Assert.False(service.IsTrialExpired);
        Assert.Equal(new DateTime(2026, 3, 17, 12, 0, 0), service.TrialExpiresUtc);

        var settings = await fixture.ClinicSettingsRepository.GetAsync();
        Assert.Equal(new DateTime(2026, 3, 10, 12, 0, 0), settings.TrialStartUtc);
    }

    [Fact]
    public async Task TrialExpiresAfter7Days()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var settings = await fixture.ClinicSettingsRepository.GetAsync();
        settings.TrialStartUtc = new DateTime(2026, 3, 1, 12, 0, 0);
        await fixture.ClinicSettingsRepository.SaveAsync(settings);

        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 8, 12, 0, 1, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await service.InitializeAsync();

        Assert.True(service.IsTrialExpired);
    }

    [Fact]
    public async Task TrialStillValidWithin7Days()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var settings = await fixture.ClinicSettingsRepository.GetAsync();
        settings.TrialStartUtc = new DateTime(2026, 3, 1, 12, 0, 0);
        await fixture.ClinicSettingsRepository.SaveAsync(settings);

        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await service.InitializeAsync();

        Assert.True(service.IsTrial);
        Assert.False(service.IsTrialExpired);
    }

    [Fact]
    public async Task RemainingTrialDays_AreCalculatedWithoutGoingNegative()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var settings = await fixture.ClinicSettingsRepository.GetAsync();
        settings.TrialStartUtc = new DateTime(2026, 3, 1, 12, 0, 0);
        await fixture.ClinicSettingsRepository.SaveAsync(settings);

        var activeService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await activeService.InitializeAsync();

        Assert.Equal(2, activeService.RemainingTrialDays);

        var expiredService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await expiredService.InitializeAsync();

        Assert.Equal(0, expiredService.RemainingTrialDays);
    }

    [Fact]
    public async Task StatusTransitions_AreReportedCorrectly()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var licensedService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Valid(new LicensePayload(LocalLicenseDefaults.ProductName, "machine", @"HOST\doctor")));

        await licensedService.InitializeAsync();

        Assert.True(licensedService.IsLicensed);
        Assert.Equal("Licenziato", licensedService.StatusLabel);

        var settings = await fixture.ClinicSettingsRepository.GetAsync();
        settings.TrialStartUtc = new DateTime(2026, 3, 5, 12, 0, 0);
        await fixture.ClinicSettingsRepository.SaveAsync(settings);

        var activeTrialService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await activeTrialService.InitializeAsync();

        Assert.True(activeTrialService.IsTrial);
        Assert.Equal("Versione di prova", activeTrialService.StatusLabel);

        var expiredTrialService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 13, 12, 0, 1, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        await expiredTrialService.InitializeAsync();

        Assert.True(expiredTrialService.IsTrialExpired);
        Assert.Equal("Scaduto", expiredTrialService.StatusLabel);
    }

    [Fact]
    public async Task PatientLimitBlocksCreationWhenAtLeast10DuringTrial()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        for (var i = 0; i < 10; i++)
        {
            await fixture.PatientRepository.AddAsync(new Patient
            {
                FirstName = $"Mario{i}",
                LastName = "Rossi",
                DateOfBirth = new DateTime(1990, 1, 1)
            });
        }

        await fixture.Context.SaveChangesAsync();
        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        var message = await service.GetPatientCreationBlockMessageAsync();

        Assert.Equal(LicenseService.TrialPatientLimitMessage, message);
    }

    [Fact]
    public async Task TherapyTemplateLimitBlocksCreationWhenAtLeast10DuringTrial()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        for (var i = 0; i < 10; i++)
        {
            await fixture.TherapyTemplateRepository.AddAsync(new TherapyTemplate
            {
                Title = $"Template {i}",
                Description = "Descrizione"
            });
        }

        await fixture.Context.SaveChangesAsync();
        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile));

        var message = await service.GetTherapyTemplateCreationBlockMessageAsync();

        Assert.Equal(LicenseService.TrialTherapyTemplateLimitMessage, message);
    }

    [Fact]
    public async Task LimitsMessagingConditions_AreDerivableFromServiceState()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var activeTrialService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile),
            LicenseLaunchMode.TrialActive);

        await activeTrialService.InitializeAsync();

        Assert.Null(await activeTrialService.GetPatientCreationBlockMessageAsync());
        Assert.Null(await activeTrialService.GetTherapyTemplateCreationBlockMessageAsync());

        var expiredTrialService = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Invalid(LicenseValidationFailure.MissingLicenseFile),
            LicenseLaunchMode.TrialExpired);

        Assert.Equal(LicenseService.TrialExpiredCreationMessage, await expiredTrialService.GetPatientCreationBlockMessageAsync());
        Assert.Equal(LicenseService.TrialExpiredCreationMessage, await expiredTrialService.GetTherapyTemplateCreationBlockMessageAsync());
        Assert.Equal(LicenseService.TrialExpiredCreationMessage, await expiredTrialService.GetTherapyCreationBlockMessageAsync());
    }

    [Fact]
    public async Task LaunchModeTrialActive_ForcesTrialWithoutLicenseFile()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Valid(new LicensePayload(LocalLicenseDefaults.ProductName, "machine", @"HOST\doctor")),
            LicenseLaunchMode.TrialActive);

        await service.InitializeAsync();

        Assert.True(service.IsTrial);
        Assert.False(service.IsTrialExpired);
        Assert.False(service.IsLicensed);
    }

    [Fact]
    public async Task LaunchModeTrialExpired_ForcesExpiredTrialWithoutLicenseFile()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = CreateLicenseService(
            fixture,
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)),
            LicenseValidationResult.Valid(new LicensePayload(LocalLicenseDefaults.ProductName, "machine", @"HOST\doctor")),
            LicenseLaunchMode.TrialExpired);

        await service.InitializeAsync();

        Assert.True(service.IsTrial);
        Assert.True(service.IsTrialExpired);
        Assert.False(service.IsLicensed);
    }

    [Fact]
    public async Task DevelopmentWithoutLaunchOverride_BypassesLicenseAndTrial()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var localLicenseService = new Mock<ILocalLicenseService>(MockBehavior.Strict);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:AllowedUsername"] = @"HOST\\doctor"
            })
            .Build();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

        var service = new LicenseService(
            localLicenseService.Object,
            fixture.ClinicSettingsRepository,
            fixture.PatientRepository,
            fixture.TherapyTemplateRepository,
            configuration,
            hostEnvironment.Object,
            new LicenseLaunchOverride(),
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero)));

        await service.InitializeAsync();

        Assert.True(service.IsLicensed);
        Assert.False(service.IsTrial);
        Assert.False(service.IsTrialExpired);
    }

    private static LicenseService CreateLicenseService(
        TestDbFixture fixture,
        TimeProvider timeProvider,
        LicenseValidationResult validationResult,
        LicenseLaunchMode launchMode = LicenseLaunchMode.None)
    {
        var localLicenseService = new Mock<ILocalLicenseService>();
        localLicenseService
            .Setup(x => x.ValidateCurrentLicense(It.IsAny<string>()))
            .Returns(validationResult);

        var settings = new Dictionary<string, string?>
        {
            ["Authentication:AllowedUsername"] = @"HOST\\doctor"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

        return new LicenseService(
            localLicenseService.Object,
            fixture.ClinicSettingsRepository,
            fixture.PatientRepository,
            fixture.TherapyTemplateRepository,
            configuration,
            hostEnvironment.Object,
            new LicenseLaunchOverride { Mode = launchMode },
            timeProvider);
    }

    private sealed class TestDbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDbFixture(SqliteConnection connection, ApplicationDbContext context)
        {
            this.connection = connection;
            Context = context;
            ClinicSettingsRepository = new ClinicSettingsRepository(context);
            PatientRepository = new PatientRepository(context);
            TherapyTemplateRepository = new TherapyTemplateRepository(context);
        }

        public ApplicationDbContext Context { get; }
        public ClinicSettingsRepository ClinicSettingsRepository { get; }
        public PatientRepository PatientRepository { get; }
        public TherapyTemplateRepository TherapyTemplateRepository { get; }

        public static async Task<TestDbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDbFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
