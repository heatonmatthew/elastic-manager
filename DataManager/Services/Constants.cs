using System;
using System.Collections.Generic;
using System.Text;

namespace DataManager.Services
{
    /// <summary>
    /// Defines constants used throughout the data manager.
    /// </summary>
    public static class Constants
    {
        // Current version
        // TODO - I think I want to go with semantic versioning here
        public const string CurrentVersion = "1";

        // Reserved identifiers.
        public const string GlobalPackageId = "Global.v1";

        // Shared automation identities
        public const string PackageManager = "PackageManager";
        public const string PackageManagerIdentity = PackageManager + ".v" + CurrentVersion;

        // PackageManager Index Names
        public const string ControlIndex = PackageManagerIdentity + ".Control";
        public const string OpsLogIndex = PackageManagerIdentity + ".OpsLog";

        // Shared json property names
        public const string PackageId = "package-id";
        public const string VariableName = "variable-name";
        public const string Default = "default";

        /// <summary>
        /// Global variable names that packages can default to.
        /// </summary>
        /// <remarks>
        /// The package manager will install with global defaults and may
        /// expose some of them to allow system tuning.
        /// </remarks>
        public static class GlobalSettings
        {
            public const string ActiveAge = "active-age";
            public const string RetentionPeriod = "retention-period";
            public const string ArchiveTo = "archive-to";

            // These match ElasticSearch setting names
            public const string NoOfShards = "number_of_shards";
            public const string NoOfReplicas = "number_of_replicas";
            public const string RefreshInterval = "refresh_interval";
        }

        // Shard allocation filtering
        // (see https://www.elastic.co/guide/en/elasticsearch/reference/2.3/shard-allocation-filtering.html)
        public const string IndexRoutingAllocationInclude = "index.routing.allocation.include.";
    }
}
