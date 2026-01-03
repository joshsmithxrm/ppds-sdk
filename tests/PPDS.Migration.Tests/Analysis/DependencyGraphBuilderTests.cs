using FluentAssertions;
using PPDS.Migration.Analysis;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Analysis;

public class DependencyGraphBuilderTests
{
    [Fact]
    public void Build_ThrowsWhenSchemaIsNull()
    {
        var builder = new DependencyGraphBuilder();

        var act = () => builder.Build(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_CreatesEntityNodes()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "account", DisplayName = "Account" },
                new() { LogicalName = "contact", DisplayName = "Contact" }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Entities.Should().HaveCount(2);
        graph.Entities.Should().Contain(e => e.LogicalName == "account");
        graph.Entities.Should().Contain(e => e.LogicalName == "contact");
    }

    [Fact]
    public void Build_CreatesDependencyEdgesFromLookupFields()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    Fields = new List<FieldSchema>()
                },
                new()
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    Fields = new List<FieldSchema>
                    {
                        new()
                        {
                            LogicalName = "parentcustomerid",
                            Type = "lookup",
                            LookupEntity = "account"
                        }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Dependencies.Should().HaveCount(1);
        graph.Dependencies[0].FromEntity.Should().Be("contact");
        graph.Dependencies[0].ToEntity.Should().Be("account");
        graph.Dependencies[0].FieldName.Should().Be("parentcustomerid");
        graph.Dependencies[0].Type.Should().Be(DependencyType.Lookup);
    }

    [Fact]
    public void Build_HandlesPolymorphicLookups()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "account", DisplayName = "Account", Fields = new List<FieldSchema>() },
                new() { LogicalName = "contact", DisplayName = "Contact", Fields = new List<FieldSchema>() },
                new()
                {
                    LogicalName = "opportunity",
                    DisplayName = "Opportunity",
                    Fields = new List<FieldSchema>
                    {
                        new()
                        {
                            LogicalName = "customerid",
                            Type = "customer",
                            LookupEntity = "account|contact"
                        }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Dependencies.Should().HaveCount(2);
        graph.Dependencies.Should().Contain(d => d.FromEntity == "opportunity" && d.ToEntity == "account" && d.Type == DependencyType.Customer);
        graph.Dependencies.Should().Contain(d => d.FromEntity == "opportunity" && d.ToEntity == "contact" && d.Type == DependencyType.Customer);
    }

    [Fact]
    public void Build_IgnoresLookupsToEntitiesNotInSchema()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    Fields = new List<FieldSchema>
                    {
                        new()
                        {
                            LogicalName = "parentcustomerid",
                            Type = "lookup",
                            LookupEntity = "account" // account not in schema
                        }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void Build_DetectsCircularReferences()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "primarycontactid", Type = "lookup", LookupEntity = "contact" }
                    }
                },
                new()
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "parentcustomerid", Type = "lookup", LookupEntity = "account" }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.HasCircularReferences.Should().BeTrue();
        graph.CircularReferences.Should().HaveCount(1);
        graph.CircularReferences[0].Entities.Should().Contain("account");
        graph.CircularReferences[0].Entities.Should().Contain("contact");
    }

    [Fact]
    public void Build_BuildsTiers()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    Fields = new List<FieldSchema>()
                },
                new()
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "parentcustomerid", Type = "lookup", LookupEntity = "account" }
                    }
                },
                new()
                {
                    LogicalName = "opportunity",
                    DisplayName = "Opportunity",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "customerid", Type = "lookup", LookupEntity = "contact" }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Tiers.Should().HaveCount(3);
        graph.Tiers[0].Should().Contain("account");
        graph.Tiers[1].Should().Contain("contact");
        graph.Tiers[2].Should().Contain("opportunity");
    }

    [Fact]
    public void Build_AssignsTierNumbersToNodes()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new()
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    Fields = new List<FieldSchema>()
                },
                new()
                {
                    LogicalName = "contact",
                    DisplayName = "Contact",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "parentcustomerid", Type = "lookup", LookupEntity = "account" }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        var accountNode = graph.Entities.First(e => e.LogicalName == "account");
        var contactNode = graph.Entities.First(e => e.LogicalName == "contact");

        accountNode.TierNumber.Should().Be(0);
        contactNode.TierNumber.Should().Be(1);
    }

    [Fact]
    public void Build_HandlesOwnerDependencies()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>
            {
                new() { LogicalName = "systemuser", DisplayName = "User", Fields = new List<FieldSchema>() },
                new()
                {
                    LogicalName = "account",
                    DisplayName = "Account",
                    Fields = new List<FieldSchema>
                    {
                        new() { LogicalName = "ownerid", Type = "owner", LookupEntity = "systemuser" }
                    }
                }
            }
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Dependencies.Should().HaveCount(1);
        graph.Dependencies[0].Type.Should().Be(DependencyType.Owner);
    }

    [Fact]
    public void Build_HandlesEmptySchema()
    {
        var schema = new MigrationSchema
        {
            Entities = new List<EntitySchema>()
        };

        var builder = new DependencyGraphBuilder();

        var graph = builder.Build(schema);

        graph.Entities.Should().BeEmpty();
        graph.Dependencies.Should().BeEmpty();
        graph.CircularReferences.Should().BeEmpty();
        graph.Tiers.Should().BeEmpty();
    }
}
