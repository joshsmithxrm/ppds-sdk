using System;

namespace PPDS.Dataverse.Sql.Ast;

/// <summary>
/// Reference to a SQL variable: @threshold, @entityName.
/// Used in expressions where a declared variable is referenced by name.
/// </summary>
public sealed class SqlVariableExpression : ISqlExpression
{
    /// <summary>The variable name including the @ prefix.</summary>
    public string VariableName { get; }

    public SqlVariableExpression(string variableName)
    {
        VariableName = variableName ?? throw new ArgumentNullException(nameof(variableName));
    }
}
