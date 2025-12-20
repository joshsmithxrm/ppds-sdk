using System.Threading;
using System.Threading.Tasks;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Import
{
    /// <summary>
    /// Interface for importing data to Dataverse.
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// Imports data from a CMT-format ZIP file.
        /// </summary>
        /// <param name="dataPath">Path to the data.zip file.</param>
        /// <param name="options">Import options.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The import result.</returns>
        Task<ImportResult> ImportAsync(
            string dataPath,
            ImportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports data using a pre-built execution plan.
        /// </summary>
        /// <param name="data">The migration data.</param>
        /// <param name="plan">The execution plan.</param>
        /// <param name="options">Import options.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The import result.</returns>
        Task<ImportResult> ImportAsync(
            MigrationData data,
            ExecutionPlan plan,
            ImportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default);
    }
}
