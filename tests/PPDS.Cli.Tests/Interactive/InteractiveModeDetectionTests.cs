using System.Reflection;
using Xunit;

namespace PPDS.Cli.Tests.Interactive;

/// <summary>
/// Tests for interactive mode detection in Program.cs.
/// </summary>
public class InteractiveModeDetectionTests
{
    private static bool InvokeIsInteractiveMode(string[] args)
    {
        var method = typeof(Program).GetMethod(
            "IsInteractiveMode",
            BindingFlags.NonPublic | BindingFlags.Static);

        return (bool)method!.Invoke(null, new object[] { args })!;
    }

    [Theory]
    [InlineData("interactive")]
    [InlineData("INTERACTIVE")]
    [InlineData("Interactive")]
    public void IsInteractiveMode_WithInteractiveArg_ReturnsTrue(string arg)
    {
        var args = new[] { arg };
        Assert.True(InvokeIsInteractiveMode(args));
    }

    [Theory]
    [InlineData("-i")]
    [InlineData("-I")]
    public void IsInteractiveMode_WithShortFlag_ReturnsTrue(string arg)
    {
        var args = new[] { arg };
        Assert.True(InvokeIsInteractiveMode(args));
    }

    [Theory]
    [InlineData("--interactive")]
    [InlineData("--INTERACTIVE")]
    [InlineData("--Interactive")]
    public void IsInteractiveMode_WithLongFlag_ReturnsTrue(string arg)
    {
        var args = new[] { arg };
        Assert.True(InvokeIsInteractiveMode(args));
    }

    [Fact]
    public void IsInteractiveMode_WithEmptyArgs_ReturnsFalse()
    {
        var args = Array.Empty<string>();
        Assert.False(InvokeIsInteractiveMode(args));
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("env")]
    [InlineData("data")]
    [InlineData("help")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void IsInteractiveMode_WithOtherCommands_ReturnsFalse(string arg)
    {
        var args = new[] { arg };
        Assert.False(InvokeIsInteractiveMode(args));
    }

    [Fact]
    public void IsInteractiveMode_WithInteractiveNotFirst_ReturnsFalse()
    {
        // If "interactive" is not the first argument, should return false
        var args = new[] { "auth", "interactive" };
        Assert.False(InvokeIsInteractiveMode(args));
    }

    [Fact]
    public void IsInteractiveMode_WithAdditionalArgs_ReturnsTrue()
    {
        // Interactive mode detection only checks first arg
        var args = new[] { "interactive", "--extra", "args" };
        Assert.True(InvokeIsInteractiveMode(args));
    }
}
