using FluentAssertions;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Models;

public class EntitySchemaTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var schema = new EntitySchema();

        schema.LogicalName.Should().BeEmpty();
        schema.DisplayName.Should().BeEmpty();
        schema.PrimaryIdField.Should().BeEmpty();
        schema.PrimaryNameField.Should().BeEmpty();
        schema.DisablePlugins.Should().BeFalse();
        schema.ObjectTypeCode.Should().BeNull();
        schema.Fields.Should().NotBeNull().And.BeEmpty();
        schema.Relationships.Should().NotBeNull().And.BeEmpty();
        schema.FetchXmlFilter.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var schema = new EntitySchema
        {
            LogicalName = "account",
            DisplayName = "Account",
            PrimaryIdField = "accountid",
            PrimaryNameField = "name",
            DisablePlugins = true,
            ObjectTypeCode = 1,
            FetchXmlFilter = "<filter><condition attribute='statecode' operator='eq' value='0'/></filter>"
        };

        schema.LogicalName.Should().Be("account");
        schema.DisplayName.Should().Be("Account");
        schema.PrimaryIdField.Should().Be("accountid");
        schema.PrimaryNameField.Should().Be("name");
        schema.DisablePlugins.Should().BeTrue();
        schema.ObjectTypeCode.Should().Be(1);
        schema.FetchXmlFilter.Should().NotBeNull();
    }

    [Fact]
    public void Fields_CanBeAssigned()
    {
        var fields = new List<FieldSchema>
        {
            new() { LogicalName = "name", Type = "string" },
            new() { LogicalName = "accountid", Type = "uniqueidentifier", IsPrimaryKey = true }
        };

        var schema = new EntitySchema { Fields = fields };

        schema.Fields.Should().HaveCount(2);
        schema.Fields.Should().Contain(f => f.LogicalName == "name");
        schema.Fields.Should().Contain(f => f.IsPrimaryKey);
    }

    [Fact]
    public void Relationships_CanBeAssigned()
    {
        var relationships = new List<RelationshipSchema>
        {
            new() { Name = "account_contact", Entity1 = "contact", Entity2 = "account" },
            new() { Name = "systemuser_account", Entity1 = "account", Entity2 = "systemuser", IsManyToMany = true }
        };

        var schema = new EntitySchema { Relationships = relationships };

        schema.Relationships.Should().HaveCount(2);
        schema.Relationships.Should().Contain(r => r.Name == "account_contact");
        schema.Relationships.Should().Contain(r => r.IsManyToMany);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var schema = new EntitySchema
        {
            LogicalName = "account",
            DisplayName = "Account"
        };

        var result = schema.ToString();

        result.Should().Be("account (Account)");
    }
}
