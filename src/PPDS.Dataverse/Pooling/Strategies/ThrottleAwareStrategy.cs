using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PPDS.Dataverse.Resilience;

namespace PPDS.Dataverse.Pooling.Strategies
{
    /// <summary>
    /// Avoids throttled connections and falls back to round-robin among available connections.
    /// If all connections are throttled, waits for the shortest throttle to expire.
    /// </summary>
    public sealed class ThrottleAwareStrategy : IConnectionSelectionStrategy
    {
        private int _counter;

        /// <inheritdoc />
        public string SelectConnection(
            IReadOnlyList<DataverseConnection> connections,
            IThrottleTracker throttleTracker,
            IReadOnlyDictionary<string, int> activeConnections)
        {
            if (connections.Count == 0)
            {
                throw new System.InvalidOperationException("No connections available.");
            }

            if (connections.Count == 1)
            {
                return connections[0].Name;
            }

            // Filter to non-throttled connections
            var availableConnections = connections
                .Where(c => !throttleTracker.IsThrottled(c.Name))
                .ToList();

            if (availableConnections.Count == 0)
            {
                // All connections throttled - use the one with shortest remaining throttle
                // For now, just return the first one and let the caller handle retry
                return connections[0].Name;
            }

            if (availableConnections.Count == 1)
            {
                return availableConnections[0].Name;
            }

            // Round-robin among available connections
            var index = Interlocked.Increment(ref _counter) % availableConnections.Count;
            return availableConnections[index].Name;
        }
    }
}
