using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Formats
{
    /// <summary>
    /// Interface for reading CMT data files.
    /// </summary>
    public interface ICmtDataReader
    {
        /// <summary>
        /// Reads migration data from a ZIP file.
        /// </summary>
        /// <param name="path">The path to the data.zip file.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed migration data.</returns>
        Task<MigrationData> ReadAsync(string path, IProgressReporter? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads migration data from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the ZIP file.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed migration data.</returns>
        Task<MigrationData> ReadAsync(Stream stream, IProgressReporter? progress = null, CancellationToken cancellationToken = default);
    }
}
