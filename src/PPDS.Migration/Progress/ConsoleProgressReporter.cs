using System;
using System.Diagnostics;

namespace PPDS.Migration.Progress
{
    /// <summary>
    /// Progress reporter that writes human-readable output to the console.
    /// </summary>
    public class ConsoleProgressReporter : IProgressReporter
    {
        private readonly Stopwatch _stopwatch = new();
        private string? _lastEntity;
        private int _lastProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleProgressReporter"/> class.
        /// </summary>
        public ConsoleProgressReporter()
        {
            _stopwatch.Start();
        }

        /// <inheritdoc />
        public void Report(ProgressEventArgs args)
        {
            var elapsed = _stopwatch.Elapsed;
            var prefix = $"[{elapsed:hh\\:mm\\:ss}]";

            switch (args.Phase)
            {
                case MigrationPhase.Analyzing:
                    Console.WriteLine($"{prefix} {args.Message}");
                    break;

                case MigrationPhase.Exporting:
                case MigrationPhase.Importing:
                    if (args.Entity != _lastEntity || args.Current == args.Total || ShouldUpdate(args.Current))
                    {
                        var phase = args.Phase == MigrationPhase.Exporting ? "Export" : "Import";
                        var tierInfo = args.TierNumber.HasValue ? $" (Tier {args.TierNumber})" : "";
                        var rps = args.RecordsPerSecond.HasValue ? $" @ {args.RecordsPerSecond:F1} rec/s" : "";
                        var pct = args.Total > 0 ? $" ({args.PercentComplete:F0}%)" : "";

                        Console.WriteLine($"{prefix} [{phase}] {args.Entity}{tierInfo}: {args.Current:N0}/{args.Total:N0}{pct}{rps}");

                        _lastEntity = args.Entity;
                        _lastProgress = args.Current;
                    }
                    break;

                case MigrationPhase.ProcessingDeferredFields:
                    Console.WriteLine($"{prefix} [Deferred] {args.Entity}.{args.Field}: {args.Current:N0}/{args.Total:N0}");
                    break;

                case MigrationPhase.ProcessingRelationships:
                    Console.WriteLine($"{prefix} [M2M] {args.Relationship}: {args.Current:N0}/{args.Total:N0}");
                    break;

                default:
                    if (!string.IsNullOrEmpty(args.Message))
                    {
                        Console.WriteLine($"{prefix} {args.Message}");
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void Complete(MigrationResult result)
        {
            _stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(result.Success ? "Migration Completed Successfully" : "Migration Completed with Errors");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Duration:    {result.Duration:hh\\:mm\\:ss}");
            Console.WriteLine($"Records:     {result.RecordsProcessed:N0}");
            Console.WriteLine($"Throughput:  {result.RecordsPerSecond:F1} records/second");

            if (result.FailureCount > 0)
            {
                Console.WriteLine($"Failures:    {result.FailureCount:N0}");
            }
            Console.WriteLine();
        }

        /// <inheritdoc />
        public void Error(Exception exception, string? context = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine();
            Console.Error.WriteLine($"ERROR: {exception.Message}");
            if (!string.IsNullOrEmpty(context))
            {
                Console.Error.WriteLine($"Context: {context}");
            }
            Console.ResetColor();
        }

        private bool ShouldUpdate(int current)
        {
            // Update every 1000 records or 10% progress
            return current - _lastProgress >= 1000 || current - _lastProgress >= 100;
        }
    }
}
