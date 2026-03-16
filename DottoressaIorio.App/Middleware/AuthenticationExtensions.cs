using Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// Registers the application's authentication behavior.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds Negotiate authentication only when it is explicitly enabled in configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="useNegotiate">
    /// <see langword="true" /> to register Windows Negotiate authentication; otherwise authentication middleware is not added.
    /// </param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDottoressaIorioAuthentication(this IServiceCollection services,
        IConfiguration config,
        bool useNegotiate)
    {
        if (useNegotiate)
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

        return services;
    }
}
