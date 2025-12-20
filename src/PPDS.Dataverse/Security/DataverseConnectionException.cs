using System;

namespace PPDS.Dataverse.Security
{
    /// <summary>
    /// Exception thrown when a Dataverse connection fails.
    /// This exception sanitizes error messages to prevent connection string secrets from leaking.
    /// </summary>
    /// <remarks>
    /// The original exception is preserved as the <see cref="Exception.InnerException"/> for debugging,
    /// but the <see cref="Exception.Message"/> is sanitized to remove any embedded credentials.
    /// </remarks>
    public class DataverseConnectionException : Exception
    {
        /// <summary>
        /// Gets the name of the connection configuration that failed.
        /// </summary>
        public string? ConnectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnectionException"/> class.
        /// </summary>
        public DataverseConnectionException()
            : base("A Dataverse connection error occurred.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnectionException"/> class.
        /// </summary>
        /// <param name="message">The error message (will be redacted if it contains sensitive data).</param>
        public DataverseConnectionException(string message)
            : base(ConnectionStringRedactor.RedactExceptionMessage(message))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnectionException"/> class.
        /// </summary>
        /// <param name="message">The error message (will be redacted if it contains sensitive data).</param>
        /// <param name="innerException">The original exception that caused this error.</param>
        public DataverseConnectionException(string message, Exception innerException)
            : base(ConnectionStringRedactor.RedactExceptionMessage(message), SanitizeInnerException(innerException))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseConnectionException"/> class.
        /// </summary>
        /// <param name="connectionName">The name of the connection configuration that failed.</param>
        /// <param name="message">The error message (will be redacted if it contains sensitive data).</param>
        /// <param name="innerException">The original exception that caused this error.</param>
        public DataverseConnectionException(string connectionName, string message, Exception innerException)
            : base(ConnectionStringRedactor.RedactExceptionMessage(message), SanitizeInnerException(innerException))
        {
            ConnectionName = connectionName;
        }

        /// <summary>
        /// Creates a connection exception for a failed connection attempt.
        /// </summary>
        /// <param name="connectionName">The name of the connection configuration.</param>
        /// <param name="innerException">The original exception from the connection attempt.</param>
        /// <returns>A sanitized exception safe for logging.</returns>
        public static DataverseConnectionException CreateConnectionFailed(string connectionName, Exception innerException)
        {
            var sanitizedMessage = $"Failed to establish connection '{connectionName}'. " +
                                   $"Error: {ConnectionStringRedactor.RedactExceptionMessage(innerException.Message)}";

            return new DataverseConnectionException(connectionName, sanitizedMessage, innerException);
        }

        /// <summary>
        /// Creates a connection exception for an authentication failure.
        /// </summary>
        /// <param name="connectionName">The name of the connection configuration.</param>
        /// <param name="innerException">The original exception from the authentication attempt.</param>
        /// <returns>A sanitized exception safe for logging.</returns>
        public static DataverseConnectionException CreateAuthenticationFailed(string connectionName, Exception innerException)
        {
            var sanitizedMessage = $"Authentication failed for connection '{connectionName}'. " +
                                   "Please verify your credentials and permissions.";

            return new DataverseConnectionException(connectionName, sanitizedMessage, innerException);
        }

        private static Exception? SanitizeInnerException(Exception? innerException)
        {
            // We keep the inner exception for debugging but note that in production logging
            // configurations, you should be careful not to log inner exception messages
            // that may contain connection strings.
            //
            // The inner exception is preserved to maintain the full stack trace for debugging,
            // but callers should use the outer exception's Message property for user-facing output.
            return innerException;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // Override ToString to ensure connection strings are redacted even in full exception output
            var baseString = base.ToString();
            return ConnectionStringRedactor.RedactExceptionMessage(baseString);
        }
    }
}
