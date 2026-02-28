using DottoressaIorio.App.Middleware;

namespace DottoressaIorio.App.Tests;

public class AuthorizationExtensionsTests
{
    [Fact]
    public void IsSpecificUserAllowed_WhenNegotiateDisabled_ReturnsTrue()
    {
        var result = AuthorizationExtensions.IsSpecificUserAllowed(
            useNegotiate: false,
            allowedUsername: "DOMAIN\\user",
            identityName: null,
            isAuthenticated: false);

        Assert.True(result);
    }

    [Fact]
    public void IsSpecificUserAllowed_WhenNegotiateEnabledAndUserMatches_IgnoresCase()
    {
        var result = AuthorizationExtensions.IsSpecificUserAllowed(
            useNegotiate: true,
            allowedUsername: "DOMAIN\\User",
            identityName: "domain\\user",
            isAuthenticated: true);

        Assert.True(result);
    }

    [Fact]
    public void IsSpecificUserAllowed_WhenNegotiateEnabledAndUserDoesNotMatch_ReturnsFalse()
    {
        var result = AuthorizationExtensions.IsSpecificUserAllowed(
            useNegotiate: true,
            allowedUsername: "DOMAIN\\user1",
            identityName: "DOMAIN\\user2",
            isAuthenticated: true);

        Assert.False(result);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void IsFallbackAuthorized_ReturnsExpected(bool useNegotiate, bool isAuthenticated, bool expected)
    {
        var result = AuthorizationExtensions.IsFallbackAuthorized(useNegotiate, isAuthenticated);

        Assert.Equal(expected, result);
    }
}
