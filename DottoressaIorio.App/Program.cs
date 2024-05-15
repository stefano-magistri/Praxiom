using DinkToPdf;
using DinkToPdf.Contracts;
using DottoressaIorio.App.Data;
using DottoressaIorio.App.Services;
using DottoressaIorio.App.Services.Repositories;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var encryptionKey = configuration["EncryptionKey"];
var allowedUsername = configuration["AllowedUsername"];

// Add services to the container.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SpecificUserOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.Name != null &&
            context.User.Identity.Name
                .Equals(allowedUsername,
                    StringComparison.InvariantCultureIgnoreCase))); // Replace with your specific username

    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

// Configure Serilog for logging to a file
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/DottoressaIorio.App.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var encryption = new SqlLiteDbEncryptionService(encryptionKey);

// Encrypt existing database or create new encrypted database if not exists
encryption.EncryptDatabaseIfNotExists();
var connectionString = encryption.GetDatabaseConnectionString();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register DinkToPdf converter
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Register PdfService
builder.Services.AddScoped<TherapyTemplateRepository>();
builder.Services.AddScoped<PatientTherapyRepository>();
builder.Services.AddScoped<PatientRepository>();
builder.Services.AddScoped<PdfService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure the database is created and apply pending migrations 
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

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();