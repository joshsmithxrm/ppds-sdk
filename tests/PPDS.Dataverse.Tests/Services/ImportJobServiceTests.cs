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

public class ImportJobServiceTests
{
    [Fact]
    public void Constructor_ThrowsOnNullConnectionPool()
    {
        // Arrange
        var logger = new NullLogger<ImportJobService>();

        // Act
        var act = () => new ImportJobService(null!, logger);

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
        var act = () => new ImportJobService(pool, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange
        var pool = new Mock<IDataverseConnectionPool>().Object;
        var logger = new NullLogger<ImportJobService>();

        // Act
        var service = new ImportJobService(pool, logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void AddDataverseConnectionPool_RegistersIImportJobService()
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
        var importJobService = provider.GetService<IImportJobService>();

        // Assert
        importJobService.Should().NotBeNull();
        importJobService.Should().BeOfType<ImportJobService>();
    }

    [Fact]
    public void AddDataverseConnectionPool_ImportJobServiceIsTransient()
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
        var service1 = provider.GetService<IImportJobService>();
        var service2 = provider.GetService<IImportJobService>();

        // Assert
        service1.Should().NotBeSameAs(service2);
    }
}
