using System.Collections.Generic;
using System.Linq;
using DataManager.Api.v1;
using DataManager.Services;
using DataManager.Services.PackageManager.Settings;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using FluentAssertions;
using Xunit;
using XUnitTests.Mocks;

namespace XUnitTests.Services.PackageManager.Settings
{
    public class TimeSeriesSettingsTests
    {
        public TimeSeriesSettingsTests()
        {
            errorCache = new ErrorCacheMock();
            globalScope = new NestedScope {Name = Constants.PackageManagerIdentity};
            packageScope = new NestedScope(globalScope) {Name = "TestPackage"};

            AddVariable(globalScope, Constants.GlobalSettings.NoOfShards, DefaultShards);
            AddVariable(globalScope, Constants.GlobalSettings.NoOfReplicas, DefaultReplicas);
            AddVariable(globalScope, Constants.GlobalSettings.RefreshInterval, DefaultRefreshInterval, SupportedTypes.String);
        }

        private static void AddVariable(NestedScope scope, string name, object value, SupportedTypes type = SupportedTypes.Integer)
        {
            var variable = new Variable
            {
                PackageId = scope.Name,
                Name = name,
                Value = value.ToString(),
                Type = type.GetStringValue(),
                Units = Units.None.GetStringValue()
            };
            scope.Store(variable);
        }

        // Common Context
        private readonly ErrorCacheMock errorCache;
        private readonly NestedScope globalScope;
        private readonly NestedScope packageScope;

        // Note: these would be really bad in production! :)
        private const int DefaultShards = 55;
        private const int DefaultReplicas = 192;
        private const string DefaultRefreshInterval = "349s";

        private const int LocalShards = 4;
        private const int LocalReplicas = 1000;
        private const string LocalRefreshInterval = "-1";

        [Theory]
        [InlineData(DefaultShards, DefaultReplicas, DefaultRefreshInterval)]
        [InlineData(LocalShards, LocalReplicas, LocalRefreshInterval, true)]
        public void TestActiveSettings(int shards, int replicas, string refresh, bool overrideAtPackage = false)
        {
            if (overrideAtPackage) OverrideAtPackageScope();

            var settings = TimeSeriesSettings
                .SettingForLifestage(packageScope, errorCache, active: true)
                .ToDictionary(t => t.Item1, t => t.Item2);

            // The settings should be either global or overriden as per the test case.
            settings.Count.Should().Be(4);
            settings[Constants.GlobalSettings.NoOfShards].Should().Be(shards);
            settings[Constants.GlobalSettings.NoOfReplicas].Should().Be(replicas);
            settings[Constants.GlobalSettings.RefreshInterval].Should().Be(refresh);

            // timeSeriesRole tag should be set to active.
            AssertTimeSeriesRoleTag(settings, TimeSeriesSettings.Active);
        }

        [Fact]
        public void TestStableSettings()
        {
            var settings = TimeSeriesSettings
                .SettingForLifestage(packageScope, errorCache, active: false)
                .ToDictionary(t => t.Item1, t => t.Item2);

            // The shard count should NOT be set here and
            // the timeSeriesRole tag set to stable.
            settings.Count.Should().Be(3);
            settings.ContainsKey(Constants.GlobalSettings.NoOfShards).Should().BeFalse();
            AssertTimeSeriesRoleTag(settings, TimeSeriesSettings.Stable);
        }

        private static void AssertTimeSeriesRoleTag(Dictionary<string, object> settings, string tagValue)
        {
            var pair = settings.First(p => p.Key.Contains(TimeSeriesSettings.RoleTag));
            pair.Key.Should().Be($"{Constants.IndexRoutingAllocationInclude}{TimeSeriesSettings.RoleTag}");
            pair.Value.Should().Be(tagValue);
        }

        private void OverrideAtPackageScope()
        {
            AddVariable(packageScope, Constants.GlobalSettings.NoOfShards, LocalShards);
            AddVariable(packageScope, Constants.GlobalSettings.NoOfReplicas, LocalReplicas);
            AddVariable(packageScope, Constants.GlobalSettings.RefreshInterval, LocalRefreshInterval, SupportedTypes.String);
        }
    }
}
