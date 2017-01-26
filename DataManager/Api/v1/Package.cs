using System.Collections.Generic;

namespace DataManager.Api.v1
{
    public class Package
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public int PackageVersion { get; set; }
        public int DataManagerVersion { get; set; }
        public List<Variable> Variables { get; set; }
        public List<DataSet> DataSets { get; set; }
    }
}
