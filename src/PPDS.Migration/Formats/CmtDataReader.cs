using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using PPDS.Migration.Models;
using PPDS.Migration.Progress;

namespace PPDS.Migration.Formats
{
    /// <summary>
    /// Reads CMT data.zip files.
    /// </summary>
    public class CmtDataReader : ICmtDataReader
    {
        private readonly ICmtSchemaReader _schemaReader;
        private readonly ILogger<CmtDataReader>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmtDataReader"/> class.
        /// </summary>
        /// <param name="schemaReader">The schema reader.</param>
        public CmtDataReader(ICmtSchemaReader schemaReader)
        {
            _schemaReader = schemaReader ?? throw new ArgumentNullException(nameof(schemaReader));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmtDataReader"/> class.
        /// </summary>
        /// <param name="schemaReader">The schema reader.</param>
        /// <param name="logger">The logger.</param>
        public CmtDataReader(ICmtSchemaReader schemaReader, ILogger<CmtDataReader> logger)
            : this(schemaReader)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<MigrationData> ReadAsync(string path, IProgressReporter? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Data file not found: {path}", path);
            }

            _logger?.LogInformation("Reading data from {Path}", path);

#if NET8_0_OR_GREATER
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
#else
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
#endif
            return await ReadAsync(stream, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<MigrationData> ReadAsync(Stream stream, IProgressReporter? progress = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            progress?.Report(new ProgressEventArgs
            {
                Phase = MigrationPhase.Analyzing,
                Message = "Opening data archive..."
            });

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // Read schema from archive
            var schemaEntry = archive.GetEntry("data_schema.xml") ?? archive.GetEntry("schema.xml");
            MigrationSchema schema;

            if (schemaEntry != null)
            {
                using var schemaStream = schemaEntry.Open();
                using var memoryStream = new MemoryStream();
                await schemaStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Position = 0;
                schema = await _schemaReader.ReadAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Data archive does not contain a schema file (data_schema.xml or schema.xml)");
            }

            // Read data from archive
            var dataEntry = archive.GetEntry("data.xml") ?? throw new InvalidOperationException("Data archive does not contain data.xml");

            using var dataStream = dataEntry.Open();
            using var dataMemoryStream = new MemoryStream();
            await dataStream.CopyToAsync(dataMemoryStream, cancellationToken).ConfigureAwait(false);
            dataMemoryStream.Position = 0;

            var entityData = await ParseDataXmlAsync(dataMemoryStream, schema, progress, cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation("Parsed data with {RecordCount} total records", entityData.Values.Sum(v => v.Count));

            return new MigrationData
            {
                Schema = schema,
                EntityData = entityData,
                RelationshipData = new Dictionary<string, IReadOnlyList<ManyToManyAssociation>>(),
                ExportedAt = DateTime.UtcNow
            };
        }

        private async Task<IReadOnlyDictionary<string, IReadOnlyList<Entity>>> ParseDataXmlAsync(
            Stream stream,
            MigrationSchema schema,
            IProgressReporter? progress,
            CancellationToken cancellationToken)
        {
#if NET8_0_OR_GREATER
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
#else
            var doc = XDocument.Load(stream, LoadOptions.None);
            await Task.CompletedTask;
#endif

            var root = doc.Root ?? throw new InvalidOperationException("Data XML has no root element");
            var entitiesElement = root.Name.LocalName.Equals("entities", StringComparison.OrdinalIgnoreCase)
                ? root
                : root.Element("entities") ?? throw new InvalidOperationException("Data XML has no <entities> element");

            var result = new Dictionary<string, IReadOnlyList<Entity>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entityElement in entitiesElement.Elements("entity"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entityName = entityElement.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(entityName))
                {
                    continue;
                }

                var entitySchema = schema.GetEntity(entityName);
                var records = new List<Entity>();

                foreach (var recordElement in entityElement.Elements("records").Elements("record"))
                {
                    var record = ParseRecord(recordElement, entityName, entitySchema);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }

                if (records.Count > 0)
                {
                    result[entityName] = records;
                    _logger?.LogDebug("Parsed {Count} records for entity {Entity}", records.Count, entityName);
                }
            }

            return result;
        }

        private Entity? ParseRecord(XElement recordElement, string entityName, EntitySchema? entitySchema)
        {
            var idAttribute = recordElement.Attribute("id")?.Value;
            if (string.IsNullOrEmpty(idAttribute) || !Guid.TryParse(idAttribute, out var recordId))
            {
                return null;
            }

            var entity = new Entity(entityName, recordId);

            foreach (var fieldElement in recordElement.Elements("field"))
            {
                var fieldName = fieldElement.Attribute("name")?.Value;
                var fieldValue = fieldElement.Attribute("value")?.Value ?? fieldElement.Value;
                var fieldType = fieldElement.Attribute("type")?.Value;

                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                var schemaField = entitySchema?.Fields.FirstOrDefault(f =>
                    f.LogicalName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                var parsedValue = ParseFieldValue(fieldValue, fieldType ?? schemaField?.Type, fieldElement);
                if (parsedValue != null)
                {
                    entity[fieldName] = parsedValue;
                }
            }

            return entity;
        }

        private object? ParseFieldValue(string? value, string? type, XElement element)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            type = type?.ToLowerInvariant();

            return type switch
            {
                "string" or "memo" or "nvarchar" => value,
                "int" or "integer" => int.TryParse(value, out var i) ? i : null,
                "decimal" or "money" => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null,
                "float" or "double" => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null,
                "bool" or "boolean" => bool.TryParse(value, out var b) ? b : null,
                "datetime" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) ? dt : null,
                "guid" or "uniqueidentifier" => Guid.TryParse(value, out var g) ? g : null,
                "lookup" or "customer" or "owner" => ParseEntityReference(element),
                "optionset" or "picklist" => ParseOptionSetValue(value),
                "state" or "status" => ParseOptionSetValue(value),
                _ => value // Return as string for unknown types
            };
        }

        private EntityReference? ParseEntityReference(XElement element)
        {
            var idValue = element.Attribute("value")?.Value ?? element.Attribute("id")?.Value;
            var entityName = element.Attribute("lookupentity")?.Value ?? element.Attribute("type")?.Value;
            var name = element.Attribute("lookupentityname")?.Value ?? element.Attribute("name")?.Value;

            if (string.IsNullOrEmpty(idValue) || !Guid.TryParse(idValue, out var id))
            {
                return null;
            }

            if (string.IsNullOrEmpty(entityName))
            {
                return null;
            }

            return new EntityReference(entityName, id) { Name = name };
        }

        private OptionSetValue? ParseOptionSetValue(string? value)
        {
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var optionValue))
            {
                return null;
            }

            return new OptionSetValue(optionValue);
        }
    }
}
