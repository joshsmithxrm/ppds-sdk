namespace PPDS.Dataverse.BulkOperations
{
    /// <summary>
    /// Configuration options for bulk operations.
    /// </summary>
    public class BulkOperationOptions
    {
        /// <summary>
        /// Gets or sets the number of records per batch.
        /// Default: 1000 (Dataverse maximum)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to continue on individual record failures.
        /// Default: true
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to bypass custom plugin execution.
        /// Default: false
        /// </summary>
        public bool BypassCustomPluginExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to bypass Power Automate flows.
        /// Default: false
        /// </summary>
        public bool BypassPowerAutomateFlows { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to suppress duplicate detection.
        /// Default: false
        /// </summary>
        public bool SuppressDuplicateDetection { get; set; } = false;
    }
}
