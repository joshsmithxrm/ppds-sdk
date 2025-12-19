using System.Collections.Generic;
using System.Threading;
using PPDS.Dataverse.Resilience;

namespace PPDS.Dataverse.Pooling.Strategies
{
    /// <summary>
    /// Simple round-robin rotation through available connections.
    /// </summary>
    public sealed class RoundRobinStrategy : IConnectionSelectionStrategy
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

            var index = Interlocked.Increment(ref _counter) % connections.Count;
            return connections[index].Name;
        }
    }
}
