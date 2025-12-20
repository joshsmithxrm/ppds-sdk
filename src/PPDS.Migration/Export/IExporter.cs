using System.Threading;
using System.Threading.Tasks;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Export
{
    /// <summary>
    /// Interface for exporting data from Dataverse.
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Exports data based on a schema file.
        /// </summary>
        /// <param name="schemaPath">Path to the schema.xml file.</param>
        /// <param name="outputPath">Output ZIP file path.</param>
        /// <param name="options">Export options.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The export result.</returns>
        Task<ExportResult> ExportAsync(
            string schemaPath,
            string outputPath,
            ExportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports data using a pre-parsed schema.
        /// </summary>
        /// <param name="schema">The migration schema.</param>
        /// <param name="outputPath">Output ZIP file path.</param>
        /// <param name="options">Export options.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The export result.</returns>
        Task<ExportResult> ExportAsync(
            MigrationSchema schema,
            string outputPath,
            ExportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default);
    }
}
