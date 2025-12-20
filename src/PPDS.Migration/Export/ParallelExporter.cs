using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Security;
using PPDS.Migration.Formats;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Export
{
    /// <summary>
    /// Parallel exporter for Dataverse data.
    /// </summary>
    public class ParallelExporter : IExporter
    {
        private readonly IDataverseConnectionPool _connectionPool;
        private readonly ICmtSchemaReader _schemaReader;
        private readonly ICmtDataWriter _dataWriter;
        private readonly ILogger<ParallelExporter>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelExporter"/> class.
        /// </summary>
        /// <param name="connectionPool">The connection pool.</param>
        /// <param name="schemaReader">The schema reader.</param>
        /// <param name="dataWriter">The data writer.</param>
        public ParallelExporter(
            IDataverseConnectionPool connectionPool,
            ICmtSchemaReader schemaReader,
            ICmtDataWriter dataWriter)
        {
            _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            _schemaReader = schemaReader ?? throw new ArgumentNullException(nameof(schemaReader));
            _dataWriter = dataWriter ?? throw new ArgumentNullException(nameof(dataWriter));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelExporter"/> class.
        /// </summary>
        /// <param name="connectionPool">The connection pool.</param>
        /// <param name="schemaReader">The schema reader.</param>
        /// <param name="dataWriter">The data writer.</param>
        /// <param name="logger">The logger.</param>
        public ParallelExporter(
            IDataverseConnectionPool connectionPool,
            ICmtSchemaReader schemaReader,
            ICmtDataWriter dataWriter,
            ILogger<ParallelExporter> logger)
            : this(connectionPool, schemaReader, dataWriter)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ExportResult> ExportAsync(
            string schemaPath,
            string outputPath,
            ExportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new ProgressEventArgs
            {
                Phase = MigrationPhase.Analyzing,
                Message = "Parsing schema..."
            });

            var schema = await _schemaReader.ReadAsync(schemaPath, cancellationToken).ConfigureAwait(false);

            return await ExportAsync(schema, outputPath, options, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ExportResult> ExportAsync(
            MigrationSchema schema,
            string outputPath,
            ExportOptions? options = null,
            IProgressReporter? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            options ??= new ExportOptions();
            var stopwatch = Stopwatch.StartNew();
            var entityResults = new ConcurrentBag<EntityExportResult>();
            var entityData = new ConcurrentDictionary<string, IReadOnlyList<Entity>>(StringComparer.OrdinalIgnoreCase);
            var errors = new ConcurrentBag<MigrationError>();

            _logger?.LogInformation("Starting parallel export of {Count} entities with parallelism {Parallelism}",
                schema.Entities.Count, options.DegreeOfParallelism);

            progress?.Report(new ProgressEventArgs
            {
                Phase = MigrationPhase.Exporting,
                Message = $"Exporting {schema.Entities.Count} entities..."
            });

            try
            {
                // Export all entities in parallel
                await Parallel.ForEachAsync(
                    schema.Entities,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = options.DegreeOfParallelism,
                        CancellationToken = cancellationToken
                    },
                    async (entitySchema, ct) =>
                    {
                        var result = await ExportEntityAsync(entitySchema, options, progress, ct).ConfigureAwait(false);
                        entityResults.Add(result);

                        if (result.Success && result.Records != null)
                        {
                            entityData[entitySchema.LogicalName] = result.Records;
                        }
                        else if (!result.Success)
                        {
                            errors.Add(new MigrationError
                            {
                                Phase = MigrationPhase.Exporting,
                                EntityLogicalName = entitySchema.LogicalName,
                                Message = result.ErrorMessage ?? "Unknown error"
                            });
                        }
                    }).ConfigureAwait(false);

                // Write to output file
                progress?.Report(new ProgressEventArgs
                {
                    Phase = MigrationPhase.Exporting,
                    Message = "Writing output file..."
                });

                var migrationData = new MigrationData
                {
                    Schema = schema,
                    EntityData = entityData,
                    ExportedAt = DateTime.UtcNow
                };

                await _dataWriter.WriteAsync(migrationData, outputPath, progress, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                var totalRecords = entityResults.Sum(r => r.RecordCount);

                _logger?.LogInformation("Export complete: {Entities} entities, {Records} records in {Duration}",
                    entityResults.Count, totalRecords, stopwatch.Elapsed);

                var result = new ExportResult
                {
                    Success = errors.Count == 0,
                    EntitiesExported = entityResults.Count(r => r.Success),
                    RecordsExported = totalRecords,
                    Duration = stopwatch.Elapsed,
                    EntityResults = entityResults.ToArray(),
                    OutputPath = outputPath,
                    Errors = errors.ToArray()
                };

                progress?.Complete(new MigrationResult
                {
                    Success = result.Success,
                    RecordsProcessed = result.RecordsExported,
                    SuccessCount = result.RecordsExported,
                    FailureCount = errors.Count,
                    Duration = result.Duration
                });

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stopwatch.Stop();
                _logger?.LogError(ex, "Export failed");

                var safeMessage = ConnectionStringRedactor.RedactExceptionMessage(ex.Message);
                progress?.Error(ex, "Export failed");

                return new ExportResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    EntityResults = entityResults.ToArray(),
                    Errors = new[]
                    {
                        new MigrationError
                        {
                            Phase = MigrationPhase.Exporting,
                            Message = safeMessage
                        }
                    }
                };
            }
        }

        private async Task<EntityExportResultWithData> ExportEntityAsync(
            EntitySchema entitySchema,
            ExportOptions options,
            IProgressReporter? progress,
            CancellationToken cancellationToken)
        {
            var entityStopwatch = Stopwatch.StartNew();
            var records = new List<Entity>();

            try
            {
                _logger?.LogDebug("Exporting entity {Entity}", entitySchema.LogicalName);

                await using var client = await _connectionPool.GetClientAsync(null, cancellationToken).ConfigureAwait(false);

                // Build FetchXML
                var fetchXml = BuildFetchXml(entitySchema, options.PageSize);
                var pageNumber = 1;
                string? pagingCookie = null;
                var lastReportedCount = 0;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var pagedFetchXml = AddPaging(fetchXml, pageNumber, pagingCookie);
                    var response = await client.RetrieveMultipleAsync(new FetchExpression(pagedFetchXml)).ConfigureAwait(false);

                    records.AddRange(response.Entities);

                    // Report progress at intervals
                    if (records.Count - lastReportedCount >= options.ProgressInterval || !response.MoreRecords)
                    {
                        var rps = entityStopwatch.Elapsed.TotalSeconds > 0
                            ? records.Count / entityStopwatch.Elapsed.TotalSeconds
                            : 0;

                        progress?.Report(new ProgressEventArgs
                        {
                            Phase = MigrationPhase.Exporting,
                            Entity = entitySchema.LogicalName,
                            Current = records.Count,
                            Total = records.Count, // We don't know total upfront
                            RecordsPerSecond = rps
                        });

                        lastReportedCount = records.Count;
                    }

                    if (!response.MoreRecords)
                    {
                        break;
                    }

                    pagingCookie = response.PagingCookie;
                    pageNumber++;
                }

                entityStopwatch.Stop();

                _logger?.LogDebug("Exported {Count} records from {Entity} in {Duration}",
                    records.Count, entitySchema.LogicalName, entityStopwatch.Elapsed);

                return new EntityExportResultWithData
                {
                    EntityLogicalName = entitySchema.LogicalName,
                    RecordCount = records.Count,
                    Duration = entityStopwatch.Elapsed,
                    Success = true,
                    Records = records
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityStopwatch.Stop();

                var safeMessage = ConnectionStringRedactor.RedactExceptionMessage(ex.Message);
                _logger?.LogError(ex, "Failed to export entity {Entity}", entitySchema.LogicalName);

                return new EntityExportResultWithData
                {
                    EntityLogicalName = entitySchema.LogicalName,
                    RecordCount = records.Count,
                    Duration = entityStopwatch.Elapsed,
                    Success = false,
                    ErrorMessage = safeMessage,
                    Records = null
                };
            }
        }

        private string BuildFetchXml(EntitySchema entitySchema, int pageSize)
        {
            var fetch = new XElement("fetch",
                new XAttribute("count", pageSize),
                new XAttribute("returntotalrecordcount", "false"),
                new XElement("entity",
                    new XAttribute("name", entitySchema.LogicalName),
                    entitySchema.Fields.Select(f => new XElement("attribute",
                        new XAttribute("name", f.LogicalName)))));

            // Add filter if specified
            if (!string.IsNullOrEmpty(entitySchema.FetchXmlFilter))
            {
                var filterDoc = XDocument.Parse($"<root>{entitySchema.FetchXmlFilter}</root>");
                var entityElement = fetch.Element("entity");
                if (filterDoc.Root != null)
                {
                    foreach (var child in filterDoc.Root.Elements())
                    {
                        entityElement?.Add(child);
                    }
                }
            }

            return fetch.ToString(SaveOptions.DisableFormatting);
        }

        private string AddPaging(string fetchXml, int pageNumber, string? pagingCookie)
        {
            var doc = XDocument.Parse(fetchXml);
            var fetch = doc.Root!;

            fetch.SetAttributeValue("page", pageNumber);

            if (!string.IsNullOrEmpty(pagingCookie))
            {
                fetch.SetAttributeValue("paging-cookie", pagingCookie);
            }

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private class EntityExportResultWithData : EntityExportResult
        {
            public IReadOnlyList<Entity>? Records { get; set; }
        }
    }
}
