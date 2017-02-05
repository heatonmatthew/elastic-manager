using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using DataManager.Api.v1;
using Nest;
using XIntegrationTests.Utilities;
using Xunit;

namespace XIntegrationTests.Api.v1
{
    public class VariableTests : LocalEsTests
    {
        public VariableTests()
        {
            client.CreateIndex("test", d => d
                .Mappings(s => s
                    .Map<Variable>(m => m.AutoMap())));
        }

        [Fact]
        public void IndexTest()
        {
            var v = new Variable
            {
                PackageId = "flskdjf",
                Name = "name"
            };

            client.Index(v, s => s.Index("test"));
        }

        [Fact]
        public void BulkIndexTest()
        {
            var objs = new[]
            {
                new Variable {PackageId = "pack", Name = "name1"},
                new Variable {PackageId = "pack", Name = "name2"},
                new Variable {PackageId = "pack", Name = "name3"},
                new Variable {PackageId = "pack", Name = "name4"},
                new Variable {PackageId = "pack2", Name = "black"}
            };

            var bulk = new BulkDescriptor();
            bulk.IndexMany(objs, 
                (d, v) => d
                .Index("test")
                //.Type("variable")
                .Document(v));

            var objs2 = new[]
{
                new Variable {PackageId = "pack4", Name = "name1"},
                new Variable {PackageId = "pack4", Name = "name2"},
            };

            bulk.IndexMany(objs2,
                (d, v) => d
                .Index("test")
                //.Type("variable")
                .Document(v));

            bulk.DeleteMany(new Variable[0], (d, v) => d.Index("test").Id("1"));

            client.Bulk(bulk);
        }
    }
}
