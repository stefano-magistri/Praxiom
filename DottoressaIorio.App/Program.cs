using DottoressaIorio.App.Middleware;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Pdf;
using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var isWindowsService = OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();

if (isWindowsService) builder.Host.UseWindowsService();

var encryptionKey = configuration["EncryptionKey"];
var allowedUsername = configuration["Authentication:AllowedUsername"];

// Read the "UseNegotiate" setting from the configuration file
var useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

// Configure Serilog for logging
builder.AddDottoressaLogging();

if (useNegotiate && string.IsNullOrWhiteSpace(allowedUsername))
    Log.Warning(
        "Authentication:UseNegotiate is enabled but Authentication:AllowedUsername is empty. Access will be denied by SpecificUserOnly.");

if (!useNegotiate && builder.Environment.IsProduction())
    Log.Warning(
        "Authentication:UseNegotiate is disabled in Production. SpecificUserOnly allows access when Negotiate is disabled.");

// Configure authorization policies
builder.Services.AddDottoressaIorioAuthorization(configuration, useNegotiate, allowedUsername);
builder.Services.AddDottoressaIorioAuthentication(configuration, useNegotiate);


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSqlLiteDbEncryption<ApplicationDbContext>(encryptionKey);

builder.Services.AddDottoressaRepositories();

builder.Services.AddPdfServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!isWindowsService) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

if (useNegotiate) app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
