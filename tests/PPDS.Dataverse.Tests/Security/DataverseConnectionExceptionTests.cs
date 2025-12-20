using PPDS.Dataverse.Security;
using Xunit;

namespace PPDS.Dataverse.Tests.Security;

public class DataverseConnectionExceptionTests
{
    [Fact]
    public void Constructor_WithSensitiveMessage_RedactsMessage()
    {
        var exception = new DataverseConnectionException(
            "Failed with ClientSecret=abc123");

        Assert.Contains("ClientSecret=***REDACTED***", exception.Message);
        Assert.DoesNotContain("abc123", exception.Message);
    }

    [Fact]
    public void Constructor_WithConnectionName_SetsProperty()
    {
        var exception = new DataverseConnectionException(
            "Primary",
            "Connection failed",
            new InvalidOperationException("inner"));

        Assert.Equal("Primary", exception.ConnectionName);
    }

    [Fact]
    public void CreateConnectionFailed_CreatesSanitizedException()
    {
        var innerException = new Exception("Failed: ClientSecret=secret123 is invalid");

        var exception = DataverseConnectionException.CreateConnectionFailed("Primary", innerException);

        Assert.Equal("Primary", exception.ConnectionName);
        Assert.DoesNotContain("secret123", exception.Message);
        Assert.Contains("Primary", exception.Message);
    }

    [Fact]
    public void CreateAuthenticationFailed_CreatesSanitizedException()
    {
        var innerException = new Exception("Auth failed with Password=secret");

        var exception = DataverseConnectionException.CreateAuthenticationFailed("Primary", innerException);

        Assert.Equal("Primary", exception.ConnectionName);
        Assert.DoesNotContain("secret", exception.Message);
        Assert.Contains("Authentication failed", exception.Message);
    }

    [Fact]
    public void ToString_RedactsSensitiveData()
    {
        var innerException = new Exception("ClientSecret=secret123");
        var exception = new DataverseConnectionException("test", innerException);

        var result = exception.ToString();

        Assert.DoesNotContain("secret123", result);
    }

    [Fact]
    public void DefaultConstructor_SetsDefaultMessage()
    {
        var exception = new DataverseConnectionException();

        Assert.Equal("A Dataverse connection error occurred.", exception.Message);
    }

    [Fact]
    public void InnerException_IsPreserved()
    {
        var inner = new InvalidOperationException("Original error");
        var exception = new DataverseConnectionException("Outer message", inner);

        Assert.Same(inner, exception.InnerException);
    }
}
