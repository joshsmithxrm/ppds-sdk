using System.Collections.Generic;
using PPDS.Dataverse.Resilience;

namespace PPDS.Dataverse.Pooling.Strategies
{
    /// <summary>
    /// Strategy for selecting which connection to use from the pool.
    /// </summary>
    public interface IConnectionSelectionStrategy
    {
        /// <summary>
        /// Selects a connection based on the strategy's criteria.
        /// </summary>
        /// <param name="connections">Available connection configurations.</param>
        /// <param name="throttleTracker">Throttle tracker for checking throttle state.</param>
        /// <param name="activeConnections">Number of active connections per configuration name.</param>
        /// <returns>The name of the selected connection.</returns>
        string SelectConnection(
            IReadOnlyList<DataverseConnection> connections,
            IThrottleTracker throttleTracker,
            IReadOnlyDictionary<string, int> activeConnections);
    }
}
