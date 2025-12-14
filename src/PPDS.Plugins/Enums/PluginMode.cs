namespace PPDS.Plugins
{
    /// <summary>
    /// Specifies the execution mode for a plugin step.
    /// </summary>
    public enum PluginMode
    {
        /// <summary>
        /// Synchronous execution (0). Plugin executes immediately and blocks the operation.
        /// Use for validation, data modification, or when immediate feedback is required.
        /// </summary>
        Synchronous = 0,

        /// <summary>
        /// Asynchronous execution (1). Plugin executes in the background via the async service.
        /// Use for non-critical operations like logging, notifications, or external integrations.
        /// </summary>
        Asynchronous = 1
    }
}
