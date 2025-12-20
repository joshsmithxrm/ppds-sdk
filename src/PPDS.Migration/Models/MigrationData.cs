using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace PPDS.Migration.Models
{
    /// <summary>
    /// Container for exported migration data.
    /// </summary>
    public class MigrationData
    {
        /// <summary>
        /// Gets or sets the schema used for this data.
        /// </summary>
        public MigrationSchema Schema { get; set; } = new();

        /// <summary>
        /// Gets or sets the entity data.
        /// Key is entity logical name, value is the list of records.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<Entity>> EntityData { get; set; }
            = new Dictionary<string, IReadOnlyList<Entity>>();

        /// <summary>
        /// Gets or sets the many-to-many relationship data.
        /// Key is relationship name, value is list of associations.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ManyToManyAssociation>> RelationshipData { get; set; }
            = new Dictionary<string, IReadOnlyList<ManyToManyAssociation>>();

        /// <summary>
        /// Gets or sets the export timestamp.
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// Gets or sets the source environment URL.
        /// </summary>
        public string? SourceEnvironment { get; set; }

        /// <summary>
        /// Gets the total record count across all entities.
        /// </summary>
        public int TotalRecordCount
        {
            get
            {
                var count = 0;
                foreach (var records in EntityData.Values)
                {
                    count += records.Count;
                }
                return count;
            }
        }
    }

    /// <summary>
    /// Represents a many-to-many association between two records.
    /// </summary>
    public class ManyToManyAssociation
    {
        /// <summary>
        /// Gets or sets the relationship name.
        /// </summary>
        public string RelationshipName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first entity logical name.
        /// </summary>
        public string Entity1LogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first record ID.
        /// </summary>
        public Guid Entity1Id { get; set; }

        /// <summary>
        /// Gets or sets the second entity logical name.
        /// </summary>
        public string Entity2LogicalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second record ID.
        /// </summary>
        public Guid Entity2Id { get; set; }
    }
}
