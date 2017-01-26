using System.Collections.Generic;
using DataManager.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DataManager.Api.v1
{
    public enum PartitionGrain
    {
        None,
        PerDay,
        //PerWeek,
        //PerMonth
    }

    public class DataSet
    {
        [JsonProperty("schema-version")]
        public Version SchemaVersion { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("grain")]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public PartitionGrain Grain { get; set; }

        [JsonProperty("query-uri")]
        public string QueryUri { get; set; }

        [JsonProperty("template-definition")]
        public TemplateInfo Template { get; set; }

        [JsonProperty("management-settings")]
        public ManagementInfo Manage { get; set; }

        public class TemplateInfo
        {
            /// <summary>
            /// Relative order of template application (see ElasticSearch documentation)
            /// </summary>
            [JsonProperty("order", NullValueHandling = NullValueHandling.Ignore)]
            public int? Order { get; set; }
            
            /// <summary>
            /// Any custom settings for this dataset.
            /// </summary>
            [JsonProperty("settings")]
            public Dictionary<string, JToken> Settings { get; set; }
            
            /// <summary>
            /// Any custom aliases for this dataset.  If not set, will
            /// default to an alias called the dataset name.
            /// </summary>
            [JsonProperty("aliases")]
            public Dictionary<string, JToken> Aliases { get; set; }
            
            /// <summary>
            /// Default type mappings for the indexes.
            /// </summary>
            [JsonProperty("mappings")]
            public Dictionary<string, JToken> Mappings { get; set; }
        }

        /// <summary>
        /// Information about how the data set is maintained (retention, archival etc.)
        /// </summary>
        public class ManagementInfo
        {
            /// <summary>
            /// Length of time until an index is assumed to be readonly.
            /// </summary>
            [JsonProperty(Constants.GlobalSettings.ActiveAge)]
            public SettingValue ActiveAge { get; set; }

            /// <summary>
            /// Length of time that an index have data kept for.
            /// </summary>
            [JsonProperty(Constants.GlobalSettings.RetentionPeriod)]
            public SettingValue RetentionAge { get; set; }

            [JsonProperty(Constants.GlobalSettings.ArchiveTo)]
            public SettingValue ArchiveUri { get; set; }
        }
    }
}
