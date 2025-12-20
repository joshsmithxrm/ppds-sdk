using System;

namespace PPDS.Migration.Export
{
    /// <summary>
    /// Options for export operations.
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Gets or sets the degree of parallelism for entity export.
        /// Default: ProcessorCount * 2
        /// </summary>
        public int DegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets the page size for FetchXML queries.
        /// Default: 5000
        /// </summary>
        public int PageSize { get; set; } = 5000;

        /// <summary>
        /// Gets or sets whether to export file attachments (notes, annotations).
        /// Default: false
        /// </summary>
        public bool ExportFiles { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum file size to export in bytes.
        /// Default: 10MB
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets or sets whether to compress the output ZIP.
        /// Default: true
        /// </summary>
        public bool CompressOutput { get; set; } = true;

        /// <summary>
        /// Gets or sets the progress reporting interval in records.
        /// Default: 100
        /// </summary>
        public int ProgressInterval { get; set; } = 100;
    }
}
