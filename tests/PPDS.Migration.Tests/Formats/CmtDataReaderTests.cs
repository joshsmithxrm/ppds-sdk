using FluentAssertions;
using PPDS.Migration.Formats;
using Xunit;

namespace PPDS.Migration.Tests.Formats;

public class CmtDataReaderTests
{
    [Fact]
    public void Constructor_RequiresSchemaReader()
    {
        var act = () => new CmtDataReader(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenPathIsNull()
    {
        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        var act = async () => await reader.ReadAsync((string)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenPathIsEmpty()
    {
        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        var act = async () => await reader.ReadAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenFileNotFound()
    {
        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        var act = async () => await reader.ReadAsync("nonexistent.zip");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
