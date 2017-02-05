using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataManager.Elastic;
using DataManager.Services;
using DataManager.Services.GlobalSetup;
using DataManager.Services.PackageManager;
using FluentAssertions;
using Nest;
using XIntegrationTests.Utilities;
using Xunit;

namespace XIntegrationTests
{
    public class BootstrapTests : LocalEsTests, IScriptLoader
    {
        public BootstrapTests()
        {
            repository = new Repository {Client = client};
            bootstrap = new Bootstrap(repository, this, errorCache);
        }

        // Member data
        private readonly Repository repository;
        private Bootstrap bootstrap;

        string IScriptLoader.LoadGlobalDataJson()
        {
            return @"
                    {
                      // Global default variables
                      ""variables"": {
                        ""number_of_shards"": 3,
                        ""number_of_replicas"": 2
                      },
                      ""scoped"": {
                        // Override default for timeseries data
                        ""timeseries"": {
                          ""variables"": {
                            ""number_of_replicas"": 1,
                            ""refresh_interval"": ""60s"",
                          }
                        },
                        // Override default for unmanaged data
                        ""unmanaged"": {
                          ""variables"": {
                            ""number_of_replicas"": 3
                          }
                        }
                      }
                    }";
        }

        IDictionary<string, TemplateMapping> IScriptLoader.LoadTemplates()
        {
            return new Dictionary<string, TemplateMapping>
            {
                {
                    Constants.ControlIndex + "-template", new TemplateMapping
                    {
                        Template = Constants.ControlIndex,
                        Settings = new IndexSettings
                        {
                            NumberOfShards = 1,
                            NumberOfReplicas = 1,
                            RefreshInterval = new Time(TimeSpan.FromMilliseconds(1))
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task TestBootstrapWorks()
        {
            await bootstrap.PrepareGlobalPackageDataAsync();
            await bootstrap.SetupPackageManagerSchemaAsync();
            await bootstrap.LoadGlobalPackageDataAsync();

            // TODO - really need to actually do a Refresh on ES rather than a sleep :(
            Thread.Sleep(100);

            var variables = (await repository
                    .VariablesByPackageAsync(Constants.PackageManagerIdentity))
                .ToList();

            variables.Count.Should().Be(5);
            variables
                .First(v => v.Name == "number_of_shards")
                .Default.Should().Be("3");
        }

        [Fact]
        public async void TestBootstrapMultipleTimesWorks()
        {
            // The bootstrap process should be repeatable.
            await TestBootstrapWorks();

            bootstrap = new Bootstrap(repository, this, errorCache);
            await TestBootstrapWorks();

            bootstrap = new Bootstrap(repository, this, errorCache);
            await TestBootstrapWorks();
        }
    }
}
