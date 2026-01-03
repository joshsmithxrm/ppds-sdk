using FluentAssertions;
using Xunit;
using UserMappingModel = PPDS.Migration.Models.UserMapping;
using UserMappingCollectionModel = PPDS.Migration.Models.UserMappingCollection;

namespace PPDS.Migration.Tests.Models;

public class UserMappingCollectionTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var collection = new UserMappingCollectionModel();

        collection.Mappings.Should().NotBeNull().And.BeEmpty();
        collection.DefaultUserId.Should().BeNull();
        collection.UseCurrentUserAsDefault.Should().BeTrue();
    }

    [Fact]
    public void TryGetMappedUserId_ReturnsExplicitMapping()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var collection = new UserMappingCollectionModel
        {
            Mappings = new Dictionary<Guid, UserMappingModel>
            {
                { sourceId, new UserMappingModel { SourceUserId = sourceId, TargetUserId = targetId } }
            }
        };

        var result = collection.TryGetMappedUserId(sourceId, out var mappedId);

        result.Should().BeTrue();
        mappedId.Should().Be(targetId);
    }

    [Fact]
    public void TryGetMappedUserId_ReturnsDefaultUserId_WhenNoExplicitMapping()
    {
        var sourceId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var collection = new UserMappingCollectionModel
        {
            DefaultUserId = defaultId
        };

        var result = collection.TryGetMappedUserId(sourceId, out var mappedId);

        result.Should().BeTrue();
        mappedId.Should().Be(defaultId);
    }

    [Fact]
    public void TryGetMappedUserId_PrefersExplicitMappingOverDefault()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var collection = new UserMappingCollectionModel
        {
            Mappings = new Dictionary<Guid, UserMappingModel>
            {
                { sourceId, new UserMappingModel { SourceUserId = sourceId, TargetUserId = targetId } }
            },
            DefaultUserId = defaultId
        };

        var result = collection.TryGetMappedUserId(sourceId, out var mappedId);

        result.Should().BeTrue();
        mappedId.Should().Be(targetId);
    }

    [Fact]
    public void TryGetMappedUserId_ReturnsFalse_WhenNoMappingAndNoDefault()
    {
        var sourceId = Guid.NewGuid();
        var collection = new UserMappingCollectionModel
        {
            DefaultUserId = null
        };

        var result = collection.TryGetMappedUserId(sourceId, out var mappedId);

        result.Should().BeFalse();
        mappedId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UseCurrentUserAsDefault_CanBeSet()
    {
        var collection = new UserMappingCollectionModel
        {
            UseCurrentUserAsDefault = false
        };

        collection.UseCurrentUserAsDefault.Should().BeFalse();
    }

    [Fact]
    public void Mappings_CanBePopulated()
    {
        var sourceId1 = Guid.NewGuid();
        var targetId1 = Guid.NewGuid();
        var sourceId2 = Guid.NewGuid();
        var targetId2 = Guid.NewGuid();

        var collection = new UserMappingCollectionModel
        {
            Mappings = new Dictionary<Guid, UserMappingModel>
            {
                { sourceId1, new UserMappingModel { SourceUserId = sourceId1, TargetUserId = targetId1 } },
                { sourceId2, new UserMappingModel { SourceUserId = sourceId2, TargetUserId = targetId2 } }
            }
        };

        collection.Mappings.Should().HaveCount(2);
        collection.Mappings[sourceId1].TargetUserId.Should().Be(targetId1);
        collection.Mappings[sourceId2].TargetUserId.Should().Be(targetId2);
    }
}

public class UserMappingModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var mapping = new UserMappingModel();

        mapping.SourceUserId.Should().Be(Guid.Empty);
        mapping.SourceUserName.Should().BeNull();
        mapping.TargetUserId.Should().Be(Guid.Empty);
        mapping.TargetUserName.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var mapping = new UserMappingModel
        {
            SourceUserId = sourceId,
            SourceUserName = "source@example.com",
            TargetUserId = targetId,
            TargetUserName = "target@example.com"
        };

        mapping.SourceUserId.Should().Be(sourceId);
        mapping.SourceUserName.Should().Be("source@example.com");
        mapping.TargetUserId.Should().Be(targetId);
        mapping.TargetUserName.Should().Be("target@example.com");
    }
}
