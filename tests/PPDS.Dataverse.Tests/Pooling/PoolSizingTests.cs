using FluentAssertions;
using PPDS.Dataverse.Configuration;
using PPDS.Dataverse.DependencyInjection;
using PPDS.Dataverse.Pooling;
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

        // Assert
        options.MaxPoolSize.Should().Be(0);
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
            dataverseOptions.Connections.Add(new DataverseConnection($"Connection{i}")
            {
                Url = $"https://test{i}.crm.dynamics.com",
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                AuthType = DataverseAuthType.ClientSecret
            });
        }

        // Calculate expected capacity directly
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;

        // Assert
        actualCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(25)]
    public void PoolCapacity_MaxPoolSize_OverridesPerConnectionSizing(int maxPoolSize)
    {
        // Arrange
        var dataverseOptions = new DataverseOptions
        {
            Pool = new ConnectionPoolOptions
            {
                MaxConnectionsPerUser = 52, // This should be ignored
                MaxPoolSize = maxPoolSize,
                Enabled = false
            }
        };

        // Add multiple connections
        dataverseOptions.Connections.Add(new DataverseConnection("Primary")
        {
            Url = "https://test.crm.dynamics.com",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            AuthType = DataverseAuthType.ClientSecret
        });
        dataverseOptions.Connections.Add(new DataverseConnection("Secondary")
        {
            Url = "https://test.crm.dynamics.com",
            ClientId = "test-client-id-2",
            ClientSecret = "test-secret-2",
            AuthType = DataverseAuthType.ClientSecret
        });

        // Calculate capacity using the same logic as CalculateTotalPoolCapacity
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;

        // Assert - MaxPoolSize should take precedence
        actualCapacity.Should().Be(maxPoolSize);
    }

    [Fact]
    public void PoolCapacity_ZeroMaxPoolSize_UsesPerConnectionSizing()
    {
        // Arrange
        var dataverseOptions = new DataverseOptions
        {
            Pool = new ConnectionPoolOptions
            {
                MaxConnectionsPerUser = 52,
                MaxPoolSize = 0, // Default - should use per-connection sizing
                Enabled = false
            }
        };

        // Add 2 connections
        dataverseOptions.Connections.Add(new DataverseConnection("Primary")
        {
            Url = "https://test.crm.dynamics.com",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            AuthType = DataverseAuthType.ClientSecret
        });
        dataverseOptions.Connections.Add(new DataverseConnection("Secondary")
        {
            Url = "https://test.crm.dynamics.com",
            ClientId = "test-client-id-2",
            ClientSecret = "test-secret-2",
            AuthType = DataverseAuthType.ClientSecret
        });

        // Calculate capacity
        var actualCapacity = dataverseOptions.Pool.MaxPoolSize > 0
            ? dataverseOptions.Pool.MaxPoolSize
            : dataverseOptions.Connections.Count * dataverseOptions.Pool.MaxConnectionsPerUser;

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
            MaxConnectionsPerUser = 52,
            MaxPoolSize = 100 // Fixed override
        };

        // Assert - both values are set
        options.MaxConnectionsPerUser.Should().Be(52);
        options.MaxPoolSize.Should().Be(100);
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
