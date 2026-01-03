using FluentAssertions;
using PPDS.Auth.Pooling;
using Xunit;

namespace PPDS.Auth.Tests.Pooling;

public class ConnectionResolverTests
{
    [Fact]
    public void ParseProfileString_NullOrEmpty_ReturnsEmpty()
    {
        var result = ConnectionResolver.ParseProfileString(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseProfileString_Whitespace_ReturnsEmpty()
    {
        var result = ConnectionResolver.ParseProfileString("   ");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseProfileString_SingleProfile_ReturnsSingle()
    {
        var result = ConnectionResolver.ParseProfileString("profile1");

        result.Should().HaveCount(1);
        result[0].Should().Be("profile1");
    }

    [Fact]
    public void ParseProfileString_MultipleProfiles_ReturnsAll()
    {
        var result = ConnectionResolver.ParseProfileString("profile1,profile2,profile3");

        result.Should().HaveCount(3);
        result[0].Should().Be("profile1");
        result[1].Should().Be("profile2");
        result[2].Should().Be("profile3");
    }

    [Fact]
    public void ParseProfileString_WithSpaces_TrimsSpaces()
    {
        var result = ConnectionResolver.ParseProfileString(" profile1 , profile2 , profile3 ");

        result.Should().HaveCount(3);
        result[0].Should().Be("profile1");
        result[1].Should().Be("profile2");
        result[2].Should().Be("profile3");
    }

    [Fact]
    public void ParseProfileString_WithEmptyEntries_SkipsEmpty()
    {
        var result = ConnectionResolver.ParseProfileString("profile1,,profile2,,,profile3");

        result.Should().HaveCount(3);
        result[0].Should().Be("profile1");
        result[1].Should().Be("profile2");
        result[2].Should().Be("profile3");
    }

    [Fact]
    public void ParseProfileString_TrailingComma_SkipsEmpty()
    {
        var result = ConnectionResolver.ParseProfileString("profile1,profile2,");

        result.Should().HaveCount(2);
        result[0].Should().Be("profile1");
        result[1].Should().Be("profile2");
    }

    [Fact]
    public void Constructor_WithNullStore_CreatesDefaultStore()
    {
        var act = () => new ConnectionResolver(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalled()
    {
        var resolver = new ConnectionResolver();

        var act = () => resolver.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var resolver = new ConnectionResolver();

        resolver.Dispose();
        var act = () => resolver.Dispose();

        act.Should().NotThrow();
    }
}
