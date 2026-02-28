using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.App.Tests;

public class AuthenticationExtensionsTests
{
    [Fact]
    public void AddDottoressaIorioAuthentication_WhenUseNegotiateTrue_SetsNegotiateDefaultScheme()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddDottoressaIorioAuthentication(config, useNegotiate: true);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>()
            .Value;

        Assert.Equal(NegotiateDefaults.AuthenticationScheme, options.DefaultScheme);
    }

    [Fact]
    public void AddDottoressaIorioAuthentication_WhenUseNegotiateFalse_DoesNotSetDefaultScheme()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddDottoressaIorioAuthentication(config, useNegotiate: false);
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetService<IAuthenticationSchemeProvider>();

        Assert.Null(schemeProvider);
    }
}
