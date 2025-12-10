using Microsoft.AspNetCore.Authentication.Negotiate;

public static class AuthenticationExtensions
{

    public static IServiceCollection AddDottoressaIorioAuthentication(this IServiceCollection services, IConfiguration config,
        bool useNegotiate)
    {

        // Conditionally configure authentication based on the configuration setting
        if (useNegotiate)
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

        return services;
    }
}