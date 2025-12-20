using System;
using PPDS.Dataverse.Security;

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
        /// <remarks>
        /// This property contains sensitive credentials and should never be logged directly.
        /// Use <see cref="ConnectionStringRedactor.Redact"/> if you need to include
        /// connection string information in logs or error messages.
        /// </remarks>
        /// <example>
        /// AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx
        /// </example>
        [SensitiveData(Reason = "Contains authentication credentials", DataType = "ConnectionString")]
        public string ConnectionString { get; set; } = string.Empty;

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
        /// <param name="maxPoolSize">The maximum connections for this configuration.</param>
        public DataverseConnection(string name, string connectionString, int maxPoolSize)
            : this(name, connectionString)
        {
            MaxPoolSize = maxPoolSize;
        }

        /// <summary>
        /// Returns a string representation of the connection configuration.
        /// The connection string is intentionally excluded to prevent credential leakage.
        /// </summary>
        /// <returns>A string containing the connection name and pool size.</returns>
        public override string ToString()
        {
            return $"DataverseConnection {{ Name = {Name}, MaxPoolSize = {MaxPoolSize} }}";
        }

        /// <summary>
        /// Gets a redacted version of the connection string safe for logging.
        /// </summary>
        /// <returns>The connection string with sensitive values replaced.</returns>
        public string GetRedactedConnectionString()
        {
            return ConnectionStringRedactor.Redact(ConnectionString);
        }
    }
}
