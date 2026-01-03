using FluentAssertions;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Models;

public class ExecutionPlanTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var plan = new ExecutionPlan();

        plan.Tiers.Should().NotBeNull().And.BeEmpty();
        plan.DeferredFields.Should().NotBeNull().And.BeEmpty();
        plan.ManyToManyRelationships.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TierCount_ReturnsNumberOfTiers()
    {
        var plan = new ExecutionPlan
        {
            Tiers = new List<ImportTier>
            {
                new() { TierNumber = 0, Entities = new List<string> { "account" } },
                new() { TierNumber = 1, Entities = new List<string> { "contact" } },
                new() { TierNumber = 2, Entities = new List<string> { "opportunity" } }
            }
        };

        plan.TierCount.Should().Be(3);
    }

    [Fact]
    public void TierCount_ReturnsZeroWhenNoTiers()
    {
        var plan = new ExecutionPlan();

        plan.TierCount.Should().Be(0);
    }

    [Fact]
    public void DeferredFieldCount_ReturnsTotalAcrossAllEntities()
    {
        var plan = new ExecutionPlan
        {
            DeferredFields = new Dictionary<string, IReadOnlyList<string>>
            {
                { "account", new List<string> { "primarycontactid", "masterid" } },
                { "contact", new List<string> { "parentcustomerid" } }
            }
        };

        plan.DeferredFieldCount.Should().Be(3);
    }

    [Fact]
    public void DeferredFieldCount_ReturnsZeroWhenNoDeferredFields()
    {
        var plan = new ExecutionPlan();

        plan.DeferredFieldCount.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var tiers = new List<ImportTier>
        {
            new() { TierNumber = 0, Entities = new List<string> { "account" } }
        };
        var deferredFields = new Dictionary<string, IReadOnlyList<string>>
        {
            { "account", new List<string> { "primarycontactid" } }
        };
        var m2mRelationships = new List<RelationshipSchema>
        {
            new() { Name = "systemuserroles_association", IsManyToMany = true }
        };

        var plan = new ExecutionPlan
        {
            Tiers = tiers,
            DeferredFields = deferredFields,
            ManyToManyRelationships = m2mRelationships
        };

        plan.Tiers.Should().HaveCount(1);
        plan.DeferredFields.Should().HaveCount(1);
        plan.ManyToManyRelationships.Should().HaveCount(1);
    }
}

public class ImportTierTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var tier = new ImportTier();

        tier.TierNumber.Should().Be(0);
        tier.Entities.Should().NotBeNull().And.BeEmpty();
        tier.HasCircularReferences.Should().BeFalse();
        tier.RequiresWait.Should().BeTrue();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var tier = new ImportTier
        {
            TierNumber = 2,
            Entities = new List<string> { "account", "contact" },
            HasCircularReferences = true,
            RequiresWait = false
        };

        tier.TierNumber.Should().Be(2);
        tier.Entities.Should().HaveCount(2);
        tier.HasCircularReferences.Should().BeTrue();
        tier.RequiresWait.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var tier = new ImportTier
        {
            TierNumber = 1,
            Entities = new List<string> { "contact", "lead" }
        };

        var result = tier.ToString();

        result.Should().Be("Tier 1: [contact, lead]");
    }
}

public class DeferredFieldTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var field = new DeferredField();

        field.EntityLogicalName.Should().BeEmpty();
        field.FieldLogicalName.Should().BeEmpty();
        field.TargetEntity.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var field = new DeferredField
        {
            EntityLogicalName = "account",
            FieldLogicalName = "primarycontactid",
            TargetEntity = "contact"
        };

        field.EntityLogicalName.Should().Be("account");
        field.FieldLogicalName.Should().Be("primarycontactid");
        field.TargetEntity.Should().Be("contact");
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var field = new DeferredField
        {
            EntityLogicalName = "account",
            FieldLogicalName = "primarycontactid",
            TargetEntity = "contact"
        };

        var result = field.ToString();

        result.Should().Be("account.primarycontactid -> contact");
    }
}
