using PPDS.Dataverse.Query;

namespace PPDS.Cli.Interactive.Components.QueryResults;

/// <summary>
/// Groups query columns by semantic type for organized display.
/// </summary>
internal static class FieldGrouper
{
    private static readonly HashSet<string> SystemFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdon",
        "createdby",
        "modifiedon",
        "modifiedby",
        "ownerid",
        "owninguser",
        "owningteam",
        "owningbusinessunit",
        "statecode",
        "statuscode",
        "versionnumber",
        "overriddencreatedon",
        "importsequencenumber",
        "timezoneruleversionnumber",
        "utcconversiontimezonecode"
    };

    /// <summary>
    /// Groups fields by semantic type.
    /// </summary>
    public static IReadOnlyList<FieldGroup> GroupFields(
        IReadOnlyList<QueryColumn> columns,
        IReadOnlyDictionary<string, QueryValue> record,
        bool includeNulls)
    {
        var identifiers = new List<FieldInfo>();
        var coreFields = new List<FieldInfo>();
        var systemFields = new List<FieldInfo>();

        foreach (var column in columns)
        {
            var key = column.Alias ?? column.LogicalName;
            record.TryGetValue(key, out var value);

            if (!includeNulls && value?.Value == null)
            {
                continue;
            }

            var fieldInfo = new FieldInfo
            {
                Column = column,
                DisplayName = GetDisplayName(column),
                Value = value
            };

            if (IsIdentifierField(column))
            {
                identifiers.Add(fieldInfo);
            }
            else if (IsSystemField(column))
            {
                systemFields.Add(fieldInfo);
            }
            else
            {
                coreFields.Add(fieldInfo);
            }
        }

        return new List<FieldGroup>
        {
            new() { Name = "Identifiers", Fields = identifiers },
            new() { Name = "Core Fields", Fields = coreFields },
            new() { Name = "System", Fields = systemFields }
        };
    }

    private static string GetDisplayName(QueryColumn column)
    {
        // Prefer display name if available
        if (!string.IsNullOrEmpty(column.DisplayName))
        {
            return column.DisplayName;
        }

        // Use alias if present
        if (!string.IsNullOrEmpty(column.Alias))
        {
            return column.Alias;
        }

        // Fall back to logical name
        return column.LogicalName;
    }

    private static bool IsIdentifierField(QueryColumn column)
    {
        // GUID type is always an identifier
        if (column.DataType == QueryColumnType.Guid)
        {
            return true;
        }

        // Lookups are identifiers
        if (column.DataType == QueryColumnType.Lookup)
        {
            return true;
        }

        // Fields ending with "id" are likely identifiers
        if (column.LogicalName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsSystemField(QueryColumn column)
    {
        return SystemFieldNames.Contains(column.LogicalName);
    }
}

/// <summary>
/// Represents a group of related fields.
/// </summary>
internal sealed class FieldGroup
{
    /// <summary>
    /// The group name (e.g., "Identifiers", "Core Fields", "System").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The fields in this group.
    /// </summary>
    public required IReadOnlyList<FieldInfo> Fields { get; init; }
}

/// <summary>
/// Represents a single field with its metadata and value.
/// </summary>
internal sealed class FieldInfo
{
    /// <summary>
    /// The column metadata.
    /// </summary>
    public required QueryColumn Column { get; init; }

    /// <summary>
    /// The display name for this field.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The value of this field in the current record.
    /// </summary>
    public QueryValue? Value { get; init; }
}
