using PPDS.Dataverse.Security;
using Xunit;

namespace PPDS.Dataverse.Tests.Security;

public class ConnectionStringRedactorTests
{
    [Fact]
    public void Redact_WithClientSecret_RedactsValue()
    {
        var connectionString = "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=abc;ClientSecret=supersecret123";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains("ClientSecret=***REDACTED***", result);
        Assert.DoesNotContain("supersecret123", result);
    }

    [Fact]
    public void Redact_WithPassword_RedactsValue()
    {
        var connectionString = "Server=myserver;Database=mydb;User=admin;Password=secret123";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains("Password=***REDACTED***", result);
        Assert.DoesNotContain("secret123", result);
    }

    [Fact]
    public void Redact_WithMultipleSecrets_RedactsAll()
    {
        var connectionString = "ClientSecret=secret1;Password=secret2;ApiKey=secret3";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains("ClientSecret=***REDACTED***", result);
        Assert.Contains("Password=***REDACTED***", result);
        Assert.Contains("ApiKey=***REDACTED***", result);
        Assert.DoesNotContain("secret1", result);
        Assert.DoesNotContain("secret2", result);
        Assert.DoesNotContain("secret3", result);
    }

    [Fact]
    public void Redact_PreservesNonSensitiveValues()
    {
        var connectionString = "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=abc;ClientSecret=supersecret";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains("AuthType=ClientSecret", result);
        Assert.Contains("Url=https://org.crm.dynamics.com", result);
        Assert.Contains("ClientId=abc", result);
    }

    [Fact]
    public void Redact_IsCaseInsensitive()
    {
        var connectionString = "clientsecret=secret1;PASSWORD=secret2;ApiKey=secret3";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.DoesNotContain("secret1", result);
        Assert.DoesNotContain("secret2", result);
        Assert.DoesNotContain("secret3", result);
    }

    [Fact]
    public void Redact_WithNullInput_ReturnsEmptyString()
    {
        var result = ConnectionStringRedactor.Redact(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Redact_WithEmptyInput_ReturnsEmptyString()
    {
        var result = ConnectionStringRedactor.Redact(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Redact_WithNoSensitiveData_ReturnsOriginal()
    {
        var connectionString = "Server=myserver;Database=mydb;IntegratedSecurity=true";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void Redact_WithQuotedValue_RedactsQuotedContent()
    {
        var connectionString = "ClientSecret=\"super secret with spaces\"";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains("ClientSecret=***REDACTED***", result);
        Assert.DoesNotContain("super secret with spaces", result);
    }

    [Fact]
    public void RedactExceptionMessage_RedactsSensitiveData()
    {
        var message = "Connection failed with ClientSecret=abc123 for user";

        var result = ConnectionStringRedactor.RedactExceptionMessage(message);

        Assert.Contains("ClientSecret=***REDACTED***", result);
        Assert.DoesNotContain("abc123", result);
    }

    [Fact]
    public void ContainsSensitiveData_WithSecret_ReturnsTrue()
    {
        var connectionString = "Server=x;Password=secret";

        var result = ConnectionStringRedactor.ContainsSensitiveData(connectionString);

        Assert.True(result);
    }

    [Fact]
    public void ContainsSensitiveData_WithoutSecret_ReturnsFalse()
    {
        var connectionString = "Server=myserver;Database=mydb";

        var result = ConnectionStringRedactor.ContainsSensitiveData(connectionString);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Token")]
    [InlineData("AccessToken")]
    [InlineData("RefreshToken")]
    [InlineData("SharedAccessKey")]
    [InlineData("AccountKey")]
    [InlineData("Credential")]
    [InlineData("Pwd")]
    [InlineData("Key")]
    [InlineData("Secret")]
    public void Redact_RedactsAllSensitiveKeyTypes(string keyName)
    {
        var connectionString = $"{keyName}=mysensitivevalue123";

        var result = ConnectionStringRedactor.Redact(connectionString);

        Assert.Contains($"{keyName}=***REDACTED***", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysensitivevalue123", result);
    }
}
