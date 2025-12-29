using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using PPDS.Dataverse.Client;

namespace PPDS.Dataverse.Pooling
{
    /// <summary>
    /// Manages a pool of Dataverse connections with intelligent selection and lifecycle management.
    /// Supports multiple connection sources for load distribution across Application Users.
    /// </summary>
    public interface IDataverseConnectionPool : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets a client from the pool asynchronously.
        /// </summary>
        /// <param name="options">Optional per-request options (CallerId, etc.)</param>
        /// <param name="excludeConnectionName">Optional connection name to exclude from selection (useful for retry after throttle).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A pooled client that returns to pool on dispose.</returns>
        /// <exception cref="TimeoutException">Thrown when no connection is available within the timeout period.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the pool is not enabled or has been disposed.</exception>
        Task<IPooledClient> GetClientAsync(
            DataverseClientOptions? options = null,
            string? excludeConnectionName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a client from the pool synchronously.
        /// </summary>
        /// <param name="options">Optional per-request options (CallerId, etc.)</param>
        /// <returns>A pooled client that returns to pool on dispose.</returns>
        /// <exception cref="TimeoutException">Thrown when no connection is available within the timeout period.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the pool is not enabled or has been disposed.</exception>
        IPooledClient GetClient(DataverseClientOptions? options = null);

        /// <summary>
        /// Gets pool statistics and health information.
        /// </summary>
        PoolStatistics Statistics { get; }

        /// <summary>
        /// Gets a value indicating whether the pool is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the number of connection sources configured in the pool.
        /// This represents the number of Application Users/app registrations available.
        /// </summary>
        int SourceCount { get; }

        /// <summary>
        /// Records an authentication failure for statistics.
        /// </summary>
        void RecordAuthFailure();

        /// <summary>
        /// Records a connection failure for statistics.
        /// </summary>
        void RecordConnectionFailure();

        /// <summary>
        /// Invalidates the seed client for a connection, forcing fresh authentication on next use.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this when a token failure is detected (e.g., <c>MessageSecurityException</c> with "Anonymous").
        /// This removes the cached seed client so the next connection request will create a fresh seed
        /// with a new authentication token.
        /// </para>
        /// <para>
        /// This is different from marking individual pooled connections as invalid. When a token expires,
        /// all clones of the seed share the same broken authentication context. Simply disposing
        /// pool members doesn't help - new clones from the same seed will also fail.
        /// Invalidating the seed forces a complete re-authentication.
        /// </para>
        /// </remarks>
        /// <param name="connectionName">The name of the connection source to invalidate.</param>
        void InvalidateSeed(string connectionName);

        /// <summary>
        /// Executes a request with automatic retry on service protection errors.
        /// This is a convenience method that handles connection management and throttle retry internally.
        /// The caller doesn't need to handle service protection exceptions - they are handled transparently.
        /// </summary>
        /// <param name="request">The organization request to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The organization response.</returns>
        /// <remarks>
        /// This method will:
        /// 1. Get a healthy (non-throttled) connection from the pool
        /// 2. Execute the request
        /// 3. If throttled, automatically wait and retry with a different connection
        /// 4. Return the successful response
        /// Service protection errors never escape this method - it retries until success or cancellation.
        /// </remarks>
        Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total recommended parallelism across all connection sources.
        /// This is the sum of RecommendedDegreesOfParallelism for each source, capped at 52 per source.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value comes from the x-ms-dop-hint response header, exposed via
        /// <c>ServiceClient.RecommendedDegreesOfParallelism</c>. This is Microsoft's recommended
        /// concurrent request limit per Application User.
        /// </para>
        /// <para>
        /// Scaling throughput should be done by adding more Application Users (connection sources),
        /// not by exceeding the recommended parallelism per user.
        /// </para>
        /// </remarks>
        /// <returns>The total recommended parallelism across all sources.</returns>
        int GetTotalRecommendedParallelism();
    }
}
