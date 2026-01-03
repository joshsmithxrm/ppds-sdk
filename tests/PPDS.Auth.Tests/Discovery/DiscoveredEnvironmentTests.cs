using FluentAssertions;
using PPDS.Auth.Discovery;
using Xunit;

namespace PPDS.Auth.Tests.Discovery;

public class DiscoveredEnvironmentTests
{
    [Fact]
    public void IsEnabled_StateZero_ReturnsTrue()
    {
        var env = new DiscoveredEnvironment { State = 0 };

        env.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_StateNonZero_ReturnsFalse()
    {
        var env = new DiscoveredEnvironment { State = 1 };

        env.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsTrial_HasTrialExpirationDate_ReturnsTrue()
    {
        var env = new DiscoveredEnvironment
        {
            TrialExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        env.IsTrial.Should().BeTrue();
    }

    [Fact]
    public void IsTrial_NoTrialExpirationDate_ReturnsFalse()
    {
        var env = new DiscoveredEnvironment();

        env.IsTrial.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, "Production")]
    [InlineData(5, "Sandbox")]
    [InlineData(6, "Sandbox")]
    [InlineData(7, "Preview")]
    [InlineData(9, "TestDrive")]
    [InlineData(11, "Trial")]
    [InlineData(12, "Default")]
    [InlineData(13, "Developer")]
    [InlineData(14, "Trial")]
    [InlineData(15, "Teams")]
    [InlineData(999, "Production")]
    public void EnvironmentType_ReturnsCorrectType(int organizationType, string expectedType)
    {
        var env = new DiscoveredEnvironment { OrganizationType = organizationType };

        env.EnvironmentType.Should().Be(expectedType);
    }

    [Fact]
    public void ToString_ReturnsFriendlyNameAndUniqueName()
    {
        var env = new DiscoveredEnvironment
        {
            FriendlyName = "My Environment",
            UniqueName = "myorg"
        };

        var result = env.ToString();

        result.Should().Contain("My Environment");
        result.Should().Contain("myorg");
    }

    [Fact]
    public void DefaultConstructor_InitializesEmptyStrings()
    {
        var env = new DiscoveredEnvironment();

        env.FriendlyName.Should().BeEmpty();
        env.UniqueName.Should().BeEmpty();
        env.ApiUrl.Should().BeEmpty();
    }

    [Fact]
    public void DefaultConstructor_NullablePropertiesAreNull()
    {
        var env = new DiscoveredEnvironment();

        env.EnvironmentId.Should().BeNull();
        env.UrlName.Should().BeNull();
        env.Url.Should().BeNull();
        env.Version.Should().BeNull();
        env.Region.Should().BeNull();
        env.TenantId.Should().BeNull();
        env.TrialExpirationDate.Should().BeNull();
    }

    [Fact]
    public void Id_CanBeSet()
    {
        var guid = Guid.NewGuid();
        var env = new DiscoveredEnvironment { Id = guid };

        env.Id.Should().Be(guid);
    }

    [Fact]
    public void State_CanBeSet()
    {
        var env = new DiscoveredEnvironment { State = 1 };

        env.State.Should().Be(1);
    }

    [Fact]
    public void OrganizationType_CanBeSet()
    {
        var env = new DiscoveredEnvironment { OrganizationType = 5 };

        env.OrganizationType.Should().Be(5);
    }

    [Fact]
    public void IsUserSysAdmin_CanBeSet()
    {
        var env = new DiscoveredEnvironment { IsUserSysAdmin = true };

        env.IsUserSysAdmin.Should().BeTrue();
    }
}
