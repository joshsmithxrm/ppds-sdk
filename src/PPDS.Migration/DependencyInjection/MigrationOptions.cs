using PPDS.Migration.Export;
using PPDS.Migration.Import;

namespace PPDS.Migration.DependencyInjection
{
    /// <summary>
    /// Options for configuring migration services.
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// Gets or sets the export options.
        /// </summary>
        public ExportOptions Export { get; set; } = new();

        /// <summary>
        /// Gets or sets the import options.
        /// </summary>
        public ImportOptions Import { get; set; } = new();
    }
}
