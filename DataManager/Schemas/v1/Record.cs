using System;

namespace DataManager.Schemas.v1
{
    /// <summary>
    /// A log entry with the mandatory fields for all index/types.
    /// </summary>
    public class Record
    {
        public string Id { get; set; }
        public DateTime Time { get; set; }
        public int SchemaVersion { get; set; }
        public string IndexName { get; set; }
        public string TypeName { get; set; }
        public string JsonDocument { get; set; }
    }
}
