using PPDS.Cli.Interactive.Components.QueryResults;
using PPDS.Dataverse.Query;
using Xunit;

namespace PPDS.Cli.Tests.Interactive.Components.QueryResults;

/// <summary>
/// Tests for FieldGrouper field categorization logic.
/// </summary>
public class FieldGrouperTests
{
    [Fact]
    public void GroupFields_IdentifiesGuidAsIdentifier()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "accountid", DataType = QueryColumnType.Guid }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["accountid"] = new() { Value = Guid.NewGuid() }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var identifiers = groups.First(g => g.Name == "Identifiers");
        Assert.Single(identifiers.Fields);
        Assert.Equal("accountid", identifiers.Fields[0].Column.LogicalName);
    }

    [Fact]
    public void GroupFields_IdentifiesLookupAsIdentifier()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "primarycontactid", DataType = QueryColumnType.Lookup }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["primarycontactid"] = new() { Value = Guid.NewGuid() }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var identifiers = groups.First(g => g.Name == "Identifiers");
        Assert.Single(identifiers.Fields);
    }

    [Fact]
    public void GroupFields_IdentifiesFieldEndingInIdAsIdentifier()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "ownerid", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["ownerid"] = new() { Value = "test" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var identifiers = groups.First(g => g.Name == "Identifiers");
        Assert.Single(identifiers.Fields);
    }

    [Fact]
    public void GroupFields_IdentifiesSystemFields()
    {
        // Note: Fields ending in "id" are classified as identifiers first,
        // so we test system fields that don't end in "id"
        var systemFieldNames = new[]
        {
            "createdon", "createdby", "modifiedon", "modifiedby",
            "owninguser", "statecode", "statuscode", "versionnumber"
        };

        foreach (var fieldName in systemFieldNames)
        {
            var columns = new List<QueryColumn>
            {
                new() { LogicalName = fieldName, DataType = QueryColumnType.String }
            };
            var record = new Dictionary<string, QueryValue>
            {
                [fieldName] = new() { Value = "test" }
            };

            var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

            var systemGroup = groups.First(g => g.Name == "System");
            Assert.Single(systemGroup.Fields);
        }
    }

    [Fact]
    public void GroupFields_SystemFieldsAreCaseInsensitive()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "CREATEDON", DataType = QueryColumnType.DateTime }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["CREATEDON"] = new() { Value = DateTime.Now }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var systemGroup = groups.First(g => g.Name == "System");
        Assert.Single(systemGroup.Fields);
    }

    [Fact]
    public void GroupFields_RegularFieldsToCoreFields()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String },
            new() { LogicalName = "description", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" },
            ["description"] = new() { Value = "Description" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal(2, coreFields.Fields.Count);
    }

    [Fact]
    public void GroupFields_ExcludesNullsWhenRequested()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String },
            new() { LogicalName = "description", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" },
            ["description"] = new() { Value = null }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: false);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Single(coreFields.Fields);
        Assert.Equal("name", coreFields.Fields[0].Column.LogicalName);
    }

    [Fact]
    public void GroupFields_IncludesNullsWhenRequested()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String },
            new() { LogicalName = "description", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" },
            ["description"] = new() { Value = null }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal(2, coreFields.Fields.Count);
    }

    [Fact]
    public void GroupFields_UsesDisplayNameWhenAvailable()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DisplayName = "Account Name", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal("Account Name", coreFields.Fields[0].DisplayName);
    }

    [Fact]
    public void GroupFields_UsesAliasWhenNoDisplayName()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", Alias = "accountname", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["accountname"] = new() { Value = "Test" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal("accountname", coreFields.Fields[0].DisplayName);
    }

    [Fact]
    public void GroupFields_FallsBackToLogicalName()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal("name", coreFields.Fields[0].DisplayName);
    }

    [Fact]
    public void GroupFields_ReturnsThreeGroups()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["name"] = new() { Value = "Test" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        Assert.Equal(3, groups.Count);
        Assert.Contains(groups, g => g.Name == "Identifiers");
        Assert.Contains(groups, g => g.Name == "Core Fields");
        Assert.Contains(groups, g => g.Name == "System");
    }

    [Fact]
    public void GroupFields_MissingRecordValue_StillIncludedWithNull()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>(); // Empty record

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Single(coreFields.Fields);
        Assert.Null(coreFields.Fields[0].Value);
    }

    [Fact]
    public void GroupFields_UsesAliasForRecordLookup()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "name", Alias = "accountname", DataType = QueryColumnType.String }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["accountname"] = new() { Value = "Test Value" }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        var coreFields = groups.First(g => g.Name == "Core Fields");
        Assert.Equal("Test Value", coreFields.Fields[0].Value?.Value);
    }

    [Fact]
    public void GroupFields_MixedFields_GroupedCorrectly()
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = "accountid", DataType = QueryColumnType.Guid },
            new() { LogicalName = "name", DataType = QueryColumnType.String },
            new() { LogicalName = "createdon", DataType = QueryColumnType.DateTime }
        };
        var record = new Dictionary<string, QueryValue>
        {
            ["accountid"] = new() { Value = Guid.NewGuid() },
            ["name"] = new() { Value = "Test" },
            ["createdon"] = new() { Value = DateTime.Now }
        };

        var groups = FieldGrouper.GroupFields(columns, record, includeNulls: true);

        Assert.Single(groups.First(g => g.Name == "Identifiers").Fields);
        Assert.Single(groups.First(g => g.Name == "Core Fields").Fields);
        Assert.Single(groups.First(g => g.Name == "System").Fields);
    }
}
