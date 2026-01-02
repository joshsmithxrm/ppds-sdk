using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace PPDS.Dataverse.Resilience;

/// <summary>
/// Detects Dataverse service protection throttle events and reports them.
/// Extracted from PooledClient for testability and SRP.
/// </summary>
internal sealed class ThrottleDetector
{
    private static readonly TimeSpan FallbackRetryAfter = TimeSpan.FromSeconds(30);

    private readonly string _connectionName;
    private readonly Action<string, TimeSpan>? _onThrottle;

    /// <summary>
    /// Creates a new throttle detector.
    /// </summary>
    /// <param name="connectionName">The connection name for reporting.</param>
    /// <param name="onThrottle">Callback when throttle is detected (connectionName, retryAfter).</param>
    public ThrottleDetector(string connectionName, Action<string, TimeSpan>? onThrottle)
    {
        _connectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        _onThrottle = onThrottle;
    }

    /// <summary>
    /// Checks if an exception is a service protection error and extracts the RetryAfter.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception was a throttle and was handled.</returns>
    public bool TryHandleThrottle(Exception ex)
    {
        if (ex is not FaultException<OrganizationServiceFault> faultEx)
        {
            return false;
        }

        var fault = faultEx.Detail;
        if (!ServiceProtectionException.IsServiceProtectionError(fault.ErrorCode))
        {
            return false;
        }

        var retryAfter = ExtractRetryAfter(fault);
        _onThrottle?.Invoke(_connectionName, retryAfter);
        return true;
    }

    /// <summary>
    /// Extracts the Retry-After duration from a fault.
    /// </summary>
    private static TimeSpan ExtractRetryAfter(OrganizationServiceFault fault)
    {
        if (fault.ErrorDetails != null &&
            fault.ErrorDetails.TryGetValue("Retry-After", out var retryAfterObj))
        {
            return retryAfterObj switch
            {
                TimeSpan ts => ts,
                int seconds => TimeSpan.FromSeconds(seconds),
                double seconds => TimeSpan.FromSeconds(seconds),
                _ => FallbackRetryAfter
            };
        }

        return FallbackRetryAfter;
    }

    /// <summary>
    /// Wraps a synchronous operation with throttle detection.
    /// </summary>
    public T Execute<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception ex) when (TryHandleThrottle(ex))
        {
            throw; // Re-throw after recording throttle
        }
    }

    /// <summary>
    /// Wraps a synchronous void operation with throttle detection.
    /// </summary>
    public void Execute(Action operation)
    {
        try
        {
            operation();
        }
        catch (Exception ex) when (TryHandleThrottle(ex))
        {
            throw; // Re-throw after recording throttle
        }
    }

    /// <summary>
    /// Wraps an async operation with throttle detection.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (TryHandleThrottle(ex))
        {
            throw; // Re-throw after recording throttle
        }
    }

    /// <summary>
    /// Wraps an async void operation with throttle detection.
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (TryHandleThrottle(ex))
        {
            throw; // Re-throw after recording throttle
        }
    }
}
