using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DataManager.Schemas.v1
{
    /// <summary>
    /// A record of log activity.
    /// </summary>
    public class IngestActivity
    {
        public IngestActivity()
        {
            When = DateTime.Now;
            Indexes = new Dictionary<string, IndexActivity>();
        }

        /// <summary>
        /// When the ingestion happened.
        /// </summary>
        public DateTime When { get; set; }

        /// <summary>
        /// The duration of the ingestion (in milliseconds)
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// The count of documents indexed.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The count of documents that encountered errors.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// The aggregate size of documents indexed.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Information about specific indexes that were ingested.
        /// </summary>
        public Dictionary<string, IndexActivity> Indexes { get; set; }

        // Member Data 
        private IndexActivity cachedIndex;

        public void AddDocument(string indexName, string indexBaseName, string typeName, int size)
        {
            if (cachedIndex == null || cachedIndex.Name != indexName)
            {
                if (!Indexes.TryGetValue(indexName, out cachedIndex))
                {
                    cachedIndex = new IndexActivity
                    {
                        Name = indexName,
                        BaseName = indexBaseName
                    };
                    Indexes.Add(indexName, cachedIndex);
                }
            }

            cachedIndex.AddDocument(typeName, size);

            Count += 1;
            Size += size;
        }

        public void CompleteIngestion()
        {
            Duration = (int) DateTime.Now.Subtract(When).TotalMilliseconds;
        }
    }

    /// <summary>
    /// A record of activity for an index
    /// </summary>
    public class IndexActivity
    {
        public IndexActivity()
        {
            Types = new Dictionary<string, TypeActivity>();
        }

        [JsonIgnore]
        public string Name { get; set; }

        /// <summary>
        /// The base name of the index (i.e. without pre and suffixes).
        /// </summary>
        public string BaseName { get; set; }

        /// <summary>
        /// The count of documents indexed.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The count of documents that encountered errors.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// The aggregate size of documents indexed.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Information about specific types for the index.
        /// </summary>
        public Dictionary<string, TypeActivity> Types { get; set; }

        // Member data
        private TypeActivity cachedType;

        public void AddDocument(string typeName, int size)
        {
            if (cachedType == null || cachedType.Name != typeName)
            {
                if (!Types.TryGetValue(typeName, out cachedType))
                {
                    cachedType = new TypeActivity {Name = typeName};
                    Types.Add(typeName, cachedType);
                }
            }

            cachedType.AddDocument(size);

            Count += 1;
            Size += size;
        }
    }

    /// <summary>
    /// A record of activity for a type in an index
    /// </summary>
    public class TypeActivity
    {
        [JsonIgnore]
        public string Name { get; set; }

        /// <summary>
        /// The count of documents indexed.
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// The count of documents that encountered errors.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// The aggregate size of documents indexed.
        /// </summary>
        public int Size { get; set; }

        public void AddDocument(int size)
        {
            Count += 1;
            Size += size;
        }
    }
}
