using FluentAssertions;
using Moq;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Pooling.Strategies;
using PPDS.Dataverse.Resilience;
using Xunit;

namespace PPDS.Dataverse.Tests.Pooling.Strategies;

public class ThrottleAwareStrategyTests
{
    private readonly ThrottleAwareStrategy _strategy;
    private readonly Mock<IThrottleTracker> _throttleTrackerMock;

    public ThrottleAwareStrategyTests()
    {
        _strategy = new ThrottleAwareStrategy();
        _throttleTrackerMock = new Mock<IThrottleTracker>();
    }

    [Fact]
    public void SelectConnection_SkipsThrottledConnections()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2")
        };
        var activeConnections = new Dictionary<string, int>();

        _throttleTrackerMock.Setup(t => t.IsThrottled("Primary")).Returns(true);
        _throttleTrackerMock.Setup(t => t.IsThrottled("Secondary")).Returns(false);

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert
        result.Should().Be("Secondary");
    }

    [Fact]
    public void SelectConnection_ReturnsFirstWhenAllThrottled()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2")
        };
        var activeConnections = new Dictionary<string, int>();

        _throttleTrackerMock.Setup(t => t.IsThrottled(It.IsAny<string>())).Returns(true);

        // Act
        var result = _strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections);

        // Assert
        result.Should().Be("Primary");
    }

    [Fact]
    public void SelectConnection_UsesRoundRobinAmongAvailable()
    {
        // Arrange
        var connections = new List<DataverseConnection>
        {
            new("Primary", "connection-string-1"),
            new("Secondary", "connection-string-2"),
            new("Tertiary", "connection-string-3")
        };
        var activeConnections = new Dictionary<string, int>();

        // First is throttled, other two are not
        _throttleTrackerMock.Setup(t => t.IsThrottled("Primary")).Returns(true);
        _throttleTrackerMock.Setup(t => t.IsThrottled("Secondary")).Returns(false);
        _throttleTrackerMock.Setup(t => t.IsThrottled("Tertiary")).Returns(false);

        // Act
        var results = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            results.Add(_strategy.SelectConnection(connections, _throttleTrackerMock.Object, activeConnections));
        }

        // Assert - should rotate between Secondary and Tertiary
        results.Should().Contain("Secondary");
        results.Should().Contain("Tertiary");
        results.Should().NotContain("Primary");
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
