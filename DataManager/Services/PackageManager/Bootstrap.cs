using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataManager.Api.v1;
using DataManager.Elastic;
using DataManager.Services.GlobalSetup;
using DataManager.Services.Scoping;
using Nest;

namespace DataManager.Services.PackageManager
{
    /// <summary>
    /// This class bootstraps the setup of the PackageManager's necessary
    /// Indexes and data.
    /// </summary>
    public class Bootstrap
    {
        public Bootstrap(IRepository repository, IScriptLoader scriptLoader, IErrorCache errorCache)
        {
            Repository = repository;
            ScriptLoader = scriptLoader;
            ErrorCache = errorCache;

            globalLoader = new GlobalLoader(errorCache);
            cache = new PackageCache(errorCache);
        }

        public IErrorCache ErrorCache { get; set; }
        public IRepository Repository { get; set; }
        public IScriptLoader ScriptLoader { get; set; }

        // Member data
        private readonly GlobalLoader globalLoader;
        private readonly PackageCache cache;

        private IEnumerable<ICreateIndexRequest> GetDesiredIndexes()
        {
            yield return new CreateIndexDescriptor(Constants.ControlIndex)
                .Mappings(s => s
                    .Map<Variable>(m => m.AutoMap()));
        }

        #region Schema Preparation

        public async Task SetupPackageManagerSchemaAsync()
        {
            await PrepareTemplatesAsync();
            await PrepareIndexesAsync();
        }

        private async Task PrepareTemplatesAsync()
        {
            var existing = await Repository.GetIndexTemplatesAsync(Constants.PackageManager + ".*");
            var desired = ScriptLoader.LoadTemplates();

            var correlation = desired.CorrelateWith(existing);
            await Repository.CreateIndexTemplatesAsync(desired);
            await Repository.DeleteIndexTemplatesAsync(correlation.NonSourceItems.Select(i => i.Key));
        }

        private async Task PrepareIndexesAsync()
        {
            // Match up desired indexes to existing ones (allow for versioned/migrated name extensions)
            var existing = await Repository.GetIndexesAsync(Constants.PackageManager + ".*");
            foreach (var desired in GetDesiredIndexes())
            {
                var matchingKey = existing.Keys.FirstOrDefault(k => k.StartsWith(desired.Index.Name));
                if (matchingKey == null)
                {
                    await Repository.CreateIndexAsync(desired);
                }
                // TODO - Should I check types/mappings on existing indexes ?!
            }

            // Don't delete any old indexes because we might need to migrate the data ;)
        }

        #endregion

        #region Data Preparation

        public async Task PrepareGlobalPackageDataAsync()
        {
            // Load the original state
            var scopes = await LoadFromElasticAsync();
            var topScope = scopes.Values.FirstOrDefault(s => s.IsTopMost);
            cache.SetOriginalState(topScope ?? new NestedScope {Name = Constants.PackageManagerIdentity});

            // Now merge in the Global defaults to the CurrentState clone (of the original)
            MergeDefaults(cache.CurrentState.ToDictionary());
        }

        public async Task LoadGlobalPackageDataAsync()
        {
            // Setup a bulk operation to record the changes
            var bulk = new BulkDescriptor();
            cache.RecordChanges(bulk);
            await Repository.SaveAllAsync(bulk);
        }

        private void MergeDefaults(IDictionary<string, NestedScope> existing)
        {
            var json = ScriptLoader.LoadGlobalDataJson();
            globalLoader.MergeDefaults(json, existing);
        }

        private async Task<IDictionary<string, NestedScope>> LoadFromElasticAsync()
        {
            var variables = await Repository.VariablesByPackageAsync(Constants.PackageManagerIdentity);
            var scopes = globalLoader.LoadFromElastic(variables.ToList());

            // TODO - more data types ?!

            return scopes;
        }

        #endregion
    }
}
