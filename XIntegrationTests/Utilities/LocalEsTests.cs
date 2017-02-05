using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Elasticsearch.Net;
using Nest;

namespace XIntegrationTests.Utilities
{
    public class LocalEsTests : IDisposable
    {
        public LocalEsTests()
        {
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                .DisableDirectStreaming(true)
                .OnRequestDataCreated(RecordRequest);

            client = new ElasticClient(settings);
            errorCache = new ErrorCacheMock();

            CleanUp(client);
        }

        private static void CleanUp(ElasticClient client)
        {
            client.DeleteIndex(Indices.All);
            foreach (var name in client.GetIndexTemplate().TemplateMappings.Keys)
            {
                client.DeleteIndexTemplate(name);
            }
        }

        private void RecordRequest(RequestData request)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{request.Method} {request.Path}");

            if (request.PostData != null)
            {
                using (var stream = new MemoryStream())
                {
                    request.PostData.Write(stream, request.ConnectionSettings);
                    stream.Position = 0;

                    using (var reader = new StreamReader(stream))
                    {
                        builder.AppendLine(reader.ReadToEnd());
                    }
                }
            }
            recordedRequests.Add(builder.ToString());
        }

        // Member data
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        protected readonly List<string> recordedRequests = new List<string>();
        protected readonly ElasticClient client;
        protected readonly ErrorCacheMock errorCache;

        protected T RegisterDisposable<T>(T disposable)
            where T : IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }

        public void Dispose()
        {
            foreach (var d in disposables)
            {
                d.Dispose();
            }
            disposables.Clear();

            // TODO - undo
            //CleanUp(client);
        }
    }
}
