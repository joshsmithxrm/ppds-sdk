using FluentAssertions;
using PPDS.Auth.Credentials;
using Xunit;

namespace PPDS.Auth.Tests.Credentials;

public class AuthenticationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new AuthenticationException("Test message");

        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new Exception("Inner exception");
        var exception = new AuthenticationException("Test message", inner);

        exception.Message.Should().Be("Test message");
        exception.InnerException.Should().BeSameAs(inner);
        exception.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_SetsBoth()
    {
        var exception = new AuthenticationException("Test message", "AUTH001");

        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("AUTH001");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAll()
    {
        var inner = new Exception("Inner exception");
        var exception = new AuthenticationException("Test message", "AUTH001", inner);

        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("AUTH001");
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Constructor_WithNullInnerException_Allowed()
    {
        var exception = new AuthenticationException("Test message", "AUTH001", null);

        exception.Message.Should().Be("Test message");
        exception.ErrorCode.Should().Be("AUTH001");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void IsException_True()
    {
        var exception = new AuthenticationException("Test");

        exception.Should().BeAssignableTo<Exception>();
    }
}
