using System;
using System.Collections.Generic;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;

namespace DataManager.Services.PackageManager.Settings
{
    /// <summary>
    /// We tag nodes for active and/or stable indicies and use the routing.allocation
    /// settings to get ElasticSearch to place them in the appropriate place.
    /// </summary>
    /// <remarks>
    /// (see https://www.elastic.co/guide/en/elasticsearch/reference/2.3/shard-allocation-filtering.html)
    /// </remarks>
    public static class TimeSeriesSettings
    {
        // This is the name of the metadata attribute to apply to the ES nodes.
        public const string RoleTag = "timeSeriesRole";

        // These are valid values for the attribute
        public const string Active = "active";      // data under change (i.e. SSDs for storage)
        public const string Stable = "stable";      // data that isn't changing

        public static IEnumerable<Tuple<string, object>> SettingForLifestage(NestedScope scope, IErrorCache errorCache, bool active)
        {
            yield return Tuple.Create(
                $"{Constants.IndexRoutingAllocationInclude}{RoleTag}",
                active ? (object) Active : Stable);

            if (active)
            {
                yield return scope.ProvideSetting<int>(Constants.GlobalSettings.NoOfShards, errorCache);
            }

            // MAYBE want to be able to support different values for active and stable ???
            yield return scope.ProvideSetting<int>(Constants.GlobalSettings.NoOfReplicas, errorCache);
            yield return scope.ProvideSetting<string>(Constants.GlobalSettings.RefreshInterval, errorCache);
        }

        private static Tuple<string, object> ProvideSetting<T>(this NestedScope scope, string settingName, IErrorCache errorCache)
        {
            var variable = scope.FirstVarOfName(settingName, errorCache);
            return variable.ProvideSetting<T>(settingName, errorCache);
        }

        private static Tuple<string, object> ProvideSetting<T>(this VariableInfo varInfo, string settingName, IErrorCache errorCache)
        {
            var value = varInfo.GetValueOrDefault<T>(errorCache);
            return Tuple.Create(settingName, (object) value);
        }
    }
}
