using PPDS.Migration.Models;

namespace PPDS.Migration.Analysis
{
    /// <summary>
    /// Interface for building entity dependency graphs from schemas.
    /// </summary>
    public interface IDependencyGraphBuilder
    {
        /// <summary>
        /// Analyzes a schema and builds a dependency graph.
        /// </summary>
        /// <param name="schema">The migration schema.</param>
        /// <returns>The dependency graph with topologically sorted tiers.</returns>
        DependencyGraph Build(MigrationSchema schema);
    }
}
