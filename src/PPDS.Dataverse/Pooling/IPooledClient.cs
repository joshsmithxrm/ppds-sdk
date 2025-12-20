using System;
using PPDS.Dataverse.Client;

namespace PPDS.Dataverse.Pooling
{
    /// <summary>
    /// A client obtained from the connection pool.
    /// Implements <see cref="IAsyncDisposable"/> and <see cref="IDisposable"/> to return the connection to the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always dispose of the pooled client when done to return it to the pool.
    /// Using 'await using' or 'using' statements is recommended.
    /// </para>
    /// <example>
    /// <code>
    /// await using var client = await pool.GetClientAsync();
    /// var result = await client.RetrieveAsync("account", id, new ColumnSet(true));
    /// </code>
    /// </example>
    /// </remarks>
    public interface IPooledClient : IDataverseClient, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this connection instance.
        /// </summary>
        Guid ConnectionId { get; }

        /// <summary>
        /// Gets the name of the connection configuration this client came from.
        /// Useful for debugging and monitoring which Application User is being used.
        /// </summary>
        string ConnectionName { get; }

        /// <summary>
        /// Gets when this connection was created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets when this connection was last used.
        /// </summary>
        DateTime LastUsedAt { get; }
    }
}
