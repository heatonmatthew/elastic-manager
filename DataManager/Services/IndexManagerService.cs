using System;
using Nest;

namespace DataManager.Services
{
    /// <summary>
    /// Interface to a service that provides management of ElasticSearch Indexes
    /// based on the loaded packages.
    /// </summary>
    public interface IIndexManagerService
    {
        string FormatIndexName(string baseName, int schemaVersion, DateTime time);
    }

    public class IndexManagerService
    {
        public IndexManagerService(IElasticClient client)
        {
            this.client = client;
        }

        // Members 
        private readonly IElasticClient client;

        public string FormatIndexName(string baseName, int schemaVersion, DateTime time)
        {
            return $"{baseName}-v{schemaVersion}-{time:yyyy.MM.dd}";
        }
    }
}
