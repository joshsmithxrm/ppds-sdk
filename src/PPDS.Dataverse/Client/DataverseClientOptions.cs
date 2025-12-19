using System;

namespace PPDS.Dataverse.Client
{
    /// <summary>
    /// Options for configuring a Dataverse client request.
    /// Used to customize behavior when acquiring a client from the pool.
    /// </summary>
    public class DataverseClientOptions
    {
        /// <summary>
        /// Gets or sets the caller ID for impersonation.
        /// When set, operations will be performed on behalf of this user.
        /// </summary>
        public Guid? CallerId { get; set; }

        /// <summary>
        /// Gets or sets the caller AAD object ID for impersonation.
        /// Alternative to CallerId for AAD-based impersonation.
        /// </summary>
        public Guid? CallerAADObjectId { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient failures.
        /// When null, uses the default configured on the pool.
        /// </summary>
        public int? MaxRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the pause time between retry attempts.
        /// When null, uses the default configured on the pool.
        /// </summary>
        public TimeSpan? RetryPauseTime { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DataverseClientOptions"/> with default values.
        /// </summary>
        public DataverseClientOptions()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DataverseClientOptions"/> for impersonation.
        /// </summary>
        /// <param name="callerId">The caller ID for impersonation.</param>
        public DataverseClientOptions(Guid callerId)
        {
            CallerId = callerId;
        }
    }
}
