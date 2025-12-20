using System;
using System.Collections.Generic;

namespace PPDS.Migration.Models
{
    /// <summary>
    /// Entity dependency graph for determining import order.
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// Gets or sets all entity nodes in the graph.
        /// </summary>
        public IReadOnlyList<EntityNode> Entities { get; set; } = Array.Empty<EntityNode>();

        /// <summary>
        /// Gets or sets the dependency edges between entities.
        /// </summary>
        public IReadOnlyList<DependencyEdge> Dependencies { get; set; } = Array.Empty<DependencyEdge>();

        /// <summary>
        /// Gets or sets detected circular references.
        /// </summary>
        public IReadOnlyList<CircularReference> CircularReferences { get; set; } = Array.Empty<CircularReference>();

        /// <summary>
        /// Gets or sets the topologically sorted tiers.
        /// Entities within the same tier can be processed in parallel.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Tiers { get; set; } = Array.Empty<IReadOnlyList<string>>();

        /// <summary>
        /// Gets the total number of tiers.
        /// </summary>
        public int TierCount => Tiers.Count;

        /// <summary>
        /// Gets whether the graph contains circular references.
        /// </summary>
        public bool HasCircularReferences => CircularReferences.Count > 0;
    }

    /// <summary>
    /// Represents an entity node in the dependency graph.
    /// </summary>
    public class EntityNode
    {
        /// <summary>
        /// Gets or sets the entity logical name.
        /// </summary>
        public string LogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the record count (populated during export).
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// Gets or sets the tier number this entity is assigned to.
        /// </summary>
        public int TierNumber { get; set; }

        /// <inheritdoc />
        public override string ToString() => LogicalName;
    }

    /// <summary>
    /// Represents a dependency edge between entities.
    /// </summary>
    public class DependencyEdge
    {
        /// <summary>
        /// Gets or sets the source entity (the entity with the lookup field).
        /// </summary>
        public string FromEntity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target entity (the entity being referenced).
        /// </summary>
        public string ToEntity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the field name creating this dependency.
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of dependency.
        /// </summary>
        public DependencyType Type { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{FromEntity}.{FieldName} -> {ToEntity}";
    }

    /// <summary>
    /// Type of dependency between entities.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>Standard lookup field.</summary>
        Lookup,

        /// <summary>Owner field (systemuser or team).</summary>
        Owner,

        /// <summary>Customer field (account or contact).</summary>
        Customer,

        /// <summary>Parent-child relationship.</summary>
        ParentChild
    }

    /// <summary>
    /// Represents a circular reference between entities.
    /// </summary>
    public class CircularReference
    {
        /// <summary>
        /// Gets or sets the entities involved in the circular reference.
        /// </summary>
        public IReadOnlyList<string> Entities { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the edges forming the cycle.
        /// </summary>
        public IReadOnlyList<DependencyEdge> Edges { get; set; } = Array.Empty<DependencyEdge>();

        /// <inheritdoc />
        public override string ToString() => $"[{string.Join(" -> ", Entities)}]";
    }
}
