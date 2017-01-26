using System;
using System.Collections.Generic;
using System.Linq;
using DataManager.Api.v1;
using DataManager.Schemas.v1.Jobs;
using DataManager.Services.Scoping;

namespace DataManager.Services.PackageManager
{
    /// <summary>
    /// Handles load package data from json, then validating 
    /// and loading it into ElasticSearch.
    /// </summary>
    public class PackageLoader
    {
        public PackageLoader()
        {
            Jobs = new List<JobBase>();
        }

        public NestedScope Scope { get; set; }
        public IErrorCache ErrorCache { get; set; }

        // Extracted information
        public List<JobBase> Jobs { get; }

        // Member data
        private Package package;

        public bool ValidatePackageToLoad(Package package)
        {
            if (this.package != null) throw new InvalidOperationException("This loader already has a package.");

            // Store details of the package
            this.package = package;

            // Check for any reserved package names (etc.)
            if (package.Id.StartsWith(Constants.PackageId))
            {
                ErrorCache.AddError($"Package can't start with the reserved name {Constants.GlobalPackageId}");
            }

            // Check the variables and load them
            CheckForDuplicateNames(package.Variables, v => v.Name, "Variables");
            foreach (var variable in package.Variables)
            {
                variable.PackageId = package.Id;
                Scope.Store(variable);
            }

            // Check the datasets and load them
            CheckForDuplicateNames(package.DataSets, d => d.Name, "Datasets");

            var validator = new DataSetApiValidator
            {
                PackageId = package.Id,
                Scope = Scope,
                ErrorCache = ErrorCache
            };
            foreach (var dataSet in package.DataSets)
            {
                Jobs.AddRange(validator.DetermineJobs(dataSet));
            }

            return ErrorCache.Count == 0;
        }

        public bool CheckForDuplicateNames<T>(IEnumerable<T> items, Func<T, string> nameExtractor, string whatItemsAre)
        {
            var grouped = from v in items
                group v by nameExtractor(v).ToLowerInvariant()
                into lowerCaseGrouped
                where lowerCaseGrouped.Count() > 1
                select Tuple.Create(lowerCaseGrouped.Key, lowerCaseGrouped.Count());

            var any = false;
            foreach (var duplicate in grouped)
            {
                any = true;
                ErrorCache.AddError($"There are {duplicate.Item2} {whatItemsAre} with name '{duplicate.Item1}'");
            }
            return any;
        }
    }
}
