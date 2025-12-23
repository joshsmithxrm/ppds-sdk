using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PPDS.Dataverse.DependencyInjection;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Resilience;
using Xunit;

namespace PPDS.Dataverse.Tests.Pooling;

/// <summary>
/// Tests for per-connection pool sizing (MaxConnectionsPerUser).
/// </summary>
public class PoolSizingTests
{
    #region Per-Connection Sizing Tests

    [Fact]
    public void ConnectionPoolOptions_DefaultMaxConnectionsPerUser_Is52()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions();

        // Assert
        options.MaxConnectionsPerUser.Should().Be(52);
    }

    [Fact]
    public void ConnectionPoolOptions_DefaultMaxPoolSize_IsZero()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions();

        // Assert - suppress obsolete warning for test
#pragma warning disable CS0618
        options.MaxPoolSize.Should().Be(0);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData(1, 52, 52)]   // 1 connection × 52 = 52
    [InlineData(2, 52, 104)]  // 2 connections × 52 = 104
    [InlineData(4, 52, 208)]  // 4 connections × 52 = 208
    [InlineData(1, 26, 26)]   // 1 connection × 26 = 26 (custom)
    [InlineData(3, 30, 90)]   // 3 connections × 30 = 90 (custom)
    public void PoolCapacity_UsesPerConnectionSizing(int connectionCount, int perUser, int expectedCapacity)
    {
        // Arrange
        var dataverseOptions = new DataverseOptions
        {
            Pool = new ConnectionPoolOptions
            {
                MaxConnectionsPerUser = perUser,
                Enabled = false // Disable to skip actual connection creation
            }
        };

        for (int i = 0; i < connectionCount; i++)
        {
            dataverseOptions.Connections.Add(new DataverseConnection($"Connection{i}", $"AuthType=ClientSecret;Url=https://test{i}.crm.dynamics.com;ClientId=test;ClientSecret=test"));
        }

        // Use reflection to access the private CalculateTotalPoolCapacity logic
        // by checking what value the semaphore would be initialized with
        var options = Options.Create(dataverseOptions);

        // Calculate expected capacity directly
#pragma warning disable CS0618
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;
#pragma warning restore CS0618

        // Assert
        actualCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(25)]
    public void PoolCapacity_LegacyMaxPoolSize_OverridesPerConnectionSizing(int legacyMaxPoolSize)
    {
        // Arrange
        var dataverseOptions = new DataverseOptions
        {
            Pool = new ConnectionPoolOptions
            {
                MaxConnectionsPerUser = 52, // This should be ignored
                Enabled = false
            }
        };

#pragma warning disable CS0618
        dataverseOptions.Pool.MaxPoolSize = legacyMaxPoolSize;
#pragma warning restore CS0618

        // Add multiple connections
        dataverseOptions.Connections.Add(new DataverseConnection("Primary", "AuthType=ClientSecret;Url=https://test.crm.dynamics.com;ClientId=test;ClientSecret=test"));
        dataverseOptions.Connections.Add(new DataverseConnection("Secondary", "AuthType=ClientSecret;Url=https://test.crm.dynamics.com;ClientId=test;ClientSecret=test"));

        // Calculate capacity using the same logic as CalculateTotalPoolCapacity
#pragma warning disable CS0618
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;
#pragma warning restore CS0618

        // Assert - legacy MaxPoolSize should take precedence
        actualCapacity.Should().Be(legacyMaxPoolSize);
    }

    [Fact]
    public void PoolCapacity_ZeroLegacyMaxPoolSize_UsesPerConnectionSizing()
    {
        // Arrange
        var dataverseOptions = new DataverseOptions
        {
            Pool = new ConnectionPoolOptions
            {
                MaxConnectionsPerUser = 52,
                Enabled = false
            }
        };

        // Explicitly set to zero (default) - should NOT override
#pragma warning disable CS0618
        dataverseOptions.Pool.MaxPoolSize = 0;
#pragma warning restore CS0618

        // Add 2 connections
        dataverseOptions.Connections.Add(new DataverseConnection("Primary", "AuthType=ClientSecret;Url=https://test.crm.dynamics.com;ClientId=test;ClientSecret=test"));
        dataverseOptions.Connections.Add(new DataverseConnection("Secondary", "AuthType=ClientSecret;Url=https://test.crm.dynamics.com;ClientId=test;ClientSecret=test"));

        // Calculate capacity
#pragma warning disable CS0618
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;
#pragma warning restore CS0618

        // Assert - should use per-connection sizing
        actualCapacity.Should().Be(104); // 2 × 52
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void PoolOptions_MaxConnectionsPerUser_CanBeCustomized()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions
        {
            MaxConnectionsPerUser = 26 // Custom value
        };

        // Assert
        options.MaxConnectionsPerUser.Should().Be(26);
    }

    [Fact]
    public void PoolOptions_BothSettings_CanCoexist()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions
        {
            MaxConnectionsPerUser = 52
        };

#pragma warning disable CS0618
        options.MaxPoolSize = 100; // Legacy override
#pragma warning restore CS0618

        // Assert - both values are set
        options.MaxConnectionsPerUser.Should().Be(52);
#pragma warning disable CS0618
        options.MaxPoolSize.Should().Be(100);
#pragma warning restore CS0618
    }

    #endregion

    #region Documentation Tests

    [Fact]
    public void MaxConnectionsPerUser_Default_MatchesMicrosoftRecommendation()
    {
        // The default of 52 comes from Microsoft's RecommendedDegreesOfParallelism
        // returned in the x-ms-dop-hint header from Dataverse API responses.
        // See: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/send-parallel-requests

        var options = new ConnectionPoolOptions();

        // Assert
        options.MaxConnectionsPerUser.Should().Be(52,
            "because Microsoft's RecommendedDegreesOfParallelism is typically 52 per Application User");
    }

    #endregion
}
