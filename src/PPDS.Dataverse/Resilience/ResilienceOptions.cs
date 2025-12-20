using System;

namespace PPDS.Dataverse.Resilience
{
    /// <summary>
    /// Configuration options for resilience and retry behavior.
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether throttle tracking is enabled.
        /// Default: true
        /// </summary>
        public bool EnableThrottleTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets the default cooldown period when throttled (if not specified by server).
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan DefaultThrottleCooldown { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the maximum retry attempts for transient failures.
        /// Default: 3
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retries.
        /// Default: 1 second
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets a value indicating whether to use exponential backoff for retries.
        /// Default: true
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// Default: 30 seconds
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    }
}
