using FluentAssertions;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Models;

public class DependencyGraphTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var graph = new DependencyGraph();

        graph.Entities.Should().NotBeNull().And.BeEmpty();
        graph.Dependencies.Should().NotBeNull().And.BeEmpty();
        graph.CircularReferences.Should().NotBeNull().And.BeEmpty();
        graph.Tiers.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TierCount_ReturnsNumberOfTiers()
    {
        var graph = new DependencyGraph
        {
            Tiers = new List<IReadOnlyList<string>>
            {
                new List<string> { "account" },
                new List<string> { "contact", "lead" },
                new List<string> { "opportunity" }
            }
        };

        graph.TierCount.Should().Be(3);
    }

    [Fact]
    public void TierCount_ReturnsZeroWhenNoTiers()
    {
        var graph = new DependencyGraph();

        graph.TierCount.Should().Be(0);
    }

    [Fact]
    public void HasCircularReferences_ReturnsTrueWhenCircularReferencesExist()
    {
        var graph = new DependencyGraph
        {
            CircularReferences = new List<CircularReference>
            {
                new() { Entities = new List<string> { "account", "contact" } }
            }
        };

        graph.HasCircularReferences.Should().BeTrue();
    }

    [Fact]
    public void HasCircularReferences_ReturnsFalseWhenNoCircularReferences()
    {
        var graph = new DependencyGraph
        {
            CircularReferences = new List<CircularReference>()
        };

        graph.HasCircularReferences.Should().BeFalse();
    }

    [Fact]
    public void Entities_CanBeSet()
    {
        var entities = new List<EntityNode>
        {
            new() { LogicalName = "account", DisplayName = "Account", TierNumber = 0 },
            new() { LogicalName = "contact", DisplayName = "Contact", TierNumber = 1 }
        };

        var graph = new DependencyGraph { Entities = entities };

        graph.Entities.Should().HaveCount(2);
        graph.Entities.Should().Contain(e => e.LogicalName == "account");
    }

    [Fact]
    public void Dependencies_CanBeSet()
    {
        var dependencies = new List<DependencyEdge>
        {
            new() { FromEntity = "contact", ToEntity = "account", FieldName = "parentcustomerid", Type = DependencyType.Lookup }
        };

        var graph = new DependencyGraph { Dependencies = dependencies };

        graph.Dependencies.Should().HaveCount(1);
        graph.Dependencies[0].FromEntity.Should().Be("contact");
        graph.Dependencies[0].ToEntity.Should().Be("account");
    }
}

public class EntityNodeTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var node = new EntityNode();

        node.LogicalName.Should().BeEmpty();
        node.DisplayName.Should().BeEmpty();
        node.RecordCount.Should().Be(0);
        node.TierNumber.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var node = new EntityNode
        {
            LogicalName = "account",
            DisplayName = "Account",
            RecordCount = 100,
            TierNumber = 2
        };

        node.LogicalName.Should().Be("account");
        node.DisplayName.Should().Be("Account");
        node.RecordCount.Should().Be(100);
        node.TierNumber.Should().Be(2);
    }

    [Fact]
    public void ToString_ReturnsLogicalName()
    {
        var node = new EntityNode { LogicalName = "account" };

        var result = node.ToString();

        result.Should().Be("account");
    }
}

public class DependencyEdgeTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var edge = new DependencyEdge();

        edge.FromEntity.Should().BeEmpty();
        edge.ToEntity.Should().BeEmpty();
        edge.FieldName.Should().BeEmpty();
        edge.Type.Should().Be(DependencyType.Lookup);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var edge = new DependencyEdge
        {
            FromEntity = "contact",
            ToEntity = "account",
            FieldName = "parentcustomerid",
            Type = DependencyType.Customer
        };

        edge.FromEntity.Should().Be("contact");
        edge.ToEntity.Should().Be("account");
        edge.FieldName.Should().Be("parentcustomerid");
        edge.Type.Should().Be(DependencyType.Customer);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var edge = new DependencyEdge
        {
            FromEntity = "contact",
            ToEntity = "account",
            FieldName = "parentcustomerid"
        };

        var result = edge.ToString();

        result.Should().Be("contact.parentcustomerid -> account");
    }
}

public class CircularReferenceTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var circularRef = new CircularReference();

        circularRef.Entities.Should().NotBeNull().And.BeEmpty();
        circularRef.Edges.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var entities = new List<string> { "account", "contact" };
        var edges = new List<DependencyEdge>
        {
            new() { FromEntity = "account", ToEntity = "contact", FieldName = "primarycontactid" },
            new() { FromEntity = "contact", ToEntity = "account", FieldName = "parentcustomerid" }
        };

        var circularRef = new CircularReference
        {
            Entities = entities,
            Edges = edges
        };

        circularRef.Entities.Should().HaveCount(2);
        circularRef.Edges.Should().HaveCount(2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var circularRef = new CircularReference
        {
            Entities = new List<string> { "account", "contact", "account" }
        };

        var result = circularRef.ToString();

        result.Should().Be("[account -> contact -> account]");
    }
}

public class DependencyTypeTests
{
    [Fact]
    public void DependencyType_HasExpectedValues()
    {
        var lookup = DependencyType.Lookup;
        var owner = DependencyType.Owner;
        var customer = DependencyType.Customer;
        var parentChild = DependencyType.ParentChild;

        lookup.Should().Be(DependencyType.Lookup);
        owner.Should().Be(DependencyType.Owner);
        customer.Should().Be(DependencyType.Customer);
        parentChild.Should().Be(DependencyType.ParentChild);
    }
}
