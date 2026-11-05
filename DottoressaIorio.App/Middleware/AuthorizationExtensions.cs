using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace DottoressaIorio.App.Middleware;

/// <summary>
/// Registers and evaluates the authorization rules used by the application.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Evaluates the <c>SpecificUserOnly</c> rule for the current identity.
    /// </summary>
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

    /// <summary>
    /// Evaluates the fallback authorization rule applied to routes without an explicit policy.
    /// </summary>
    public static bool IsFallbackAuthorized(bool useNegotiate, bool isAuthenticated)
    {
        return !useNegotiate || isAuthenticated;
    }

    /// <summary>
    /// Registers the application's authorization policies without changing the existing auth semantics.
    /// </summary>
    public static IServiceCollection AddDottoressaIorioAuthorization(this IServiceCollection services,
        IConfiguration config,
        bool useNegotiate, string allowedUsername)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("SpecificUserOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    // When Negotiate is disabled, the current implementation intentionally leaves the app open.
                    if (!useNegotiate)
                    {
                        return true;
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

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                    IsFallbackAuthorized(useNegotiate, context.User.Identity?.IsAuthenticated == true))
                .Build();
        });
        return services;
    }
}
