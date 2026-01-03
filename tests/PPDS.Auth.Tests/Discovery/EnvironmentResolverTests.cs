using FluentAssertions;
using PPDS.Auth.Discovery;
using Xunit;

namespace PPDS.Auth.Tests.Discovery;

public class EnvironmentResolverTests
{
    private readonly List<DiscoveredEnvironment> _environments;

    public EnvironmentResolverTests()
    {
        _environments = new List<DiscoveredEnvironment>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FriendlyName = "Dev Environment",
                UniqueName = "orgdev",
                ApiUrl = "https://orgdev.crm.dynamics.com",
                UrlName = "orgdev"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FriendlyName = "Test Environment",
                UniqueName = "orgtest",
                ApiUrl = "https://orgtest.crm.dynamics.com",
                UrlName = "orgtest"
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FriendlyName = "Production",
                UniqueName = "orgprod",
                ApiUrl = "https://orgprod.crm.dynamics.com",
                UrlName = "orgprod"
            }
        };
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Resolve_NullOrWhitespace_ReturnsNull(string? identifier)
    {
        var result = EnvironmentResolver.Resolve(_environments, identifier!);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_ByGuid_ReturnsEnvironment()
    {
        var guid = "11111111-1111-1111-1111-111111111111";

        var result = EnvironmentResolver.Resolve(_environments, guid);

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Dev Environment");
    }

    [Fact]
    public void Resolve_ByGuidNotFound_ReturnsNull()
    {
        var guid = "99999999-9999-9999-9999-999999999999";

        var result = EnvironmentResolver.Resolve(_environments, guid);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_ByExactUrl_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "https://orgtest.crm.dynamics.com");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void Resolve_ByUrlWithoutTrailingSlash_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "https://orgtest.crm.dynamics.com/");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void Resolve_ByUrlCaseInsensitive_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "HTTPS://ORGTEST.CRM.DYNAMICS.COM");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void Resolve_ByUniqueName_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "orgprod");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Production");
    }

    [Fact]
    public void Resolve_ByUniqueNameCaseInsensitive_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "ORGPROD");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Production");
    }

    [Fact]
    public void Resolve_ByFriendlyName_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "Production");

        result.Should().NotBeNull();
        result!.UniqueName.Should().Be("orgprod");
    }

    [Fact]
    public void Resolve_ByFriendlyNameCaseInsensitive_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "production");

        result.Should().NotBeNull();
        result!.UniqueName.Should().Be("orgprod");
    }

    [Fact]
    public void Resolve_ByUrlNamePartial_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "orgdev");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Dev Environment");
    }

    [Fact]
    public void Resolve_ByFriendlyNamePartial_SingleMatch_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.Resolve(_environments, "Production");

        result.Should().NotBeNull();
        result!.UniqueName.Should().Be("orgprod");
    }

    [Fact]
    public void Resolve_ByFriendlyNamePartial_MultipleMatches_Throws()
    {
        // Add another environment with "Environment" in the name
        _environments.Add(new DiscoveredEnvironment
        {
            Id = Guid.NewGuid(),
            FriendlyName = "Staging Environment",
            UniqueName = "orgstaging",
            ApiUrl = "https://orgstaging.crm.dynamics.com"
        });

        var act = () => EnvironmentResolver.Resolve(_environments, "Environment");

        act.Should().Throw<AmbiguousMatchException>()
            .WithMessage("*Multiple environments*");
    }

    [Fact]
    public void Resolve_NotFound_ReturnsNull()
    {
        var result = EnvironmentResolver.Resolve(_environments, "nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_EmptyList_ReturnsNull()
    {
        var result = EnvironmentResolver.Resolve(new List<DiscoveredEnvironment>(), "test");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveByUrl_WithValidUrl_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, "https://orgtest.crm.dynamics.com");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void ResolveByUrl_WithTrailingSlash_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, "https://orgtest.crm.dynamics.com/");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void ResolveByUrl_CaseInsensitive_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, "HTTPS://ORGTEST.CRM.DYNAMICS.COM");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Fact]
    public void ResolveByUrl_HostOnly_ReturnsEnvironment()
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, "orgtest.crm.dynamics.com");

        result.Should().NotBeNull();
        result!.FriendlyName.Should().Be("Test Environment");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ResolveByUrl_NullOrWhitespace_ReturnsNull(string? url)
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, url!);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveByUrl_NotFound_ReturnsNull()
    {
        var result = EnvironmentResolver.ResolveByUrl(_environments, "https://nonexistent.crm.dynamics.com");

        result.Should().BeNull();
    }

    [Fact]
    public void AmbiguousMatchException_Constructor_SetsMessage()
    {
        var exception = new AmbiguousMatchException("Test message");

        exception.Message.Should().Be("Test message");
    }

    [Fact]
    public void AmbiguousMatchException_IsException()
    {
        var exception = new AmbiguousMatchException("Test");

        exception.Should().BeAssignableTo<Exception>();
    }
}
