using System;
using System.Collections.Generic;

namespace PPDS.Migration.Models
{
    /// <summary>
    /// Execution plan for importing data with dependency resolution.
    /// </summary>
    public class ExecutionPlan
    {
        /// <summary>
        /// Gets or sets the ordered tiers for import.
        /// </summary>
        public IReadOnlyList<ImportTier> Tiers { get; set; } = Array.Empty<ImportTier>();

        /// <summary>
        /// Gets or sets fields that must be deferred (set to null initially, updated after all records exist).
        /// Key is entity logical name, value is list of field names to defer.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> DeferredFields { get; set; }
            = new Dictionary<string, IReadOnlyList<string>>();

        /// <summary>
        /// Gets or sets many-to-many relationships to process after entity import.
        /// </summary>
        public IReadOnlyList<RelationshipSchema> ManyToManyRelationships { get; set; }
            = Array.Empty<RelationshipSchema>();

        /// <summary>
        /// Gets the total number of tiers.
        /// </summary>
        public int TierCount => Tiers.Count;

        /// <summary>
        /// Gets the total number of deferred fields across all entities.
        /// </summary>
        public int DeferredFieldCount
        {
            get
            {
                var count = 0;
                foreach (var fields in DeferredFields.Values)
                {
                    count += fields.Count;
                }
                return count;
            }
        }
    }

    /// <summary>
    /// Represents a tier of entities that can be imported in parallel.
    /// </summary>
    public class ImportTier
    {
        /// <summary>
        /// Gets or sets the tier number (0 = first).
        /// </summary>
        public int TierNumber { get; set; }

        /// <summary>
        /// Gets or sets the entities in this tier.
        /// </summary>
        public IReadOnlyList<string> Entities { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets whether this tier contains circular references.
        /// </summary>
        public bool HasCircularReferences { get; set; }

        /// <summary>
        /// Gets or sets whether to wait for this tier to complete before starting next.
        /// </summary>
        public bool RequiresWait { get; set; } = true;

        /// <inheritdoc />
        public override string ToString() => $"Tier {TierNumber}: [{string.Join(", ", Entities)}]";
    }

    /// <summary>
    /// Represents a field that must be deferred during initial import.
    /// </summary>
    public class DeferredField
    {
        /// <summary>
        /// Gets or sets the entity containing the deferred field.
        /// </summary>
        public string EntityLogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the field logical name.
        /// </summary>
        public string FieldLogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target entity for the lookup.
        /// </summary>
        public string TargetEntity { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString() => $"{EntityLogicalName}.{FieldLogicalName} -> {TargetEntity}";
    }
}
