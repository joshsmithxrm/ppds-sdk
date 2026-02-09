using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Query;
using Xunit;

namespace PPDS.Dataverse.Tests.Query;

[Trait("Category", "PlanUnit")]
public class QueryExecutorGetCountTests
{
    /// <summary>
    /// Builds a <see cref="RetrieveTotalRecordCountResponse"/> with the given entity counts.
    /// Uses the SDK <see cref="EntityRecordCountCollection"/> type so the strongly-typed
    /// property accessor works correctly.
    /// </summary>
    private static RetrieveTotalRecordCountResponse BuildCountResponse(
        params (string entity, long count)[] entries)
    {
        var collection = new EntityRecordCountCollection();
        foreach (var (entity, count) in entries)
        {
            collection.Add(entity, count);
        }

        var response = new RetrieveTotalRecordCountResponse();
        response.Results["EntityRecordCountCollection"] = collection;
        return response;
    }

    /// <summary>
    /// Creates a mock pool that returns a mock client. The client's ExecuteAsync
    /// is set up to return the given response for any OrganizationRequest.
    /// </summary>
    private static (Mock<IDataverseConnectionPool> pool, Mock<IPooledClient> client) CreateMockPool(
        OrganizationResponse response)
    {
        var mockClient = new Mock<IPooledClient>();
        mockClient
            .Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var mockPool = new Mock<IDataverseConnectionPool>();
        mockPool
            .Setup(p => p.GetClientAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockClient.Object);

        return (mockPool, mockClient);
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_ReturnsCount_WhenEntityFound()
    {
        // Arrange
        var response = BuildCountResponse(("account", 42000L));
        var (mockPool, mockClient) = CreateMockPool(response);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        var count = await executor.GetTotalRecordCountAsync("account");

        // Assert
        Assert.Equal(42000L, count);
        mockClient.Verify(
            c => c.ExecuteAsync(
                It.Is<RetrieveTotalRecordCountRequest>(r =>
                    r.EntityNames != null && r.EntityNames.Length == 1 && r.EntityNames[0] == "account"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_ReturnsNull_WhenEntityNotInCollection()
    {
        // Arrange: response has a different entity
        var response = BuildCountResponse(("contact", 100L));
        var (mockPool, _) = CreateMockPool(response);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        var count = await executor.GetTotalRecordCountAsync("account");

        // Assert
        Assert.Null(count);
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_DisposesPooledClient()
    {
        // Arrange
        var response = BuildCountResponse(("account", 1L));
        var (mockPool, mockClient) = CreateMockPool(response);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        await executor.GetTotalRecordCountAsync("account");

        // Assert: client was disposed (returned to pool)
        mockClient.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var response = BuildCountResponse(("lead", 500L));
        var (mockPool, mockClient) = CreateMockPool(response);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        var count = await executor.GetTotalRecordCountAsync("lead", token);

        // Assert
        Assert.Equal(500L, count);
        mockPool.Verify(
            p => p.GetClientAsync(null, null, token),
            Times.Once);
        mockClient.Verify(
            c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), token),
            Times.Once);
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_ThrowsForNullEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert: ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        // (ArgumentNullException derives from ArgumentException)
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => executor.GetTotalRecordCountAsync(null!));
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_ThrowsForEmptyEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => executor.GetTotalRecordCountAsync(""));
    }

    [Fact]
    public async Task GetTotalRecordCountAsync_ThrowsForWhitespaceEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => executor.GetTotalRecordCountAsync("   "));
    }
}

[Trait("Category", "PlanUnit")]
public class QueryExecutorGetMinMaxCreatedOnTests
{
    /// <summary>
    /// Creates a mock pool whose client returns the given EntityCollection
    /// from RetrieveMultipleAsync (used by ExecuteFetchXmlAsync).
    /// </summary>
    private static (Mock<IDataverseConnectionPool> pool, Mock<IPooledClient> client) CreateMockPoolForFetchXml(
        EntityCollection entityCollection)
    {
        var mockClient = new Mock<IPooledClient>();
        mockClient
            .Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityCollection);

