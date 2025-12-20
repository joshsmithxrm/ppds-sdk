using FluentAssertions;
using Moq;
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
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2"),
            new("Tertiary", "connection-string-3")
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
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2")
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
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2")
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
}
