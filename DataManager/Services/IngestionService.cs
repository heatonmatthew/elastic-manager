using System.Collections.Generic;
using System.Threading.Tasks;
using DataManager.Schemas.v1;
using Elasticsearch.Net;
using Nest;

namespace DataManager.Services
{
    /// <summary>
    /// Interface to a service that ingests data into an ElasticSearch Index.
    /// </summary>
    public interface IIngestionService
    {
        int BatchSize { get; set; }
        Task Ingest(IEnumerable<Record> entries);
    }

    public class IngestionService : IIngestionService
    {
        public IngestionService(IElasticClient client, IIndexManagerService indexManager, IActivityLoggingService activityLogger, int batchSize = DefaultBatchSize)
        {
            this.client = client;
            this.indexManager = indexManager;
            this.activityLogger = activityLogger;

            BatchSize = batchSize;
        }

        // Constants
        public const int DefaultBatchSize = 1000;

        // Members
        private readonly IElasticClient client;
        private readonly IIndexManagerService indexManager;
        private readonly IActivityLoggingService activityLogger;

        public int BatchSize { get; set; }

        public async Task Ingest(IEnumerable<Record> entries)
        {
            var activity = new IngestActivity();
            var operations = new List<IBulkOperation>(BatchSize);
            foreach (var entry in entries)
            {
                var indexName = GetIndex(entry);
                operations.Add(new BulkIndexOperation<object>(entry.JsonDocument)
                {
                    Id = !string.IsNullOrWhiteSpace(entry.Id) ? new Id(entry.Id) : null,
                    Index = indexName,
                    Type = entry.TypeName
                });
                activity.AddDocument(indexName, entry.IndexName, entry.TypeName, entry.JsonDocument.Length);

                if (operations.Count == BatchSize)
                {
                    await IngestBatch(operations, activity);
                    operations.Clear();
                    activity = new IngestActivity();
                }
            }

            if (operations.Count > 0)
            {
                await IngestBatch(operations, activity);
            }
        }

        private async Task IngestBatch(IList<IBulkOperation> indexOperations, IngestActivity activity)
        {
            var request = new BulkRequest
            {
                Refresh = false,
                Operations = indexOperations
            };

            var result = await client.BulkAsync(request);
            foreach (var item in result.ItemsWithErrors)
            {
                // TODO
            }

            activity.CompleteIngestion();
            activityLogger.LogActivity(activity);
        }

        private string GetIndex(Record entry)
        {
            return indexManager.FormatIndexName(entry.IndexName, entry.SchemaVersion, entry.Time);
        }
    }
}
