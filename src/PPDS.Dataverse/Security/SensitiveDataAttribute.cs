using System;

namespace PPDS.Dataverse.Security
{
    /// <summary>
    /// Marks a property or field as containing sensitive data that should not be logged or displayed.
    /// This attribute serves as documentation and can be used by static analysis tools or
    /// custom serializers to identify data requiring redaction.
    /// </summary>
    /// <remarks>
    /// Properties marked with this attribute may contain:
    /// <list type="bullet">
    /// <item>Connection strings with embedded credentials</item>
    /// <item>Client secrets or API keys</item>
    /// <item>Passwords or tokens</item>
    /// <item>Other authentication credentials</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class DatabaseConfig
    /// {
    ///     public string ServerName { get; set; }
    ///
    ///     [SensitiveData]
    ///     public string ConnectionString { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class SensitiveDataAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a description of why this data is sensitive.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the type of sensitive data (e.g., "ConnectionString", "ApiKey", "Password").
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveDataAttribute"/> class.
        /// </summary>
        public SensitiveDataAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveDataAttribute"/> class.
        /// </summary>
        /// <param name="reason">A description of why this data is sensitive.</param>
        public SensitiveDataAttribute(string reason)
        {
            Reason = reason;
        }
    }
}
