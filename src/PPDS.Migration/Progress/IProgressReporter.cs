using System;

namespace PPDS.Migration.Progress
{
    /// <summary>
    /// Interface for reporting migration progress.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports a progress update.
        /// </summary>
        /// <param name="args">The progress event data.</param>
        void Report(ProgressEventArgs args);

        /// <summary>
        /// Reports operation completion.
        /// </summary>
        /// <param name="result">The migration result.</param>
        void Complete(MigrationResult result);

        /// <summary>
        /// Reports an error.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="context">Optional context about what was happening.</param>
        void Error(Exception exception, string? context = null);
    }
}
