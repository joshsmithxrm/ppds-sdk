using System.IO.Compression;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
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

    [Theory]
    [InlineData("int", "42")]
    [InlineData("integer", "42")]
    [InlineData("number", "42")] // CMT alias for integer
    public async Task ReadAsync_ParsesIntegerTypes(string type, string value)
    {
        // Arrange
        var stream = CreateTestArchive(
            schemaXml: $@"<entities><entity name=""test"" displayname=""Test""><fields><field name=""testfield"" type=""{type}"" displayname=""Test Field"" /></fields></entity></entities>",
            dataXml: $@"<entities><entity name=""test""><records><record id=""11111111-1111-1111-1111-111111111111""><field name=""testfield"" value=""{value}"" /></record></records></entity></entities>"
        );

        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        // Act
        var result = await reader.ReadAsync(stream);

        // Assert
        result.EntityData.Should().ContainKey("test");
        var record = result.EntityData["test"].First();
        record["testfield"].Should().Be(42);
        record["testfield"].Should().BeOfType<int>();
    }

    [Fact]
    public async Task ReadAsync_ParsesBigIntType()
    {
        // Arrange
        var bigValue = "9223372036854775807"; // long.MaxValue
        var stream = CreateTestArchive(
            schemaXml: @"<entities><entity name=""test"" displayname=""Test""><fields><field name=""testfield"" type=""bigint"" displayname=""Test Field"" /></fields></entity></entities>",
            dataXml: $@"<entities><entity name=""test""><records><record id=""11111111-1111-1111-1111-111111111111""><field name=""testfield"" value=""{bigValue}"" /></record></records></entity></entities>"
        );

        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        // Act
        var result = await reader.ReadAsync(stream);

        // Assert
        result.EntityData.Should().ContainKey("test");
        var record = result.EntityData["test"].First();
        record["testfield"].Should().Be(long.MaxValue);
        record["testfield"].Should().BeOfType<long>();
    }

    [Theory]
    [InlineData("lookup")]
    [InlineData("entityreference")]
    [InlineData("customer")]
    [InlineData("owner")]
    [InlineData("partylist")] // CMT email participant type
    public async Task ReadAsync_ParsesLookupTypes(string type)
    {
        // Arrange
        var targetId = "22222222-2222-2222-2222-222222222222";
        var stream = CreateTestArchive(
            schemaXml: $@"<entities><entity name=""test"" displayname=""Test""><fields><field name=""testfield"" type=""{type}"" displayname=""Test Field"" /></fields></entity></entities>",
            dataXml: $@"<entities><entity name=""test""><records><record id=""11111111-1111-1111-1111-111111111111""><field name=""testfield"" lookupentity=""account"">{targetId}</field></record></records></entity></entities>"
        );

        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        // Act
        var result = await reader.ReadAsync(stream);

        // Assert
        result.EntityData.Should().ContainKey("test");
        var record = result.EntityData["test"].First();
        record["testfield"].Should().BeOfType<EntityReference>();
        var entityRef = (EntityReference)record["testfield"];
        entityRef.LogicalName.Should().Be("account");
        entityRef.Id.Should().Be(new Guid(targetId));
    }

    [Theory]
    [InlineData("optionset", "100000000")]
    [InlineData("optionsetvalue", "100000001")] // CMT format
    [InlineData("picklist", "100000002")]
    public async Task ReadAsync_ParsesOptionSetTypes(string type, string value)
    {
        // Arrange
        var stream = CreateTestArchive(
            schemaXml: $@"<entities><entity name=""test"" displayname=""Test""><fields><field name=""testfield"" type=""{type}"" displayname=""Test Field"" /></fields></entity></entities>",
            dataXml: $@"<entities><entity name=""test""><records><record id=""11111111-1111-1111-1111-111111111111""><field name=""testfield"" value=""{value}"" /></record></records></entity></entities>"
        );

        var schemaReader = new CmtSchemaReader();
        var reader = new CmtDataReader(schemaReader);

        // Act
        var result = await reader.ReadAsync(stream);

        // Assert
        result.EntityData.Should().ContainKey("test");
        var record = result.EntityData["test"].First();
        record["testfield"].Should().BeOfType<OptionSetValue>();
        var osv = (OptionSetValue)record["testfield"];
        osv.Value.Should().Be(int.Parse(value));
    }

    private static MemoryStream CreateTestArchive(string schemaXml, string dataXml)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var schemaEntry = archive.CreateEntry("data_schema.xml");
            using (var writer = new StreamWriter(schemaEntry.Open()))
            {
                writer.Write(schemaXml);
            }

            var dataEntry = archive.CreateEntry("data.xml");
            using (var writer = new StreamWriter(dataEntry.Open()))
            {
                writer.Write(dataXml);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
