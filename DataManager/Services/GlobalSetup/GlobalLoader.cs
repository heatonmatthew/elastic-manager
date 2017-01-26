using System.Collections.Generic;
using System.Linq;
using DataManager.Api.v1;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataManager.Services.GlobalSetup
{
    /// <summary>
    /// Handles loading the global variables.
    /// </summary>
    public class GlobalLoader
    {
        public GlobalLoader(IErrorCache errorCache)
        {
            ErrorCache = errorCache;
        }

        public IErrorCache ErrorCache { get; set; }

        class NestedData
        {
            [JsonProperty("variables")]
            public Dictionary<string, JToken> Variables { get; set; }

            [JsonProperty("scoped")]
            public Dictionary<string, NestedData> SubScopes { get; set; }
        }

        public IDictionary<string, NestedScope> LoadFromElastic(IList<Variable> variables, IDictionary<string, NestedScope> existing = null)
        {
            var scopes = NestedScope.BuildHeirarchy(variables.Select(v => v.PackageId), ErrorCache, existing);
            foreach (var variable in variables)
            {
                scopes[variable.PackageId].Store(variable);
            }
            return scopes;
        }

        public void MergeDefaults(string defaultJson, IDictionary<string, NestedScope> existing)
        {
            var data = JsonConvert.DeserializeObject<NestedData>(defaultJson);
            var variables = LoadData(data, Constants.PackageManagerIdentity).ToList();

            // Complete the hierarchy and then merge the variables into the hierarchy
            var scopes = NestedScope.BuildHeirarchy(variables.Select(v => v.PackageId), ErrorCache, existing);
            foreach (var variable in variables)
            {
                var varRef = new VariableRef {PackageId = variable.PackageId, VariableName = variable.Name};
                if (scopes[variable.PackageId].TryFindVariable(varRef, out var existingVariable))
                {
                    existingVariable.Default = variable.Default;
                    if (existingVariable.Type != variable.Type)
                    {
                        existingVariable.Type = variable.Type;
                        existingVariable.Value = null;
                    }
                }
                else
                {
                    scopes[variable.PackageId].Store(variable);
                }
            }
        }

        private IEnumerable<Variable> LoadData(NestedData data, string packageId)
        {
            // load the variables
            foreach (var pair in data.Variables)
            {
                var value = CreateFromJToken(pair.Value);
                if (value == null) continue;

                value.PackageId = packageId;
                value.Name = pair.Key;
                yield return value;
            }

            // Load any child scopes
            foreach (var pair in data.SubScopes)
            {
                foreach (var variable in LoadData(pair.Value, $"{packageId}.{pair.Key}"))
                {
                    yield return variable;
                }
            }
        }

        private Variable CreateFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return new Variable
                    {
                        Default = token.Value<int>().ToString(),
                        Units = Units.None.GetStringValue(),
                        Type = SupportedTypes.Integer.GetStringValue()
                    };
                    
                case JTokenType.String:
                    var strValue = token.Value<string>();
                    if (Constant.TryParseEncodedToVariable(strValue, out var result))
                    {
                        result.Value = null;
                        return result;
                    }

                    return new Variable
                    {
                        Default = strValue,
                        Units = Units.None.GetStringValue(),
                        Type = SupportedTypes.String.GetStringValue()
                    };

                default:
                    ErrorCache.AddError($"Unhandled JTokenType '{token.Type}' parsed from data file");
                    return null;
            }
        }
    }
}
