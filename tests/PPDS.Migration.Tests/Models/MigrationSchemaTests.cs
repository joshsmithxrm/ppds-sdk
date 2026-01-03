using FluentAssertions;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Models;

public class MigrationSchemaTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var schema = new MigrationSchema();

        schema.Version.Should().BeEmpty();
        schema.GeneratedAt.Should().BeNull();
        schema.Entities.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var timestamp = DateTime.UtcNow;
        var entities = new List<EntitySchema>
        {
            new() { LogicalName = "account", DisplayName = "Account" },
            new() { LogicalName = "contact", DisplayName = "Contact" }
        };

        var schema = new MigrationSchema
        {
            Version = "1.0",
            GeneratedAt = timestamp,
            Entities = entities
        };

        schema.Version.Should().Be("1.0");
        schema.GeneratedAt.Should().Be(timestamp);
        schema.Entities.Should().HaveCount(2);
    }

    [Fact]
    public void GetEntity_ReturnsEntityWhenFound()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "account", DisplayName = "Account" },
                new() { LogicalName = "contact", DisplayName = "Contact" }
            }
        };

        var result = schema.GetEntity("account");

        result.Should().NotBeNull();
        result!.LogicalName.Should().Be("account");
        result.DisplayName.Should().Be("Account");
    }

    [Fact]
    public void GetEntity_IsCaseInsensitive()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "account", DisplayName = "Account" }
            }
        };

        var result = schema.GetEntity("ACCOUNT");

        result.Should().NotBeNull();
        result!.LogicalName.Should().Be("account");
    }

    [Fact]
    public void GetEntity_ReturnsNullWhenNotFound()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "account", DisplayName = "Account" }
            }
        };

        var result = schema.GetEntity("contact");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAllLookupFields_ReturnsAllLookupFields()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "contact",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "firstname", Type = "string" },
                        new() { LogicalName = "parentcustomerid", Type = "lookup", LookupEntity = "account" }
                    }
                },
                new()
                {
                    LogicalName = "opportunity",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "customerid", Type = "customer", LookupEntity = "account|contact" },
                        new() { LogicalName = "name", Type = "string" }
                    }
                }
            }
        };

        var lookupFields = schema.GetAllLookupFields().ToList();

        lookupFields.Should().HaveCount(2);
        lookupFields.Should().Contain(tuple => tuple.Entity.LogicalName == "contact" && tuple.Field.LogicalName == "parentcustomerid");
        lookupFields.Should().Contain(tuple => tuple.Entity.LogicalName == "opportunity" && tuple.Field.LogicalName == "customerid");
    }

    [Fact]
    public void GetAllLookupFields_ReturnsEmptyWhenNoLookups()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "account",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "name", Type = "string" },
                        new() { LogicalName = "revenue", Type = "money" }
                    }
                }
            }
        };

        var lookupFields = schema.GetAllLookupFields().ToList();

        lookupFields.Should().BeEmpty();
    }

    [Fact]
    public void GetAllManyToManyRelationships_ReturnsUniqueRelationships()
    {
        var m2mRel = new RelationshipSchema
        {
            Name = "systemuserroles_association",
            IsManyToMany = true,
            Entity1 = "systemuser",
            Entity2 = "role"
        };

        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "systemuser",
                    Relationships = new List<RelationshipSchema> { m2mRel }
                },
                new()
                {
                    LogicalName = "role",
                    Relationships = new List<RelationshipSchema> { m2mRel }
                }
            }
        };

        var m2mRelationships = schema.GetAllManyToManyRelationships().ToList();

        m2mRelationships.Should().HaveCount(1);
        m2mRelationships[0].Name.Should().Be("systemuserroles_association");
    }

    [Fact]
    public void GetAllManyToManyRelationships_FiltersOutOneToMany()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "contact",
                    Relationships = new List<RelationshipSchema>
                    {
                        new() { Name = "account_contact", IsManyToMany = false },
                        new() { Name = "contactleads_association", IsManyToMany = true }
                    }
                }
            }
        };

        var m2mRelationships = schema.GetAllManyToManyRelationships().ToList();

        m2mRelationships.Should().HaveCount(1);
        m2mRelationships[0].Name.Should().Be("contactleads_association");
    }

    [Fact]
    public void GetAllManyToManyRelationships_IsCaseInsensitiveForDeduplication()
    {
        var relLower = new RelationshipSchema
        {
            Name = "systemuserroles_association",
            IsManyToMany = true
        };
        var relUpper = new RelationshipSchema
        {
            Name = "SYSTEMUSERROLES_ASSOCIATION",
            IsManyToMany = true
        };

        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "systemuser", Relationships = new List<RelationshipSchema> { relLower } },
                new() { LogicalName = "role", Relationships = new List<RelationshipSchema> { relUpper } }
            }
        };

        var m2mRelationships = schema.GetAllManyToManyRelationships().ToList();

        m2mRelationships.Should().HaveCount(1);
    }
}
