using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class ConnectionResolverTests
{
    private const string TestEnvVar = "PPDS_TEST_CONNECTION";

    [Fact]
    public void Resolve_WithArgumentValue_ReturnsArgument()
    {
        var result = ConnectionResolver.Resolve("arg-value", TestEnvVar);

        Assert.Equal("arg-value", result);
    }

    [Fact]
    public void Resolve_WithNullArgument_ReadsEnvironmentVariable()
    {
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, "env-value");

            var result = ConnectionResolver.Resolve(null, TestEnvVar);

            Assert.Equal("env-value", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithEmptyArgument_ReadsEnvironmentVariable()
    {
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, "env-value");

            var result = ConnectionResolver.Resolve("", TestEnvVar);

            Assert.Equal("env-value", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithWhitespaceArgument_ReadsEnvironmentVariable()
    {
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, "env-value");

            var result = ConnectionResolver.Resolve("   ", TestEnvVar);

            Assert.Equal("env-value", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_ArgumentTakesPrecedenceOverEnvVar()
    {
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, "env-value");

            var result = ConnectionResolver.Resolve("arg-value", TestEnvVar);

            Assert.Equal("arg-value", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
    }

    [Fact]
    public void Resolve_WithNoValueAvailable_ThrowsInvalidOperationException()
    {
        Environment.SetEnvironmentVariable(TestEnvVar, null);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectionResolver.Resolve(null, TestEnvVar, "test-connection"));

        Assert.Contains("test-connection", exception.Message);
        Assert.Contains(TestEnvVar, exception.Message);
    }

    [Fact]
    public void TryResolve_WithArgumentValue_ReturnsArgument()
    {
        var result = ConnectionResolver.TryResolve("arg-value", TestEnvVar);

        Assert.Equal("arg-value", result);
    }

    [Fact]
    public void TryResolve_WithEnvVar_ReturnsEnvVar()
    {
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, "env-value");

            var result = ConnectionResolver.TryResolve(null, TestEnvVar);

            Assert.Equal("env-value", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
    }

    [Fact]
    public void TryResolve_WithNoValue_ReturnsNull()
    {
        Environment.SetEnvironmentVariable(TestEnvVar, null);

        var result = ConnectionResolver.TryResolve(null, TestEnvVar);

        Assert.Null(result);
    }

    [Fact]
    public void GetHelpDescription_IncludesEnvVarName()
    {
        var result = ConnectionResolver.GetHelpDescription(TestEnvVar);

        Assert.Contains(TestEnvVar, result);
        Assert.Contains("environment variable", result.ToLower());
    }

    [Fact]
    public void EnvironmentVariableNames_AreCorrect()
    {
        Assert.Equal("PPDS_CONNECTION", ConnectionResolver.ConnectionEnvVar);
        Assert.Equal("PPDS_SOURCE_CONNECTION", ConnectionResolver.SourceConnectionEnvVar);
        Assert.Equal("PPDS_TARGET_CONNECTION", ConnectionResolver.TargetConnectionEnvVar);
    }
}
