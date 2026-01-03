using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using PPDS.Auth.Credentials;
using Xunit;

namespace PPDS.Auth.Tests.Credentials;

public class JwtClaimsParserTests
{
    [Fact]
    public void Parse_ClaimsPrincipalWithPuid_ExtractsPuid()
    {
        var claims = new[] { new Claim("puid", "test-puid-123") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var result = JwtClaimsParser.Parse(principal, null);

        result.Should().NotBeNull();
        result!.Puid.Should().Be("test-puid-123");
    }

    [Fact]
    public void Parse_ClaimsPrincipalWithoutPuid_ReturnsNull()
    {
        var claims = new[] { new Claim("sub", "subject") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var result = JwtClaimsParser.Parse(principal, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_NullClaimsPrincipalAndNullToken_ReturnsNull()
    {
        var result = JwtClaimsParser.Parse(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ValidJwtWithPuid_ExtractsPuid()
    {
        // Create a simple JWT with puid claim
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new[] { new Claim("puid", "jwt-puid-456") }
        );
        var tokenString = handler.WriteToken(token);

        var result = JwtClaimsParser.Parse(tokenString);

        result.Should().NotBeNull();
        result!.Puid.Should().Be("jwt-puid-456");
    }

    [Fact]
    public void Parse_JwtWithoutPuid_ReturnsNull()
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new[] { new Claim("sub", "subject") }
        );
        var tokenString = handler.WriteToken(token);

        var result = JwtClaimsParser.Parse(tokenString);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidJwt_ReturnsNull()
    {
        var invalidToken = "not-a-valid-jwt";

        var result = JwtClaimsParser.Parse(invalidToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void Parse_NullOrEmptyToken_ReturnsNull(string? token)
    {
        var result = JwtClaimsParser.Parse(token);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ClaimsPrincipalTakesPrecedenceOverToken()
    {
        var claims = new[] { new Claim("puid", "principal-puid") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new[] { new Claim("puid", "token-puid") }
        );
        var tokenString = handler.WriteToken(token);

        var result = JwtClaimsParser.Parse(principal, tokenString);

        result.Should().NotBeNull();
        result!.Puid.Should().Be("principal-puid");
    }

    [Fact]
    public void Parse_FallsBackToTokenWhenPrincipalHasNoPuid()
    {
        var claims = new[] { new Claim("sub", "subject") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new[] { new Claim("puid", "token-puid") }
        );
        var tokenString = handler.WriteToken(token);

        var result = JwtClaimsParser.Parse(principal, tokenString);

        result.Should().NotBeNull();
        result!.Puid.Should().Be("token-puid");
    }

    [Fact]
    public void Parse_PuidClaim_CaseInsensitive()
    {
        // Test with uppercase PUID claim
        var claims = new[] { new Claim("PUID", "test-puid-uppercase") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var result = JwtClaimsParser.Parse(principal, null);

        result.Should().NotBeNull();
        result!.Puid.Should().Be("test-puid-uppercase");
    }
}
