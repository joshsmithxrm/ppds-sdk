using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace PPDS.Dataverse.Resilience
{
    /// <summary>
    /// Tracks throttle state for connections using a thread-safe concurrent dictionary.
    /// </summary>
    public sealed class ThrottleTracker : IThrottleTracker
    {
        private readonly ConcurrentDictionary<string, ThrottleState> _throttleStates;
        private readonly ILogger<ThrottleTracker> _logger;
        private long _totalThrottleEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottleTracker"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public ThrottleTracker(ILogger<ThrottleTracker> logger)
        {
            _throttleStates = new ConcurrentDictionary<string, ThrottleState>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public long TotalThrottleEvents => _totalThrottleEvents;

        /// <inheritdoc />
        public void RecordThrottle(string connectionName, TimeSpan retryAfter)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                throw new ArgumentNullException(nameof(connectionName));
            }

            var now = DateTime.UtcNow;
            var state = new ThrottleState
            {
                ConnectionName = connectionName,
                ThrottledAt = now,
                ExpiresAt = now + retryAfter,
                RetryAfter = retryAfter
            };

            _throttleStates.AddOrUpdate(connectionName, state, (_, __) => state);
            Interlocked.Increment(ref _totalThrottleEvents);

            _logger.LogWarning(
                "Connection throttled. Name: {ConnectionName}, RetryAfter: {RetryAfter}, ExpiresAt: {ExpiresAt}",
                connectionName,
                retryAfter,
                state.ExpiresAt);
        }

        /// <inheritdoc />
        public bool IsThrottled(string connectionName)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                return false;
            }

            if (!_throttleStates.TryGetValue(connectionName, out var state))
            {
                return false;
            }

            if (state.IsExpired)
            {
                // Clean up expired throttle
                _throttleStates.TryRemove(connectionName, out _);
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public DateTime? GetThrottleExpiry(string connectionName)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                return null;
            }

            if (!_throttleStates.TryGetValue(connectionName, out var state))
            {
                return null;
            }

            if (state.IsExpired)
            {
                _throttleStates.TryRemove(connectionName, out _);
                return null;
            }

            return state.ExpiresAt;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAvailableConnections()
        {
            // Clean up expired entries while iterating
            var expired = new List<string>();

            foreach (var kvp in _throttleStates)
            {
                if (kvp.Value.IsExpired)
                {
                    expired.Add(kvp.Key);
                }
            }

            foreach (var key in expired)
            {
                _throttleStates.TryRemove(key, out _);
            }

            // Return connections that are not in the throttle dictionary
            // (This method is typically called with a list of all connections,
            // so the caller filters based on this)
            return _throttleStates.Keys.Where(k => !IsThrottled(k));
        }

        /// <inheritdoc />
        public void ClearThrottle(string connectionName)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                return;
            }

            if (_throttleStates.TryRemove(connectionName, out _))
            {
                _logger.LogInformation("Cleared throttle for connection: {ConnectionName}", connectionName);
            }
        }
    }
}
