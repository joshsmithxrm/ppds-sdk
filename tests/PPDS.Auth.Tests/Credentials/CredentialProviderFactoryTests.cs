using FluentAssertions;
using PPDS.Auth.Cloud;
using PPDS.Auth.Credentials;
using PPDS.Auth.Profiles;
using Xunit;

namespace PPDS.Auth.Tests.Credentials;

public class CredentialProviderFactoryTests
{
    [Fact]
    public void Create_NullProfile_Throws()
    {
        var act = () => CredentialProviderFactory.Create(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_InteractiveBrowser_ReturnsInteractiveBrowserProvider()
    {
        var profile = new AuthProfile { AuthMethod = AuthMethod.InteractiveBrowser };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<InteractiveBrowserCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_ClientSecret_WithValidProfile_ReturnsClientSecretProvider()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.ClientSecret,
            ApplicationId = "app-id",
            ClientSecret = "secret",
            TenantId = "tenant-id"
        };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<ClientSecretCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_ClientSecret_MissingApplicationId_Throws()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.ClientSecret,
            ClientSecret = "secret",
            TenantId = "tenant-id"
        };

        var act = () => CredentialProviderFactory.Create(profile);

        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("ApplicationId"));
    }

    [Fact]
    public void Create_ManagedIdentity_ReturnsManagedIdentityProvider()
    {
        var profile = new AuthProfile { AuthMethod = AuthMethod.ManagedIdentity };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<ManagedIdentityCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_GitHubFederated_WithValidProfile_ReturnsGitHubProvider()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.GitHubFederated,
            ApplicationId = "app-id",
            TenantId = "tenant-id"
        };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<GitHubFederatedCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_GitHubFederated_MissingApplicationId_Throws()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.GitHubFederated,
            TenantId = "tenant-id"
        };

        var act = () => CredentialProviderFactory.Create(profile);

        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("ApplicationId"));
    }

    [Fact]
    public void Create_GitHubFederated_MissingTenantId_Throws()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.GitHubFederated,
            ApplicationId = "app-id"
        };

        var act = () => CredentialProviderFactory.Create(profile);

        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("TenantId"));
    }

    [Fact]
    public void Create_AzureDevOpsFederated_WithValidProfile_ReturnsAzureDevOpsProvider()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.AzureDevOpsFederated,
            ApplicationId = "app-id",
            TenantId = "tenant-id"
        };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<AzureDevOpsFederatedCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_UsernamePassword_WithValidProfile_ReturnsUsernamePasswordProvider()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.UsernamePassword,
            Username = "user@example.com",
            Password = "password"
        };

        var provider = CredentialProviderFactory.Create(profile);

        provider.Should().BeOfType<UsernamePasswordCredentialProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Create_UsernamePassword_MissingUsername_Throws()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.UsernamePassword,
            Password = "password"
        };

        var act = () => CredentialProviderFactory.Create(profile);

        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Username"));
    }

    [Fact]
    public void Create_UsernamePassword_MissingPassword_Throws()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.UsernamePassword,
            Username = "user@example.com"
        };

        var act = () => CredentialProviderFactory.Create(profile);

        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Password"));
    }

    [Theory]
    [InlineData(AuthMethod.InteractiveBrowser)]
    [InlineData(AuthMethod.DeviceCode)]
    [InlineData(AuthMethod.ClientSecret)]
    [InlineData(AuthMethod.CertificateFile)]
    [InlineData(AuthMethod.CertificateStore)]
    [InlineData(AuthMethod.ManagedIdentity)]
    [InlineData(AuthMethod.GitHubFederated)]
    [InlineData(AuthMethod.AzureDevOpsFederated)]
    [InlineData(AuthMethod.UsernamePassword)]
    public void IsSupported_ValidAuthMethod_ReturnsTrue(AuthMethod authMethod)
    {
        var result = CredentialProviderFactory.IsSupported(authMethod);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSupported_InvalidAuthMethod_ReturnsFalse()
    {
        var result = CredentialProviderFactory.IsSupported((AuthMethod)999);

        result.Should().BeFalse();
    }
}
