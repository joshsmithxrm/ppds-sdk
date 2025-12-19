using System;

namespace PPDS.Dataverse.Resilience
{
    /// <summary>
    /// Represents the throttle state for a connection.
    /// </summary>
    public class ThrottleState
    {
        /// <summary>
        /// Gets or sets the connection name.
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the throttle was recorded.
        /// </summary>
        public DateTime ThrottledAt { get; set; }

        /// <summary>
        /// Gets or sets when the throttle expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the retry-after duration.
        /// </summary>
        public TimeSpan RetryAfter { get; set; }

        /// <summary>
        /// Gets a value indicating whether the throttle has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
