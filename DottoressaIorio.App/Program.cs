using DinkToPdf;
using DinkToPdf.Contracts;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;

/// <summary>
/// Entry point for the DottoressaIorio application.
/// </summary>
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var encryptionKey = configuration["EncryptionKey"];
var allowedUsername = configuration["Authentication:AllowedUsername"];

// Read the "UseNegotiate" setting from the configuration file
bool useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

// Configure Serilog for logging
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/DottoressaIorio.App.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Always add the SpecificUserOnly policy
    options.AddPolicy("SpecificUserOnly", policy =>
        policy.RequireAssertion(context =>
        {
            if (!useNegotiate)
            {
                Log.Information("Negotiate authentication is disabled, allowing all access");
                return true; // Allow all access when Negotiate is disabled
            }

            var identityName = context.User.Identity?.Name;
            Log.Information("User Identity Name: {IdentityName}", identityName);
            Log.Information("Allowed Username: {AllowedUsername}", allowedUsername);
            Log.Information("Is Authenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
            
            return context.User.Identity?.Name != null &&
                   context.User.Identity.Name
                       .Equals(allowedUsername, StringComparison.InvariantCultureIgnoreCase);
        }));

    // Set the fallback policy based on authentication configuration
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(context => useNegotiate ? context.User.Identity?.IsAuthenticated == true : true)
        .Build();
});

// Conditionally configure authentication based on the configuration setting
if (useNegotiate)
{
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}

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
