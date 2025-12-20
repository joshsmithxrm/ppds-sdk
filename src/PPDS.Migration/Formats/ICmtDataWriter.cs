using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Formats
{
    /// <summary>
    /// Interface for writing CMT-compatible data files.
    /// </summary>
    public interface ICmtDataWriter
    {
        /// <summary>
        /// Writes migration data to a ZIP file.
        /// </summary>
        /// <param name="data">The migration data to write.</param>
        /// <param name="path">The output ZIP file path.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WriteAsync(MigrationData data, string path, IProgressReporter? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes migration data to a stream.
        /// </summary>
        /// <param name="data">The migration data to write.</param>
        /// <param name="stream">The output stream.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WriteAsync(MigrationData data, Stream stream, IProgressReporter? progress = null, CancellationToken cancellationToken = default);
    }
}
