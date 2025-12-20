using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PPDS.Migration.Models;

namespace PPDS.Migration.Formats
{
    /// <summary>
    /// Interface for reading CMT schema files.
    /// </summary>
    public interface ICmtSchemaReader
    {
        /// <summary>
        /// Reads a schema from a file path.
        /// </summary>
        /// <param name="path">The path to the schema.xml file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed migration schema.</returns>
        Task<MigrationSchema> ReadAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a schema from a stream.
        /// </summary>
        /// <param name="stream">The stream containing schema XML.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed migration schema.</returns>
        Task<MigrationSchema> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