        var mockPool = new Mock<IDataverseConnectionPool>();
        mockPool
            .Setup(p => p.GetClientAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockClient.Object);

        return (mockPool, mockClient);
    }

    /// <summary>
    /// Builds an EntityCollection that simulates a MIN/MAX aggregate response.
    /// Dataverse returns AliasedValue objects for aggregate results.
    /// </summary>
    private static EntityCollection BuildMinMaxAggregateResponse(
        string entityLogicalName,
        DateTime? minDate,
        DateTime? maxDate)
    {
        var entity = new Entity(entityLogicalName);

        if (minDate.HasValue)
        {
            entity["mindate"] = new AliasedValue(entityLogicalName, "createdon", minDate.Value);
        }

        if (maxDate.HasValue)
        {
            entity["maxdate"] = new AliasedValue(entityLogicalName, "createdon", maxDate.Value);
        }

        var collection = new EntityCollection(new[] { entity });
        collection.EntityName = entityLogicalName;
        return collection;
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ReturnsCorrectDates_WhenRecordsExist()
    {
        // Arrange
        var minDate = new DateTime(2020, 1, 15, 8, 30, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var entityCollection = BuildMinMaxAggregateResponse("account", minDate, maxDate);
        var (mockPool, _) = CreateMockPoolForFetchXml(entityCollection);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        var (min, max) = await executor.GetMinMaxCreatedOnAsync("account");

        // Assert
        Assert.Equal(minDate, min);
        Assert.Equal(maxDate, max);
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ReturnsNulls_WhenNoRecords()
    {
        // Arrange: empty entity collection (entity has no records)
        var entityCollection = new EntityCollection();
        entityCollection.EntityName = "account";
        var (mockPool, _) = CreateMockPoolForFetchXml(entityCollection);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        var (min, max) = await executor.GetMinMaxCreatedOnAsync("account");

        // Assert
        Assert.Null(min);
        Assert.Null(max);
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ThrowsForNullEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => executor.GetMinMaxCreatedOnAsync(null!));
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ThrowsForEmptyEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => executor.GetMinMaxCreatedOnAsync(""));
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ThrowsForWhitespaceEntityName()
    {
        // Arrange
        var mockPool = new Mock<IDataverseConnectionPool>();
        var executor = new QueryExecutor(mockPool.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => executor.GetMinMaxCreatedOnAsync("   "));
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_ExecutesFetchXmlWithCorrectAggregate()
    {
        // Arrange
        var minDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var entityCollection = BuildMinMaxAggregateResponse("contact", minDate, maxDate);

        FetchExpression? capturedFetch = null;
        var mockClient = new Mock<IPooledClient>();
        mockClient
            .Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
            .Callback<QueryBase, CancellationToken>((q, _) => capturedFetch = q as FetchExpression)
            .ReturnsAsync(entityCollection);

        var mockPool = new Mock<IDataverseConnectionPool>();
        mockPool
            .Setup(p => p.GetClientAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockClient.Object);

        var executor = new QueryExecutor(mockPool.Object);

        // Act
        await executor.GetMinMaxCreatedOnAsync("contact");

        // Assert: verify FetchXML was passed with aggregate, min, and max
        // Note: XDocument.ToString normalizes single quotes to double quotes
        Assert.NotNull(capturedFetch);
        var query = capturedFetch!.Query;
        Assert.Contains("aggregate=\"true\"", query);
        Assert.Contains("name=\"contact\"", query);
        Assert.Contains("aggregate=\"min\"", query);
        Assert.Contains("aggregate=\"max\"", query);
        Assert.Contains("alias=\"mindate\"", query);
        Assert.Contains("alias=\"maxdate\"", query);
    }

    [Fact]
    public async Task GetMinMaxCreatedOnAsync_DisposesPooledClient()
    {
        // Arrange
        var entityCollection = BuildMinMaxAggregateResponse(
            "account",
            new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        var (mockPool, mockClient) = CreateMockPoolForFetchXml(entityCollection);
        var executor = new QueryExecutor(mockPool.Object);

        // Act
        await executor.GetMinMaxCreatedOnAsync("account");

        // Assert: client was disposed (returned to pool)
        mockClient.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
