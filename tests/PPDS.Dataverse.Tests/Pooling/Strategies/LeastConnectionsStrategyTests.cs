using FluentAssertions;
using Moq;
using PPDS.Dataverse.Configuration;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Pooling.Strategies;
using PPDS.Dataverse.Resilience;
using Xunit;

namespace PPDS.Dataverse.Tests.Pooling.Strategies;

public class LeastConnectionsStrategyTests
{
    private readonly LeastConnectionsStrategy _strategy;
    private readonly Mock<IThrottleTracker> _throttleTrackerMock;

    public LeastConnectionsStrategyTests()
    {
        _strategy = new LeastConnectionsStrategy();
        _throttleTrackerMock = new Mock<IThrottleTracker>();
    }

    [Fact]
    public void SelectConnection_SelectsConnectionWithFewestActive()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            CreateConnection("Primary"),
            CreateConnection("Secondary"),
            CreateConnection("Tertiary")
        };
        var activeConnections = new Dictionary<string, int>
        {
            { "Primary", 10 },
            { "Secondary", 5 },
            { "Tertiary", 15 }
        };

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert
        result.Should().Be("Secondary");
    }

    [Fact]
    public void SelectConnection_SelectsFirstOnTie()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            CreateConnection("Primary"),
            CreateConnection("Secondary")
        };
        var activeConnections = new Dictionary<string, int>
        {
            { "Primary", 5 },
            { "Secondary", 5 }
        };

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert
        result.Should().Be("Primary");
    }

    [Fact]
    public void SelectConnection_HandlesZeroActiveConnections()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            CreateConnection("Primary"),
            CreateConnection("Secondary")
        };
        var activeConnections = new Dictionary<string, int>(); // Empty

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert - should return first since all have 0
        result.Should().Be("Primary");
    }

    [Fact]
    public void SelectConnection_ThrowsWhenNoConnections()
    {
        // Arrange
        var connections = new List<DataverseConnection>();
        var activeConnections = new Dictionary<string, int>();

        // Act & Assert
        var act = () => _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);
        act.Should().Throw<InvalidOperationException>();
    }

    private static DataverseConnection CreateConnection(string name)
    {
        return new DataverseConnection(name)
        {
            Url = $"https://{name.ToLower()}.crm.dynamics.com",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            AuthType = DataverseAuthType.ClientSecret
        };
    }
}
