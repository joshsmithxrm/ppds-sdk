using System.Collections.Generic;
using System.Linq;
using PPDS.Dataverse.Resilience;

namespace PPDS.Dataverse.Pooling.Strategies
{
    /// <summary>
    /// Selects the connection with the fewest active clients.
    /// </summary>
    public sealed class LeastConnectionsStrategy : IConnectionSelectionStrategy
    {
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

            // Find connection with least active connections
            string? selectedName = null;
            int minConnections = int.MaxValue;

            foreach (var connection in connections)
            {
                var active = activeConnections.TryGetValue(connection.Name, out var count) ? count : 0;

                if (active < minConnections)
                {
                    minConnections = active;
                    selectedName = connection.Name;
                }
            }

            return selectedName ?? connections[0].Name;
        }
    }
}
