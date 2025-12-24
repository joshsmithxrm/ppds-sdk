using System;

namespace PPDS.Dataverse.Resilience
{
    /// <summary>
    /// Configuration options for adaptive rate control.
    /// </summary>
    public class AdaptiveRateOptions
    {
        /// <summary>
        /// Gets or sets whether adaptive rate control is enabled.
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the hard ceiling for parallelism (Microsoft's per-user limit).
        /// Default: 52
        /// </summary>
        public int HardCeiling { get; set; } = 52;

        /// <summary>
        /// Gets or sets the absolute minimum parallelism.
        /// Fallback if server recommends less than this.
        /// Default: 1
        /// </summary>
        public int MinParallelism { get; set; } = 1;

        /// <summary>
        /// Gets or sets the parallelism increase amount per stabilization period.
        /// Default: 2
        /// </summary>
        public int IncreaseRate { get; set; } = 2;

        /// <summary>
        /// Gets or sets the multiplier applied on throttle (0.5-0.9).
        /// Default: 0.5 (aggressive backoff, throttle ceiling handles future probing)
        /// </summary>
        public double DecreaseFactor { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the number of successful batches required before considering increase.
        /// Default: 3
        /// </summary>
        public int StabilizationBatches { get; set; } = 3;

        /// <summary>
        /// Gets or sets the minimum time between parallelism increases.
        /// Default: 5 seconds
        /// </summary>
        public TimeSpan MinIncreaseInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the multiplier for recovery phase increases.
        /// Default: 2.0
        /// </summary>
        public double RecoveryMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the TTL for lastKnownGood value.
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan LastKnownGoodTTL { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the idle period after which state resets.
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan IdleResetPeriod { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether execution time-based ceiling is enabled.
        /// When enabled, parallelism is capped based on observed batch durations
        /// to avoid exhausting the server's execution time budget.
        /// Default: true
        /// </summary>
        public bool ExecutionTimeCeilingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the execution time ceiling factor.
        /// The ceiling is calculated as: Factor / AverageBatchTimeSeconds.
        /// Higher values allow more aggressive parallelism.
        /// Default: 250 (e.g., 10s batches → ceiling of 25, 15s batches → ceiling of 16)
        /// </summary>
        /// <remarks>
        /// This factor accounts for Microsoft's execution time limit (1200s per 5-min window = 4s/s).
        /// Server execution time is roughly 1/3 of wall-clock batch time.
        /// Formula derivation: at parallelism P with batch time T, consumption ≈ (P/T) × (T/3) = P/3.
        /// For P/3 ≤ 4 → P ≤ 12 per user. Factor 250 gives ceiling = 250/T, which is conservative.
        /// </remarks>
        public int ExecutionTimeCeilingFactor { get; set; } = 250;

        /// <summary>
        /// Gets or sets the minimum number of batch samples required before
        /// applying the execution time ceiling. Until this threshold is reached,
        /// the hard ceiling is used.
        /// Default: 3 (ceiling kicks in quickly to prevent over-ramping)
        /// </summary>
        public int MinBatchSamplesForCeiling { get; set; } = 3;

        /// <summary>
        /// Gets or sets the slow batch threshold in milliseconds.
        /// Execution time ceiling is only applied when average batch duration exceeds this threshold.
        /// This allows fast operations (like creates) to run at full parallelism while
        /// protecting slow operations (like updates/deletes) from execution time exhaustion.
        /// Default: 10000 (10 seconds)
        /// </summary>
        public int SlowBatchThresholdMs { get; set; } = 10_000;

        /// <summary>
        /// Gets or sets the smoothing factor for the exponential moving average
        /// of batch durations. Higher values weight recent batches more heavily.
        /// Range: 0.0-1.0. Default: 0.3
        /// </summary>
        public double BatchDurationSmoothingFactor { get; set; } = 0.3;
    }
}
