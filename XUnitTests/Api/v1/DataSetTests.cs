using DataManager.Api.v1;
using DataManager.Services.Variables;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Version=DataManager.Api.v1.Version;

namespace XUnitTests.Api.v1
{
    public class DataSetTests
    {
        [Fact]
        public void DeserializeTest()
        {
            const string json = @"
                {
                    ""schema-version"": ""2012.55.1"",
                    ""name"": ""itl-log"",
                    ""description"": ""Data from the STREAMS interlocking service."",
                    ""grain"": ""perDay"",
                    ""query-uri"": ""/ITL/Search/v1"",
                    ""template-definition"": 
                    {
                        ""order"" : 0,
                        ""settings"" : {
                            ""number_of_shards"" : 1
                        },
                        ""mappings"" : {
                            ""type1"" : {
                                ""_source"" : { ""enabled"" : false }
                            }
                        },
                        ""aliases"" : {
                            ""alias1"" : { },
                            ""alias2"" : {
                                    ""filter"" : {
                                        ""term"" : { ""user"" : ""kimchy"" }
                                    },
                                ""routing"" : ""kimchy""
                            },
                            ""{index}-alias"" : {}
                        }
                    },
                    ""management-settings"":
                    {
                        ""active-age"": ""1 day"",
                        ""retention-period"": ""ITL Log Retention Period""
                    }
                }";

            var dataSet = JsonConvert.DeserializeObject<DataSet>(json);

            // Descriptive properties
            dataSet.SchemaVersion.Should().Be(new Version {Major = 2012, Minor = 55, Revision = 1});
            dataSet.Name.Should().Be("itl-log");
            dataSet.Description.Should().Be("Data from the STREAMS interlocking service.");
            dataSet.Grain.Should().Be(PartitionGrain.PerDay);
            dataSet.QueryUri.Should().Be("/ITL/Search/v1");

            // Template definition
            var template = dataSet.Template;
            template.Order.Should().Be(0);
            template.Settings.Count.Should().Be(1);
            ((string) template.Settings["number_of_shards"]).Should().Be("1");
            template.Mappings.Count.Should().Be(1);
            template.Mappings["type1"].Should().NotBeNull();
            template.Aliases.Count.Should().Be(3);
            template.Aliases.Keys.Should().Contain(new[] {"alias1", "alias2", "{index}-alias"});

            // Management settings
            var manage = dataSet.Manage;
            manage.ActiveAge.ConstantValue.Value.Should().Be(1);
            manage.ActiveAge.ConstantValue.Units.Should().Be(Units.Days);
            manage.RetentionAge.FromVariable.VariableName.Should().Be("ITL Log Retention Period");
            manage.RetentionAge.FromVariable.PackageId.Should().BeNull();
        }
    }
}
