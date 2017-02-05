using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataManager.Api.v1;
using DataManager.Services;
using Nest;

namespace DataManager.Elastic
{
    public interface IRepository
    {
        Task<IDictionary<string, TemplateMapping>> GetIndexTemplatesAsync(Names names);
        Task CreateIndexTemplatesAsync(IDictionary<string, TemplateMapping> templates);
        Task DeleteIndexTemplatesAsync(IEnumerable<string> names);

        Task<IDictionary<string, IndexState>> GetIndexesAsync(Indices indices);
        Task CreateIndexAsync(ICreateIndexRequest indexRequest);

        Task<IEnumerable<Variable>> VariablesByPackageAsync(string packageId);
        Task<bool> SaveAllAsync(BulkDescriptor descriptor);
    }

    // TODO - I have to put (or inject) error handling on all the calls.
    // TODO - Will throw exceptions if there are problems.
    public class Repository : IRepository
    {
        public IElasticClient Client { get; set; }

        public async Task<IDictionary<string, TemplateMapping>> GetIndexTemplatesAsync(Names names)
        {
            var response = await Client.GetIndexTemplateAsync(new GetIndexTemplateRequest(names));
            return response.TemplateMappings;
        }

        public async Task CreateIndexTemplatesAsync(IDictionary<string, TemplateMapping> templates)
        {
            foreach (var pair in templates)
            {
                var request = new PutIndexTemplateRequest(pair.Key)
                {
                    Template = pair.Value.Template,
                    Order = pair.Value.Order,
                    Settings = pair.Value.Settings,
                    Mappings = pair.Value.Mappings,
                    Aliases = pair.Value.Aliases,
                };
                var response = await Client.PutIndexTemplateAsync(request);
            }
        }

        public async Task DeleteIndexTemplatesAsync(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                var response = await Client.DeleteIndexTemplateAsync(name);
            }
        }

        public async Task<IDictionary<string, IndexState>> GetIndexesAsync(Indices indices)
        {
            var response = await Client.GetIndexAsync(new GetIndexRequest(indices));
            return response.Indices;
        }

        public async Task CreateIndexAsync(ICreateIndexRequest indexRequest)
        {
            var response = await Client.CreateIndexAsync(indexRequest);
        }

        public async Task<IEnumerable<Variable>> VariablesByPackageAsync(string packageId)
        {
            var result = await Client.SearchAsync<Variable>(
                d => d.Index(Constants.ControlIndex)
                    .Size(1000)
                    .Query(q => q.Prefix("package-id", packageId)));

            return result.Documents;
        }

        public async Task<bool> SaveAllAsync(BulkDescriptor descriptor)
        {
            var result = await Client.BulkAsync(descriptor);
            return !result.Errors;
        }
    }
}
