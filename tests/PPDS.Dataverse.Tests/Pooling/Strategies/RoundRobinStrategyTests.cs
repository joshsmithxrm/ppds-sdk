using FluentAssertions;
using Moq;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Pooling.Strategies;
using PPDS.Dataverse.Resilience;
using Xunit;

namespace PPDS.Dataverse.Tests.Pooling.Strategies;

public class RoundRobinStrategyTests
{
    private readonly RoundRobinStrategy _strategy;
    private readonly Mock<IThrottleTracker> _throttleTrackerMock;

    public RoundRobinStrategyTests()
    {
        _strategy = new RoundRobinStrategy();
        _throttleTrackerMock = new Mock<IThrottleTracker>();
    }

    [Fact]
    public void SelectConnection_ThrowsWhenNoConnections()
    {
        // Arrange
        var connections = new List<DataverseConnection>();
        var activeConnections = new Dictionary<string, int>();

        // Act & Assert
        var act = () => _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No connections available.");
    }

    [Fact]
    public void SelectConnection_ReturnsSingleConnection()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string")
        };
        var activeConnections = new Dictionary<string, int>();

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert
        result.Should().Be("Primary");
    }

    [Fact]
    public void SelectConnection_RotatesThroughConnections()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2"),
            new("Tertiary", "connection-string-3")
        };
        var activeConnections = new Dictionary<string, int>();

        // Act
        var results = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            results.Add(_strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections));
        }

        // Assert - should see each connection at least once
        results.Should().Contain("Primary");
        results.Should().Contain("Secondary");
        results.Should().Contain("Tertiary");
    }

    [Fact]
    public void SelectConnection_IsThreadSafe()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2")
        };
        var activeConnections = new Dictionary<string, int>();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);
            results.Add(result);
        });

        // Assert - should have selected from both connections
        results.Should().Contain("Primary");
        results.Should().Contain("Secondary");
    }
}
