using PPDS.Migration.Models;

namespace PPDS.Migration.Analysis
{
    /// <summary>
    /// Interface for building execution plans from dependency graphs.
    /// </summary>
    public interface IExecutionPlanBuilder
    {
        /// <summary>
        /// Creates an execution plan from a dependency graph.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="schema">The migration schema.</param>
        /// <returns>The execution plan with tiers and deferred fields.</returns>
        ExecutionPlan Build(DependencyGraph graph, MigrationSchema schema);
    }
}
