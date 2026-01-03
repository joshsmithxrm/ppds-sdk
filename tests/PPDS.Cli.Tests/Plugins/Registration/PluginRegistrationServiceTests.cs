using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Moq;
using PPDS.Cli.Plugins.Registration;
using Xunit;

namespace PPDS.Cli.Tests.Plugins.Registration;

public class PluginRegistrationServiceTests
{
    private readonly Mock<IOrganizationService> _mockService;
    private readonly Mock<ILogger<PluginRegistrationService>> _mockLogger;
    private readonly PluginRegistrationService _sut;

    public PluginRegistrationServiceTests()
    {
        _mockService = new Mock<IOrganizationService>();
        _mockLogger = new Mock<ILogger<PluginRegistrationService>>();
        _sut = new PluginRegistrationService(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ListAssembliesAsync_ReturnsEmptyList_WhenNoAssembliesExist()
    {
        // Arrange
        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(new EntityCollection());

        // Act
        var result = await _sut.ListAssembliesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAssembliesAsync_ReturnsAssemblies_WhenTheyExist()
    {
        // Arrange
        var entities = new EntityCollection();
        var assembly = new Entity("pluginassembly", Guid.NewGuid())
        {
            ["name"] = "TestAssembly",
            ["version"] = "1.0.0.0",
            ["publickeytoken"] = "abc123",
            ["isolationmode"] = new OptionSetValue(2)
        };
        entities.Entities.Add(assembly);

        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(entities);

        // Act
        var result = await _sut.ListAssembliesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("TestAssembly", result[0].Name);
        Assert.Equal("1.0.0.0", result[0].Version);
    }

    [Fact]
    public async Task UpsertAssemblyAsync_CreatesNewAssembly_WhenNotExists()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(new EntityCollection());
        _mockService
            .Setup(s => s.Create(It.IsAny<Entity>()))
            .Returns(expectedId);

        // Act
        var result = await _sut.UpsertAssemblyAsync("TestAssembly", new byte[] { 1, 2, 3 });

        // Assert
        Assert.Equal(expectedId, result);
        _mockService.Verify(s => s.Create(It.Is<Entity>(e => e.LogicalName == "pluginassembly")), Times.Once);
    }

    [Fact]
    public async Task UpsertAssemblyAsync_UpdatesExisting_WhenAssemblyExists()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var entities = new EntityCollection();
        entities.Entities.Add(new Entity("pluginassembly", existingId)
        {
            ["name"] = "TestAssembly",
            ["version"] = "1.0.0.0"
        });

        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(entities);

        // Act
        var result = await _sut.UpsertAssemblyAsync("TestAssembly", new byte[] { 1, 2, 3 });

        // Assert
        Assert.Equal(existingId, result);
        _mockService.Verify(s => s.Update(It.Is<Entity>(e => e.Id == existingId)), Times.Once);
        _mockService.Verify(s => s.Create(It.IsAny<Entity>()), Times.Never);
    }

    [Fact]
    public async Task GetSdkMessageIdAsync_ReturnsNull_WhenMessageNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(new EntityCollection());

        // Act
        var result = await _sut.GetSdkMessageIdAsync("NonExistentMessage");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSdkMessageIdAsync_ReturnsId_WhenMessageExists()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var entities = new EntityCollection();
        entities.Entities.Add(new Entity("sdkmessage", messageId));

        _mockService
            .Setup(s => s.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryExpression>()))
            .Returns(entities);

        // Act
        var result = await _sut.GetSdkMessageIdAsync("Create");

        // Assert
        Assert.Equal(messageId, result);
    }
}
