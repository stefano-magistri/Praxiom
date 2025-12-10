using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace DottoressaIorio.App.Middleware;

public static class AuthorizationExtensions
{
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
                .RequireAssertion(context => !useNegotiate || context.User.Identity?.IsAuthenticated == true)
                .Build();
        }); 
        return services;
    }
}