using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PPDS.Dataverse.BulkOperations;
using PPDS.Dataverse.Configuration;
using PPDS.Dataverse.DependencyInjection;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Resilience;
using Xunit;

namespace PPDS.Dataverse.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDataverseConnectionPool_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddDataverseConnectionPool(options =>
        {
            options.Connections.Add(new DataverseConnection("Primary")
            {
                Url = "https://test.crm.dynamics.com",
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                AuthType = DataverseAuthType.ClientSecret
            });
        });

        // Assert
        var provider = services.BuildServiceProvider();

        provider.GetService<IThrottleTracker>().Should().NotBeNull();
        provider.GetService<IDataverseConnectionPool>().Should().NotBeNull();
        provider.GetService<IBulkOperationExecutor>().Should().NotBeNull();
    }

    [Fact]
    public void AddDataverseConnectionPool_ThrottleTrackerIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataverseConnectionPool(options =>
        {
            options.Connections.Add(new DataverseConnection("Primary")
            {
                Url = "https://test.crm.dynamics.com",
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                AuthType = DataverseAuthType.ClientSecret
            });
        });

        // Act
        var provider = services.BuildServiceProvider();
        var tracker1 = provider.GetService<IThrottleTracker>();
        var tracker2 = provider.GetService<IThrottleTracker>();

        // Assert
        tracker1.Should().BeSameAs(tracker2);
    }

    [Fact]
    public void AddDataverseConnectionPool_ConnectionPoolIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataverseConnectionPool(options =>
        {
            options.Connections.Add(new DataverseConnection("Primary")
            {
                Url = "https://test.crm.dynamics.com",
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                AuthType = DataverseAuthType.ClientSecret
            });
        });

        // Act
        var provider = services.BuildServiceProvider();
        var pool1 = provider.GetService<IDataverseConnectionPool>();
        var pool2 = provider.GetService<IDataverseConnectionPool>();

        // Assert
        pool1.Should().BeSameAs(pool2);
    }

    [Fact]
    public void AddDataverseConnectionPool_BulkExecutorIsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataverseConnectionPool(options =>
        {
            options.Connections.Add(new DataverseConnection("Primary")
            {
                Url = "https://test.crm.dynamics.com",
                ClientId = "test-client-id",
                ClientSecret = "test-secret",
                AuthType = DataverseAuthType.ClientSecret
            });
        });

        // Act
        var provider = services.BuildServiceProvider();
        var executor1 = provider.GetService<IBulkOperationExecutor>();
        var executor2 = provider.GetService<IBulkOperationExecutor>();

        // Assert
        executor1.Should().NotBeSameAs(executor2);
    }

    [Fact]
    public void AddDataverseConnectionPool_ThrowsOnNullServices()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddDataverseConnectionPool(null!, _ => { });
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDataverseConnectionPool_ThrowsOnNullConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddDataverseConnectionPool((Action<DataverseOptions>)null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
