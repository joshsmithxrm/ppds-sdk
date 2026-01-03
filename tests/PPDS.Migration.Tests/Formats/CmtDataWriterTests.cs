using FluentAssertions;
using PPDS.Migration.Formats;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Formats;

public class CmtDataWriterTests
{
    [Fact]
    public void Constructor_InitializesSuccessfully()
    {
        var writer = new CmtDataWriter();

        writer.Should().NotBeNull();
    }

    [Fact]
    public async Task WriteAsync_ThrowsWhenDataIsNull()
    {
        var writer = new CmtDataWriter();

        var act = async () => await writer.WriteAsync(null!, "output.zip");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteAsync_ThrowsWhenPathIsNull()
    {
        var writer = new CmtDataWriter();
        var data = new MigrationData();

        var act = async () => await writer.WriteAsync(data, (string)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteAsync_ThrowsWhenPathIsEmpty()
    {
        var writer = new CmtDataWriter();
        var data = new MigrationData();

        var act = async () => await writer.WriteAsync(data, string.Empty);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
