using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace DottoressaIorio.App.Middleware;

public static class AuthorizationExtensions
{
    public static bool IsSpecificUserAllowed(
        bool useNegotiate,
        string? allowedUsername,
        string? identityName,
        bool isAuthenticated)
    {
        if (!useNegotiate)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(identityName) &&
               !string.IsNullOrWhiteSpace(allowedUsername) &&
               identityName.Equals(allowedUsername, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsFallbackAuthorized(bool useNegotiate, bool isAuthenticated)
    {
        return !useNegotiate || isAuthenticated;
    }

    public static IServiceCollection AddDottoressaIorioAuthorization(this IServiceCollection services,
        IConfiguration config,
        bool useNegotiate, string allowedUsername)
    {
        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            // Always add the SpecificUserOnly policy
            options.AddPolicy("SpecificUserOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    if (!useNegotiate)
                    {
                        return true; // Allow all access when Negotiate is disabled
                    }

                    var identityName = context.User.Identity?.Name;
                    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
                    var isAllowedUser = IsSpecificUserAllowed(
                        useNegotiate,
                        allowedUsername,
                        identityName,
                        isAuthenticated);

                    Log.Debug(
                        "SpecificUserOnly evaluated. Authenticated={IsAuthenticated}, AllowedUserMatch={AllowedUserMatch}",
                        isAuthenticated, isAllowedUser);

                    return isAllowedUser;
                }));

            // Set the fallback policy based on authentication configuration
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                    IsFallbackAuthorized(useNegotiate, context.User.Identity?.IsAuthenticated == true))
                .Build();
        });
        return services;
    }
}
