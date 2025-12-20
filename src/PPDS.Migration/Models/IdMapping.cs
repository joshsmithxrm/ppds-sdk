using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PPDS.Migration.Models
{
    /// <summary>
    /// Tracks old-to-new GUID mappings during import.
    /// Thread-safe for concurrent access during parallel import.
    /// </summary>
    public class IdMappingCollection
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Guid>> _mappings = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Adds a mapping from old ID to new ID for an entity.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name.</param>
        /// <param name="oldId">The original record ID.</param>
        /// <param name="newId">The new record ID in the target environment.</param>
        public void AddMapping(string entityLogicalName, Guid oldId, Guid newId)
        {
            var entityMappings = _mappings.GetOrAdd(entityLogicalName, _ => new ConcurrentDictionary<Guid, Guid>());
            entityMappings[oldId] = newId;
        }

        /// <summary>
        /// Tries to get the new ID for an old ID.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name.</param>
        /// <param name="oldId">The original record ID.</param>
        /// <param name="newId">The new record ID if found.</param>
        /// <returns>True if mapping exists, false otherwise.</returns>
        public bool TryGetNewId(string entityLogicalName, Guid oldId, out Guid newId)
        {
            if (_mappings.TryGetValue(entityLogicalName, out var entityMappings))
            {
                return entityMappings.TryGetValue(oldId, out newId);
            }
            newId = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Gets the new ID for an old ID, throwing if not found.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name.</param>
        /// <param name="oldId">The original record ID.</param>
        /// <returns>The new record ID.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when mapping doesn't exist.</exception>
        public Guid GetNewId(string entityLogicalName, Guid oldId)
        {
            if (TryGetNewId(entityLogicalName, oldId, out var newId))
            {
                return newId;
            }
            throw new KeyNotFoundException($"No mapping found for {entityLogicalName} ID {oldId}");
        }

        /// <summary>
        /// Gets the count of mappings for a specific entity.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name.</param>
        /// <returns>The number of mappings.</returns>
        public int GetMappingCount(string entityLogicalName)
        {
            return _mappings.TryGetValue(entityLogicalName, out var entityMappings)
                ? entityMappings.Count
                : 0;
        }

        /// <summary>
        /// Gets the total count of mappings across all entities.
        /// </summary>
        public int TotalMappingCount
        {
            get
            {
                var count = 0;
                foreach (var entityMappings in _mappings.Values)
                {
                    count += entityMappings.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets all mappings for an entity.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name.</param>
        /// <returns>Dictionary of old-to-new ID mappings.</returns>
        public IReadOnlyDictionary<Guid, Guid> GetMappingsForEntity(string entityLogicalName)
        {
            if (_mappings.TryGetValue(entityLogicalName, out var entityMappings))
            {
                return entityMappings;
            }
            return new Dictionary<Guid, Guid>();
        }

        /// <summary>
        /// Gets all entity logical names with mappings.
        /// </summary>
        public IEnumerable<string> GetMappedEntities() => _mappings.Keys;
    }
}
