using DataManager.Api.v1;
using DataManager.Services;
using DataManager.Services.GlobalSetup;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using FluentAssertions;
using Xunit;
using XUnitTests.Mocks;

namespace XUnitTests.Services.GlobalSetup
{
    public class ScriptLoaderTests
    {
        public ScriptLoaderTests()
        {
            scope = new NestedScope();
            errorCache = new ErrorCacheMock();

            loader = new ScriptLoader
            {
                Scope = scope,
                ErrorCache = errorCache
            };

            scope.Store(new Variable
            {
                PackageId = Constants.GlobalPackageId,
                Name = "var1",
                Default = "1",
                Type = SupportedTypes.Integer.GetStringValue(),
                Units = Units.None.GetStringValue()
            });

            scope.Store(new Variable
            {
                PackageId = Constants.GlobalPackageId,
                Name = "var_2",
                Default = "60s",
                Type = SupportedTypes.String.GetStringValue(),
                Units = Units.None.GetStringValue()
            });
        }

        // Member data
        private readonly NestedScope scope;
        private readonly IErrorCache errorCache;
        private readonly ScriptLoader loader;

        [Fact]
        public void TestSubstituteVariablesWorks()
        {
            const string sampleScript = @"abc: @@var1@@,
                                          number_of_shards = @@var_2@@";
            const string expectedScri = @"abc: 1,
                                          number_of_shards = ""60s""";

            var result = loader.SubstituteVariables(sampleScript);
            result.Should().Be(expectedScri);
        }
    }
}
