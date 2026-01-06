using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PPDS.Dataverse.Configuration;
using PPDS.Dataverse.DependencyInjection;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Services;
using Xunit;

namespace PPDS.Dataverse.Tests.Services;

public class EnvironmentVariableServiceTests
{
    [Fact]
    public void Constructor_ThrowsOnNullConnectionPool()
    {
        // Arrange
        var logger = new NullLogger<EnvironmentVariableService>();

        // Act
        var act = () => new EnvironmentVariableService(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("pool");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Arrange
        var pool = new Mock<IDataverseConnectionPool>().Object;

        // Act
        var act = () => new EnvironmentVariableService(pool, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange
        var pool = new Mock<IDataverseConnectionPool>().Object;
        var logger = new NullLogger<EnvironmentVariableService>();

        // Act
        var service = new EnvironmentVariableService(pool, logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void AddDataverseConnectionPool_RegistersIEnvironmentVariableService()
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
        var envVarService = provider.GetService<IEnvironmentVariableService>();

        // Assert
        envVarService.Should().NotBeNull();
        envVarService.Should().BeOfType<EnvironmentVariableService>();
    }

    [Fact]
    public void AddDataverseConnectionPool_EnvironmentVariableServiceIsTransient()
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
        var service1 = provider.GetService<IEnvironmentVariableService>();
        var service2 = provider.GetService<IEnvironmentVariableService>();

        // Assert
        service1.Should().NotBeSameAs(service2);
    }
}
