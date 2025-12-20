using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class ConsoleOutputTests
{
    #region WriteProgress Tests

    [Fact]
    public void WriteProgress_WithJsonFalse_WritesTextFormat()
    {
        var output = CaptureConsoleOutput(() => ConsoleOutput.WriteProgress("test", "message", json: false));
        Assert.Equal("[test] message", output.Trim());
    }

    [Fact]
    public void WriteProgress_WithJsonTrue_WritesJsonFormat()
    {
        var output = CaptureConsoleOutput(() => ConsoleOutput.WriteProgress("test", "message", json: true));
        Assert.Contains("\"phase\":\"test\"", output);
        Assert.Contains("\"message\":\"message\"", output);
        Assert.Contains("\"timestamp\":", output);
    }

    #endregion

    #region WriteCompletion Tests

    [Fact]
    public void WriteCompletion_WithJsonFalse_WritesNothing()
    {
        var output = CaptureConsoleOutput(() => ConsoleOutput.WriteCompletion(TimeSpan.FromSeconds(5), 100, 2, json: false));
        Assert.Empty(output);
    }

    [Fact]
    public void WriteCompletion_WithJsonTrue_WritesJsonFormat()
    {
        var output = CaptureConsoleOutput(() => ConsoleOutput.WriteCompletion(TimeSpan.FromSeconds(5), 100, 2, json: true));
        Assert.Contains("\"phase\":\"complete\"", output);
        Assert.Contains("\"recordsProcessed\":100", output);
        Assert.Contains("\"errors\":2", output);
        Assert.Contains("\"timestamp\":", output);
    }

    #endregion

    #region WriteError Tests

    [Fact]
    public void WriteError_WithJsonFalse_WritesTextFormat()
    {
        var output = CaptureConsoleError(() => ConsoleOutput.WriteError("test error", json: false));
        Assert.Equal("Error: test error", output.Trim());
    }

    [Fact]
    public void WriteError_WithJsonTrue_WritesJsonFormat()
    {
        var output = CaptureConsoleError(() => ConsoleOutput.WriteError("test error", json: true));
        Assert.Contains("\"phase\":\"error\"", output);
        Assert.Contains("\"message\":\"test error\"", output);
        Assert.Contains("\"timestamp\":", output);
    }

    #endregion

    #region EscapeJson Tests

    [Fact]
    public void EscapeJson_EscapesBackslashes()
    {
        var result = ConsoleOutput.EscapeJson("path\\to\\file");
        Assert.Equal("path\\\\to\\\\file", result);
    }

    [Fact]
    public void EscapeJson_EscapesQuotes()
    {
        var result = ConsoleOutput.EscapeJson("say \"hello\"");
        Assert.Equal("say \\\"hello\\\"", result);
    }

    [Fact]
    public void EscapeJson_EscapesNewlines()
    {
        var result = ConsoleOutput.EscapeJson("line1\nline2");
        Assert.Equal("line1\\nline2", result);
    }

    [Fact]
    public void EscapeJson_EscapesCarriageReturns()
    {
        var result = ConsoleOutput.EscapeJson("line1\rline2");
        Assert.Equal("line1\\rline2", result);
    }

    [Fact]
    public void EscapeJson_HandlesComplexString()
    {
        var result = ConsoleOutput.EscapeJson("Error: \"file\\path\"\nDetails");
        Assert.Equal("Error: \\\"file\\\\path\\\"\\nDetails", result);
    }

    #endregion

    #region Helpers

    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            action();
            return stringWriter.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string CaptureConsoleError(Action action)
    {
        var originalError = Console.Error;
        using var stringWriter = new StringWriter();
        Console.SetError(stringWriter);
        try
        {
            action();
            return stringWriter.ToString();
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    #endregion
}
