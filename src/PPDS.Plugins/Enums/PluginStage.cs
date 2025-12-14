namespace PPDS.Plugins
{
    /// <summary>
    /// Specifies the stage in the event pipeline when a plugin executes.
    /// </summary>
    public enum PluginStage
    {
        /// <summary>
        /// Pre-validation stage (10). Executes before main system validation.
        /// Use for validation that should occur before any database locks.
        /// </summary>
        PreValidation = 10,

        /// <summary>
        /// Pre-operation stage (20). Executes before the main operation.
        /// Use for modifying data before it's written to the database.
        /// </summary>
        PreOperation = 20,

        /// <summary>
        /// Post-operation stage (40). Executes after the main operation.
        /// Use for actions that depend on the committed data.
        /// </summary>
        PostOperation = 40
    }
}
