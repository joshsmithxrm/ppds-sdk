namespace PPDS.Migration.Schema
{
    /// <summary>
    /// Options for schema generation.
    /// </summary>
    public class SchemaGeneratorOptions
    {
        /// <summary>
        /// Gets or sets whether to include all fields. Default: true.
        /// </summary>
        public bool IncludeAllFields { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include system fields (createdon, modifiedon, etc.). Default: false.
        /// </summary>
        public bool IncludeSystemFields { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include relationships. Default: true.
        /// </summary>
        public bool IncludeRelationships { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include only custom fields. Default: false.
        /// </summary>
        public bool CustomFieldsOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the default value for disabling plugins during import. Default: false.
        /// </summary>
        public bool DisablePluginsByDefault { get; set; } = false;
    }
}
