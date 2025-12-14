namespace PPDS.Plugins
{
    /// <summary>
    /// Specifies the type of entity image to register with a plugin step.
    /// </summary>
    public enum PluginImageType
    {
        /// <summary>
        /// Pre-image (0). Snapshot of the entity before the operation.
        /// Available in Pre-operation and Post-operation stages.
        /// </summary>
        PreImage = 0,

        /// <summary>
        /// Post-image (1). Snapshot of the entity after the operation.
        /// Only available in Post-operation stage.
        /// </summary>
        PostImage = 1,

        /// <summary>
        /// Both pre and post images (2).
        /// Only available in Post-operation stage.
        /// </summary>
        Both = 2
    }
}
