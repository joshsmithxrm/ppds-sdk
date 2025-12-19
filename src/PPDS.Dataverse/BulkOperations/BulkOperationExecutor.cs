using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using PPDS.Dataverse.DependencyInjection;
using PPDS.Dataverse.Pooling;

namespace PPDS.Dataverse.BulkOperations
{
    /// <summary>
    /// Executes bulk operations using modern Dataverse APIs.
    /// </summary>
    public sealed class BulkOperationExecutor : IBulkOperationExecutor
    {
        private readonly IDataverseConnectionPool _connectionPool;
        private readonly DataverseOptions _options;
        private readonly ILogger<BulkOperationExecutor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkOperationExecutor"/> class.
        /// </summary>
        /// <param name="connectionPool">The connection pool.</param>
        /// <param name="options">Configuration options.</param>
        /// <param name="logger">Logger instance.</param>
        public BulkOperationExecutor(
            IDataverseConnectionPool connectionPool,
            IOptions<DataverseOptions> options,
            ILogger<BulkOperationExecutor> logger)
        {
            _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> CreateMultipleAsync(
            string entityLogicalName,
            IEnumerable<Entity> entities,
            BulkOperationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _options.BulkOperations;
            var entityList = entities.ToList();

            _logger.LogInformation("CreateMultiple starting. Entity: {Entity}, Count: {Count}", entityLogicalName, entityList.Count);

            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            foreach (var batch in Batch(entityList, options.BatchSize))
            {
                var batchResult = await ExecuteBatchAsync(
                    entityLogicalName,
                    batch,
                    "CreateMultiple",
                    e => new CreateRequest { Target = e },
                    options,
                    cancellationToken);

                successCount += batchResult.SuccessCount;
                errors.AddRange(batchResult.Errors);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "CreateMultiple completed. Entity: {Entity}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                entityLogicalName, successCount, errors.Count, stopwatch.ElapsedMilliseconds);

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> UpdateMultipleAsync(
            string entityLogicalName,
            IEnumerable<Entity> entities,
            BulkOperationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _options.BulkOperations;
            var entityList = entities.ToList();

            _logger.LogInformation("UpdateMultiple starting. Entity: {Entity}, Count: {Count}", entityLogicalName, entityList.Count);

            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            foreach (var batch in Batch(entityList, options.BatchSize))
            {
                var batchResult = await ExecuteBatchAsync(
                    entityLogicalName,
                    batch,
                    "UpdateMultiple",
                    e => new UpdateRequest { Target = e },
                    options,
                    cancellationToken);

                successCount += batchResult.SuccessCount;
                errors.AddRange(batchResult.Errors);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "UpdateMultiple completed. Entity: {Entity}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                entityLogicalName, successCount, errors.Count, stopwatch.ElapsedMilliseconds);

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> UpsertMultipleAsync(
            string entityLogicalName,
            IEnumerable<Entity> entities,
            BulkOperationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _options.BulkOperations;
            var entityList = entities.ToList();

            _logger.LogInformation("UpsertMultiple starting. Entity: {Entity}, Count: {Count}", entityLogicalName, entityList.Count);

            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            foreach (var batch in Batch(entityList, options.BatchSize))
            {
                var batchResult = await ExecuteBatchAsync(
                    entityLogicalName,
                    batch,
                    "UpsertMultiple",
                    e => new UpsertRequest { Target = e },
                    options,
                    cancellationToken);

                successCount += batchResult.SuccessCount;
                errors.AddRange(batchResult.Errors);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "UpsertMultiple completed. Entity: {Entity}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                entityLogicalName, successCount, errors.Count, stopwatch.ElapsedMilliseconds);

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> DeleteMultipleAsync(
            string entityLogicalName,
            IEnumerable<Guid> ids,
            BulkOperationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= _options.BulkOperations;
            var idList = ids.ToList();

            _logger.LogInformation("DeleteMultiple starting. Entity: {Entity}, Count: {Count}", entityLogicalName, idList.Count);

            var stopwatch = Stopwatch.StartNew();
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            // Convert IDs to EntityReferences for deletion
            var entities = idList.Select((id, index) => new Entity(entityLogicalName, id)).ToList();

            foreach (var batch in Batch(entities, options.BatchSize))
            {
                var batchResult = await ExecuteBatchAsync(
                    entityLogicalName,
                    batch,
                    "DeleteMultiple",
                    e => new DeleteRequest { Target = e.ToEntityReference() },
                    options,
                    cancellationToken);

                successCount += batchResult.SuccessCount;
                errors.AddRange(batchResult.Errors);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "DeleteMultiple completed. Entity: {Entity}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                entityLogicalName, successCount, errors.Count, stopwatch.ElapsedMilliseconds);

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                Duration = stopwatch.Elapsed
            };
        }

        private async Task<BulkOperationResult> ExecuteBatchAsync(
            string entityLogicalName,
            List<Entity> batch,
            string operationName,
            Func<Entity, OrganizationRequest> requestFactory,
            BulkOperationOptions options,
            CancellationToken cancellationToken)
        {
            var errors = new List<BulkOperationError>();
            var successCount = 0;

            await using var client = await _connectionPool.GetClientAsync(cancellationToken: cancellationToken);

            // Build ExecuteMultiple request
            var executeMultiple = new ExecuteMultipleRequest
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = options.ContinueOnError,
                    ReturnResponses = true
                }
            };

            foreach (var entity in batch)
            {
                var request = requestFactory(entity);

                // Apply bypass options
                if (options.BypassCustomPluginExecution)
                {
                    request.Parameters["BypassCustomPluginExecution"] = true;
                }

                if (options.SuppressDuplicateDetection)
                {
                    request.Parameters["SuppressDuplicateDetection"] = true;
                }

                executeMultiple.Requests.Add(request);
            }

            var response = (ExecuteMultipleResponse)await client.ExecuteAsync(executeMultiple, cancellationToken);

            // Process responses
            for (int i = 0; i < batch.Count; i++)
            {
                var itemResponse = response.Responses.FirstOrDefault(r => r.RequestIndex == i);

                if (itemResponse?.Fault != null)
                {
                    errors.Add(new BulkOperationError
                    {
                        Index = i,
                        RecordId = batch[i].Id != Guid.Empty ? batch[i].Id : null,
                        ErrorCode = itemResponse.Fault.ErrorCode,
                        Message = itemResponse.Fault.Message
                    });
                }
                else
                {
                    successCount++;
                }
            }

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                Duration = TimeSpan.Zero
            };
        }

        private static IEnumerable<List<T>> Batch<T>(IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);

            foreach (var item in source)
            {
                batch.Add(item);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
