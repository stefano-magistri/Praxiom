using DinkToPdf;
using DinkToPdf.Contracts;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var encryptionKey = configuration["EncryptionKey"];
var allowedUsername = configuration["Authentication:AllowedUsername"];

// Read the "UseNegotiate" setting from the configuration file
bool useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

// Conditionally configure authentication based on the configuration setting
if (useNegotiate)
{
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SpecificUserOnly", policy =>
            policy.RequireAssertion(context =>
                context.User.Identity?.Name != null &&
                context.User.Identity.Name
                    .Equals(allowedUsername, StringComparison.InvariantCultureIgnoreCase)));

        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
else
{
    // If Negotiate is disabled, set up a fallback policy without requiring authentication
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)  // Allow anonymous access
            .Build();
    });
}

// Configure Serilog for logging
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/DottoressaIorio.App.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var encryption = new SqlLiteDbEncryptionService(encryptionKey);
encryption.EncryptDatabaseIfNotExists();
var connectionString = encryption.GetDatabaseConnectionString();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<TherapyTemplateRepository>();
builder.Services.AddScoped<PatientTherapyRepository>();
builder.Services.AddScoped<PatientRepository>();
builder.Services.AddScoped<PdfService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

if (useNegotiate)
{
    app.UseAuthentication();
}
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
