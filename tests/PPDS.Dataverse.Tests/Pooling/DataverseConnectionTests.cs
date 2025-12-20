using PPDS.Dataverse.Pooling;
using Xunit;

namespace PPDS.Dataverse.Tests.Pooling;

public class DataverseConnectionTests
{
    [Fact]
    public void ToString_ExcludesConnectionString()
    {
        var connection = new DataverseConnection("Primary", "ClientSecret=supersecret123");

        var result = connection.ToString();

        Assert.DoesNotContain("ClientSecret", result);
        Assert.DoesNotContain("supersecret", result);
        Assert.Contains("Primary", result);
        Assert.Contains("MaxPoolSize", result);
    }

    [Fact]
    public void ToString_IncludesNameAndMaxPoolSize()
    {
        var connection = new DataverseConnection("TestConnection", "ignored", 25);

        var result = connection.ToString();

        Assert.Contains("TestConnection", result);
        Assert.Contains("25", result);
    }

    [Fact]
    public void GetRedactedConnectionString_RedactsSecrets()
    {
        var connection = new DataverseConnection(
            "Primary",
            "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=abc;ClientSecret=supersecret");

        var result = connection.GetRedactedConnectionString();

        Assert.Contains("ClientSecret=***REDACTED***", result);
        Assert.DoesNotContain("supersecret", result);
        Assert.Contains("AuthType=ClientSecret", result);
        Assert.Contains("Url=https://org.crm.dynamics.com", result);
    }

    [Fact]
    public void Constructor_WithNameAndConnectionString_SetsProperties()
    {
        var connection = new DataverseConnection("TestName", "TestConnectionString");

        Assert.Equal("TestName", connection.Name);
        Assert.Equal("TestConnectionString", connection.ConnectionString);
        Assert.Equal(10, connection.MaxPoolSize); // Default
    }

    [Fact]
    public void Constructor_WithMaxPoolSize_SetsAllProperties()
    {
        var connection = new DataverseConnection("TestName", "TestConnectionString", 50);

        Assert.Equal("TestName", connection.Name);
        Assert.Equal("TestConnectionString", connection.ConnectionString);
        Assert.Equal(50, connection.MaxPoolSize);
    }

    [Fact]
    public void DefaultConstructor_SetsDefaults()
    {
        var connection = new DataverseConnection();

        Assert.Equal(string.Empty, connection.Name);
        Assert.Equal(string.Empty, connection.ConnectionString);
        Assert.Equal(10, connection.MaxPoolSize);
    }
}
