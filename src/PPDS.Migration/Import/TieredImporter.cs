using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using PPDS.Dataverse.BulkOperations;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Security;
using PPDS.Migration.Analysis;
using PPDS.Migration.Formats;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Import
{
    /// <summary>
    /// Tiered importer that respects dependency order.
    /// </summary>
    public class TieredImporter : IImporter
    {
        private readonly IDataverseConnectionPool _connectionPool;
        private readonly IBulkOperationExecutor _bulkExecutor;
        private readonly ICmtDataReader _dataReader;
        private readonly IDependencyGraphBuilder _graphBuilder;
        private readonly IExecutionPlanBuilder _planBuilder;
        private readonly ILogger<TieredImporter>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TieredImporter"/> class.
        /// </summary>
        public TieredImporter(
            IDataverseConnectionPool connectionPool,
            IBulkOperationExecutor bulkExecutor,
            ICmtDataReader dataReader,
            IDependencyGraphBuilder graphBuilder,
            IExecutionPlanBuilder planBuilder)
        {
            _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            _bulkExecutor = bulkExecutor ?? throw new ArgumentNullException(nameof(bulkExecutor));
            _dataReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
            _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
            _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TieredImporter"/> class.
        /// </summary>
        public TieredImporter(
            IDataverseConnectionPool connectionPool,
            IBulkOperationExecutor bulkExecutor,
            ICmtDataReader dataReader,
            IDependencyGraphBuilder graphBuilder,
            IExecutionPlanBuilder planBuilder,
            ILogger<TieredImporter> logger)
            : this(connectionPool, bulkExecutor, dataReader, graphBuilder, planBuilder)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ImportResult> ImportAsync(
            string dataPath,
            ImportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new ProgressEventArgs
            {
                Phase = MigrationPhase.Analyzing,
                Message = "Reading data archive..."
            });

            var data = await _dataReader.ReadAsync(dataPath, progress, cancellationToken).ConfigureAwait(false);

            progress?.Report(new ProgressEventArgs
            {
                Phase = MigrationPhase.Analyzing,
                Message = "Building dependency graph..."
            });

            var graph = _graphBuilder.Build(data.Schema);
            var plan = _planBuilder.Build(graph, data.Schema);

            return await ImportAsync(data, plan, options, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ImportResult> ImportAsync(
            MigrationData data,
            ExecutionPlan plan,
            ImportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            options ??= new ImportOptions();
            var stopwatch = Stopwatch.StartNew();
            var idMappings = new IdMappingCollection();
            var entityResults = new ConcurrentBag<EntityImportResult>();
            var errors = new ConcurrentBag<MigrationError>();
            var totalImported = 0;

            _logger?.LogInformation("Starting tiered import: {Tiers} tiers, {Records} records",
                plan.TierCount, data.TotalRecordCount);

            try
            {
                // Process each tier sequentially
                foreach (var tier in plan.Tiers)
                {
                    progress?.Report(new ProgressEventArgs
                    {
                        Phase = MigrationPhase.Importing,
                        TierNumber = tier.TierNumber,
                        Message = $"Processing tier {tier.TierNumber}: {string.Join(", ", tier.Entities)}"
                    });

                    // Process entities within tier in parallel
                    await Parallel.ForEachAsync(
                        tier.Entities,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = options.MaxParallelEntities,
                            CancellationToken = cancellationToken
                        },
                        async (entityName, ct) =>
                        {
                            if (!data.EntityData.TryGetValue(entityName, out var records) || records.Count == 0)
                            {
                                return;
                            }

                            // Get deferred fields for this entity
                            plan.DeferredFields.TryGetValue(entityName, out var deferredFields);

                            var result = await ImportEntityAsync(
                                entityName,
                                records,
                                tier.TierNumber,
                                deferredFields,
                                idMappings,
                                options,
                                progress,
                                ct).ConfigureAwait(false);

                            entityResults.Add(result);
                            Interlocked.Add(ref totalImported, result.SuccessCount);

                            if (!result.Success)
                            {
                                errors.Add(new MigrationError
                                {
                                    Phase = MigrationPhase.Importing,
                                    EntityLogicalName = entityName,
                                    Message = $"Entity import had {result.FailureCount} failures"
                                });
                            }
                        }).ConfigureAwait(false);

                    _logger?.LogInformation("Tier {Tier} complete", tier.TierNumber);
                }

                // Process deferred fields
                var deferredUpdates = 0;
                if (plan.DeferredFieldCount > 0)
                {
                    deferredUpdates = await ProcessDeferredFieldsAsync(
                        data, plan, idMappings, options, progress, cancellationToken).ConfigureAwait(false);
                }

                // Process M2M relationships
                var relationshipsProcessed = 0;
                if (plan.ManyToManyRelationships.Count > 0)
                {
                    relationshipsProcessed = await ProcessRelationshipsAsync(
                        data, plan, idMappings, options, progress, cancellationToken).ConfigureAwait(false);
                }

                stopwatch.Stop();

                _logger?.LogInformation("Import complete: {Records} imported, {Deferred} deferred, {M2M} relationships in {Duration}",
                    totalImported, deferredUpdates, relationshipsProcessed, stopwatch.Elapsed);

                var result = new ImportResult
                {
                    Success = errors.IsEmpty,
                    TiersProcessed = plan.TierCount,
                    RecordsImported = totalImported,
                    RecordsUpdated = deferredUpdates,
                    RelationshipsProcessed = relationshipsProcessed,
                    Duration = stopwatch.Elapsed,
                    EntityResults = entityResults.ToArray(),
                    Errors = errors.ToArray()
                };

                progress?.Complete(new MigrationResult
                {
                    Success = result.Success,
                    RecordsProcessed = result.RecordsImported + result.RecordsUpdated,
                    SuccessCount = result.RecordsImported,
                    FailureCount = errors.Count,
                    Duration = result.Duration
                });

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stopwatch.Stop();
                _logger?.LogError(ex, "Import failed");

                var safeMessage = ConnectionStringRedactor.RedactExceptionMessage(ex.Message);
                progress?.Error(ex, "Import failed");

                return new ImportResult
                {
                    Success = false,
                    TiersProcessed = plan.TierCount,
                    RecordsImported = totalImported,
                    Duration = stopwatch.Elapsed,
                    EntityResults = entityResults.ToArray(),
                    Errors = new[]
                    {
                        new MigrationError
                        {
                            Phase = MigrationPhase.Importing,
                            Message = safeMessage
                        }
                    }
                };
            }
        }

        private async Task<EntityImportResult> ImportEntityAsync(
            string entityName,
            IReadOnlyList<Entity> records,
            int tierNumber,
            IReadOnlyList<string>? deferredFields,
            IdMappingCollection idMappings,
            ImportOptions options,
            IProgressReporter? progress,
            CancellationToken cancellationToken)
        {
            var entityStopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var failureCount = 0;
            var deferredSet = deferredFields != null
                ? new HashSet<string>(deferredFields, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _logger?.LogDebug("Importing {Count} records for {Entity}", records.Count, entityName);

            // Prepare records: remap lookups and null deferred fields
            var preparedRecords = new List<Entity>();
            foreach (var record in records)
            {
                var prepared = PrepareRecordForImport(record, deferredSet, idMappings);
                preparedRecords.Add(prepared);
            }

            // Batch import
            for (var i = 0; i < preparedRecords.Count; i += options.BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = preparedRecords.Skip(i).Take(options.BatchSize).ToList();
                var batchResult = await ImportBatchAsync(entityName, batch, options, cancellationToken).ConfigureAwait(false);

                // Track ID mappings
                for (var j = 0; j < batch.Count && j < batchResult.CreatedIds.Count; j++)
                {
                    var oldId = records[i + j].Id;
                    var newId = batchResult.CreatedIds[j];
                    idMappings.AddMapping(entityName, oldId, newId);
                }

                successCount += batchResult.SuccessCount;
                failureCount += batchResult.FailureCount;

                // Report progress
                var rps = entityStopwatch.Elapsed.TotalSeconds > 0
                    ? (successCount + failureCount) / entityStopwatch.Elapsed.TotalSeconds
                    : 0;

                progress?.Report(new ProgressEventArgs
                {
                    Phase = MigrationPhase.Importing,
                    Entity = entityName,
                    TierNumber = tierNumber,
                    Current = successCount + failureCount,
                    Total = records.Count,
                    RecordsPerSecond = rps
                });
            }

            entityStopwatch.Stop();

            return new EntityImportResult
            {
                EntityLogicalName = entityName,
                TierNumber = tierNumber,
                RecordCount = records.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Duration = entityStopwatch.Elapsed,
                Success = failureCount == 0
            };
        }

        private Entity PrepareRecordForImport(
            Entity record,
            HashSet<string> deferredFields,
            IdMappingCollection idMappings)
        {
            var prepared = new Entity(record.LogicalName);
            prepared.Id = record.Id; // Keep original ID for mapping

            foreach (var attr in record.Attributes)
            {
                // Skip deferred fields
                if (deferredFields.Contains(attr.Key))
                {
                    continue;
                }

                // Remap entity references
                if (attr.Value is EntityReference er)
                {
                    if (idMappings.TryGetNewId(er.LogicalName, er.Id, out var newId))
                    {
                        prepared[attr.Key] = new EntityReference(er.LogicalName, newId);
                    }
                    // If not mapped yet, keep original (will be processed in deferred phase)
                }
                else
                {
                    prepared[attr.Key] = attr.Value;
                }
            }

            return prepared;
        }

        private async Task<BatchImportResult> ImportBatchAsync(
            string entityName,
            List<Entity> batch,
            ImportOptions options,
            CancellationToken cancellationToken)
        {
            var bulkOptions = new BulkOperationOptions
            {
                BatchSize = options.BatchSize,
                ContinueOnError = options.ContinueOnError,
                BypassCustomPluginExecution = options.BypassCustomPluginExecution
            };

            if (options.UseBulkApis)
            {
                var result = options.Mode switch
                {
                    ImportMode.Create => await _bulkExecutor.CreateMultipleAsync(entityName, batch, bulkOptions, cancellationToken).ConfigureAwait(false),
                    ImportMode.Update => await _bulkExecutor.UpdateMultipleAsync(entityName, batch, bulkOptions, cancellationToken).ConfigureAwait(false),
                    _ => await _bulkExecutor.UpsertMultipleAsync(entityName, batch, bulkOptions, cancellationToken).ConfigureAwait(false)
                };

                return new BatchImportResult
                {
                    SuccessCount = result.SuccessCount,
                    FailureCount = result.FailureCount,
                    CreatedIds = batch.Select(e => e.Id).ToList() // For bulk, IDs are preserved
                };
            }
            else
            {
                // Fallback to individual operations
                var createdIds = new List<Guid>();
                var successCount = 0;
                var failureCount = 0;

                await using var client = await _connectionPool.GetClientAsync(null, cancellationToken).ConfigureAwait(false);

                foreach (var record in batch)
                {
                    try
                    {
                        Guid newId;
                        switch (options.Mode)
                        {
                            case ImportMode.Create:
                                newId = await client.CreateAsync(record).ConfigureAwait(false);
                                break;
                            case ImportMode.Update:
                                await client.UpdateAsync(record).ConfigureAwait(false);
                                newId = record.Id;
                                break;
                            default:
                                var response = (UpsertResponse)await client.ExecuteAsync(new UpsertRequest { Target = record }).ConfigureAwait(false);
                                newId = response.Target?.Id ?? record.Id;
                                break;
                        }

                        createdIds.Add(newId);
                        successCount++;
                    }
                    catch
                    {
                        failureCount++;
                        if (!options.ContinueOnError)
                        {
                            throw;
                        }
                    }
                }

                return new BatchImportResult
                {
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    CreatedIds = createdIds
                };
            }
        }

        private async Task<int> ProcessDeferredFieldsAsync(
            MigrationData data,
            ExecutionPlan plan,
            IdMappingCollection idMappings,
            ImportOptions options,
            IProgressReporter? progress,
            CancellationToken cancellationToken)
        {
            var totalUpdated = 0;

            foreach (var (entityName, fields) in plan.DeferredFields)
            {
                if (!data.EntityData.TryGetValue(entityName, out var records))
                {
                    continue;
                }

                progress?.Report(new ProgressEventArgs
                {
                    Phase = MigrationPhase.ProcessingDeferredFields,
                    Entity = entityName,
                    Message = $"Updating deferred fields: {string.Join(", ", fields)}"
                });

                foreach (var record in records)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!idMappings.TryGetNewId(entityName, record.Id, out var newId))
                    {
                        continue;
                    }

                    var update = new Entity(entityName, newId);
                    var hasUpdates = false;

                    foreach (var fieldName in fields)
                    {
                        if (record.Contains(fieldName) && record[fieldName] is EntityReference er)
                        {
                            if (idMappings.TryGetNewId(er.LogicalName, er.Id, out var mappedId))
                            {
                                update[fieldName] = new EntityReference(er.LogicalName, mappedId);
                                hasUpdates = true;
                            }
                        }
                    }

                    if (hasUpdates)
                    {
                        await using var client = await _connectionPool.GetClientAsync(null, cancellationToken).ConfigureAwait(false);
                        await client.UpdateAsync(update).ConfigureAwait(false);
                        totalUpdated++;
                    }
                }
            }

            _logger?.LogInformation("Updated {Count} deferred field records", totalUpdated);
            return totalUpdated;
        }

        private async Task<int> ProcessRelationshipsAsync(
            MigrationData data,
            ExecutionPlan plan,
            IdMappingCollection idMappings,
            ImportOptions options,
            IProgressReporter? progress,
            CancellationToken cancellationToken)
        {
            var totalProcessed = 0;

            foreach (var relationship in plan.ManyToManyRelationships)
            {
                if (!data.RelationshipData.TryGetValue(relationship.Name, out var associations))
                {
                    continue;
                }

                progress?.Report(new ProgressEventArgs
                {
                    Phase = MigrationPhase.ProcessingRelationships,
                    Relationship = relationship.Name,
                    Total = associations.Count
                });

                foreach (var assoc in associations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!idMappings.TryGetNewId(assoc.Entity1LogicalName, assoc.Entity1Id, out var entity1NewId) ||
                        !idMappings.TryGetNewId(assoc.Entity2LogicalName, assoc.Entity2Id, out var entity2NewId))
                    {
                        continue;
                    }

                    await using var client = await _connectionPool.GetClientAsync(null, cancellationToken).ConfigureAwait(false);

                    var request = new AssociateRequest
                    {
                        Target = new EntityReference(assoc.Entity1LogicalName, entity1NewId),
                        RelatedEntities = new EntityReferenceCollection
                        {
                            new EntityReference(assoc.Entity2LogicalName, entity2NewId)
                        },
                        Relationship = new Relationship(relationship.Name)
                    };

                    try
                    {
                        await client.ExecuteAsync(request).ConfigureAwait(false);
                        totalProcessed++;
                    }
                    catch
                    {
                        // M2M associations may fail if already exists - log but continue
                        if (!options.ContinueOnError)
                        {
                            throw;
                        }
                    }
                }
            }

            _logger?.LogInformation("Processed {Count} M2M relationships", totalProcessed);
            return totalProcessed;
        }

        private class BatchImportResult
        {
            public int SuccessCount { get; set; }
            public int FailureCount { get; set; }
            public List<Guid> CreatedIds { get; set; } = new();
        }
    }
}
