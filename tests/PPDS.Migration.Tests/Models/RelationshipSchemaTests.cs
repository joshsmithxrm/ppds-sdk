using FluentAssertions;
using PPDS.Migration.Models;
using Xunit;

namespace PPDS.Migration.Tests.Models;

public class RelationshipSchemaTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var relationship = new RelationshipSchema();

        relationship.Name.Should().BeEmpty();
        relationship.Entity1.Should().BeEmpty();
        relationship.Entity1Attribute.Should().BeEmpty();
        relationship.Entity2.Should().BeEmpty();
        relationship.Entity2Attribute.Should().BeEmpty();
        relationship.IsManyToMany.Should().BeFalse();
        relationship.IntersectEntity.Should().BeNull();
        relationship.IsReflexive.Should().BeFalse();
        relationship.TargetEntityPrimaryKey.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSetForOneToMany()
    {
        var relationship = new RelationshipSchema
        {
            Name = "account_contact",
            Entity1 = "contact",
            Entity1Attribute = "parentcustomerid",
            Entity2 = "account",
            Entity2Attribute = "accountid",
            IsManyToMany = false
        };

        relationship.Name.Should().Be("account_contact");
        relationship.Entity1.Should().Be("contact");
        relationship.Entity1Attribute.Should().Be("parentcustomerid");
        relationship.Entity2.Should().Be("account");
        relationship.Entity2Attribute.Should().Be("accountid");
        relationship.IsManyToMany.Should().BeFalse();
    }

    [Fact]
    public void Properties_CanBeSetForManyToMany()
    {
        var relationship = new RelationshipSchema
        {
            Name = "systemuserroles_association",
            Entity1 = "systemuser",
            Entity2 = "role",
            IsManyToMany = true,
            IntersectEntity = "systemuserroles",
            TargetEntityPrimaryKey = "roleid"
        };

        relationship.Name.Should().Be("systemuserroles_association");
        relationship.Entity1.Should().Be("systemuser");
        relationship.Entity2.Should().Be("role");
        relationship.IsManyToMany.Should().BeTrue();
        relationship.IntersectEntity.Should().Be("systemuserroles");
        relationship.TargetEntityPrimaryKey.Should().Be("roleid");
    }

    [Fact]
    public void IsReflexive_CanBeSet()
    {
        var relationship = new RelationshipSchema
        {
            Name = "account_parent_account",
            Entity1 = "account",
            Entity2 = "account",
            IsReflexive = true
        };

        relationship.IsReflexive.Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsFormattedStringForManyToMany()
    {
        var relationship = new RelationshipSchema
        {
            Name = "systemuserroles_association",
            Entity1 = "systemuser",
            Entity2 = "role",
            IsManyToMany = true
        };

        var result = relationship.ToString();

        result.Should().Be("systemuserroles_association (M2M: systemuser <-> role)");
    }

    [Fact]
    public void ToString_ReturnsFormattedStringForOneToMany()
    {
        var relationship = new RelationshipSchema
        {
            Name = "account_contact",
            Entity1 = "contact",
            Entity2 = "account",
            IsManyToMany = false
        };

        var result = relationship.ToString();

        result.Should().Be("account_contact (contact -> account)");
    }
}
