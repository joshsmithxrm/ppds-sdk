using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PPDS.Dataverse.Services;

/// <summary>
/// Service for querying and monitoring Dataverse import jobs.
/// </summary>
public interface IImportJobService
{
    /// <summary>
    /// Lists import jobs in the environment.
    /// </summary>
    /// <param name="solutionName">Optional filter by solution name.</param>
    /// <param name="top">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<ImportJobInfo>> ListAsync(
        string? solutionName = null,
        int top = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an import job by ID.
    /// </summary>
    /// <param name="importJobId">The import job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ImportJobInfo?> GetAsync(Guid importJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the raw XML data for an import job.
    /// </summary>
    /// <param name="importJobId">The import job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> GetDataAsync(Guid importJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for an import job to complete.
    /// </summary>
    /// <param name="importJobId">The import job ID.</param>
    /// <param name="pollInterval">Interval between status checks.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="onProgress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ImportJobInfo> WaitForCompletionAsync(
        Guid importJobId,
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        Action<ImportJobInfo>? onProgress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an import job.
/// </summary>
public record ImportJobInfo(
    Guid Id,
    string? Name,
    string? SolutionName,
    Guid? SolutionId,
    double Progress,
    DateTime? StartedOn,
    DateTime? CompletedOn,
    DateTime? CreatedOn,
    bool IsComplete);
