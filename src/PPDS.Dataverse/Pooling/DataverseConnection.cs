using System;

namespace PPDS.Dataverse.Pooling
{
    /// <summary>
    /// Configuration for a Dataverse connection source.
    /// Multiple connections can be configured to distribute load across Application Users.
    /// </summary>
    public class DataverseConnection
    {
        /// <summary>
        /// Gets or sets the unique name for this connection.
        /// Used for logging, metrics, and identifying which Application User is handling requests.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Dataverse connection string.
        /// </summary>
        /// <example>
        /// AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx
        /// </example>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the weight for load balancing.
        /// Higher weight means more traffic is routed to this connection.
        /// Default: 1
        /// </summary>
        public int Weight { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum connections to create for this configuration.
        /// Default: 10
        /// </summary>
        public int MaxPoolSize { get; set; } = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnection"/> class.
        /// </summary>
        public DataverseConnection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnection"/> class.
        /// </summary>
        /// <param name="name">The unique name for this connection.</param>
        /// <param name="connectionString">The Dataverse connection string.</param>
        public DataverseConnection(string name, string connectionString)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnection"/> class.
        /// </summary>
        /// <param name="name">The unique name for this connection.</param>
        /// <param name="connectionString">The Dataverse connection string.</param>
        /// <param name="weight">The weight for load balancing.</param>
        /// <param name="maxPoolSize">The maximum connections for this configuration.</param>
        public DataverseConnection(string name, string connectionString, int weight, int maxPoolSize)
            : this(name, connectionString)
        {
            Weight = weight;
            MaxPoolSize = maxPoolSize;
        }
    }
}
