using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class ConnectionResolverTests
{
    [Fact]
    public void Resolve_WithAllEnvVars_ReturnsConfig()
    {
        try
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, "https://test.crm.dynamics.com");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, "test-client-id");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, "test-secret");
            Environment.SetEnvironmentVariable(ConnectionResolver.TenantIdEnvVar, "test-tenant");

            var result = ConnectionResolver.Resolve();

            Assert.Equal("https://test.crm.dynamics.com", result.Url);
            Assert.Equal("test-client-id", result.ClientId);
            Assert.Equal("test-secret", result.ClientSecret);
            Assert.Equal("test-tenant", result.TenantId);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.TenantIdEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithoutTenantId_ReturnsConfigWithNullTenant()
    {
        try
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, "https://test.crm.dynamics.com");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, "test-client-id");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, "test-secret");

            var result = ConnectionResolver.Resolve();

            Assert.Equal("https://test.crm.dynamics.com", result.Url);
            Assert.Equal("test-client-id", result.ClientId);
            Assert.Equal("test-secret", result.ClientSecret);
            Assert.Null(result.TenantId);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithMissingUrl_ThrowsInvalidOperationException()
    {
        try
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, "test-client-id");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, "test-secret");

            var exception = Assert.Throws<InvalidOperationException>(() => ConnectionResolver.Resolve());

            Assert.Contains("URL", exception.Message);
            Assert.Contains(ConnectionResolver.UrlEnvVar, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithMissingClientId_ThrowsInvalidOperationException()
    {
        try
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, "https://test.crm.dynamics.com");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, "test-secret");

            var exception = Assert.Throws<InvalidOperationException>(() => ConnectionResolver.Resolve());

            Assert.Contains("client ID", exception.Message);
            Assert.Contains(ConnectionResolver.ClientIdEnvVar, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithMissingClientSecret_ThrowsInvalidOperationException()
    {
        try
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, "https://test.crm.dynamics.com");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, "test-client-id");
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientSecretEnvVar, null);

            var exception = Assert.Throws<InvalidOperationException>(() => ConnectionResolver.Resolve());

            Assert.Contains("client secret", exception.Message);
            Assert.Contains(ConnectionResolver.ClientSecretEnvVar, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionResolver.UrlEnvVar, null);
            Environment.SetEnvironmentVariable(ConnectionResolver.ClientIdEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithPrefix_UsesCorrectEnvVars()
    {
        try
        {
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}URL", "https://source.crm.dynamics.com");
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}CLIENT_ID", "source-client-id");
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}CLIENT_SECRET", "source-secret");
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}TENANT_ID", "source-tenant");

            var result = ConnectionResolver.Resolve(ConnectionResolver.SourcePrefix, "source");

            Assert.Equal("https://source.crm.dynamics.com", result.Url);
            Assert.Equal("source-client-id", result.ClientId);
            Assert.Equal("source-secret", result.ClientSecret);
            Assert.Equal("source-tenant", result.TenantId);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}URL", null);
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}CLIENT_ID", null);
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}CLIENT_SECRET", null);
            Environment.SetEnvironmentVariable($"{ConnectionResolver.SourcePrefix}TENANT_ID", null);
        }
    }

    [Fact]
    public void GetHelpDescription_IncludesEnvVarNames()
    {
        var result = ConnectionResolver.GetHelpDescription();

        Assert.Contains(ConnectionResolver.UrlEnvVar, result);
        Assert.Contains(ConnectionResolver.ClientIdEnvVar, result);
        Assert.Contains(ConnectionResolver.ClientSecretEnvVar, result);
        Assert.Contains(ConnectionResolver.TenantIdEnvVar, result);
    }

    [Fact]
    public void GetSourceHelpDescription_IncludesSourcePrefix()
    {
        var result = ConnectionResolver.GetSourceHelpDescription();

        Assert.Contains(ConnectionResolver.SourcePrefix, result);
        Assert.Contains("URL", result);
        Assert.Contains("CLIENT_ID", result);
        Assert.Contains("CLIENT_SECRET", result);
    }

    [Fact]
    public void GetTargetHelpDescription_IncludesTargetPrefix()
    {
        var result = ConnectionResolver.GetTargetHelpDescription();

        Assert.Contains(ConnectionResolver.TargetPrefix, result);
        Assert.Contains("URL", result);
        Assert.Contains("CLIENT_ID", result);
        Assert.Contains("CLIENT_SECRET", result);
    }

    [Fact]
    public void EnvironmentVariableNames_AreCorrect()
    {
        Assert.Equal("PPDS_URL", ConnectionResolver.UrlEnvVar);
        Assert.Equal("PPDS_CLIENT_ID", ConnectionResolver.ClientIdEnvVar);
        Assert.Equal("PPDS_CLIENT_SECRET", ConnectionResolver.ClientSecretEnvVar);
        Assert.Equal("PPDS_TENANT_ID", ConnectionResolver.TenantIdEnvVar);
        Assert.Equal("PPDS_SOURCE_", ConnectionResolver.SourcePrefix);
        Assert.Equal("PPDS_TARGET_", ConnectionResolver.TargetPrefix);
    }

    [Fact]
    public void ConnectionConfig_IsRecord()
    {
        var config1 = new ConnectionResolver.ConnectionConfig("url", "clientId", "secret", "tenant");
        var config2 = new ConnectionResolver.ConnectionConfig("url", "clientId", "secret", "tenant");

        Assert.Equal(config1, config2);
    }
}
