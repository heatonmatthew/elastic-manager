using System;
using DataManager.Services;
using Nest;
using Newtonsoft.Json;

namespace DataManager.Api.v1
{
    /// <summary>
    /// A variable that can be configured for the system.
    /// </summary>
    [ElasticsearchType(Name = "variable", IdProperty = "id")]
    public class Variable
    {
        [String(Name = "id", Index = FieldIndexOption.NotAnalyzed)]
        public string Id { get; set; }
        [String(Name = Constants.PackageId, Index = FieldIndexOption.NotAnalyzed)]
        public string PackageId { get; set; }
        [String(Name = Constants.VariableName, Index = FieldIndexOption.NotAnalyzed)]
        public string Name { get; set; }
        [String(Name = "value", Index = FieldIndexOption.NotAnalyzed)]
        public string Value { get; set; }
        [String(Name = "type", Index = FieldIndexOption.NotAnalyzed)]
        public string Type { get; set; }
        [String(Name = Constants.Default, Index = FieldIndexOption.NotAnalyzed)]
        public string Default { get; set; }
        [String(Name = "max", Index = FieldIndexOption.NotAnalyzed)]
        public string Max { get; set; }
        [String(Name = "min", Index = FieldIndexOption.NotAnalyzed)]
        public string Min { get; set; }
        [String(Name = "units", Index = FieldIndexOption.NotAnalyzed)]
        public string Units { get; set; }  // TODO - I don't think this should be here !!!
        [String(Name = "is-generated")]
        public bool? IsGenerated { get; set; }
        [String(Name = "edit-uri", Index = FieldIndexOption.NotAnalyzed)]
        public string EditUri { get; set; }

        public bool IsChanged(Variable other)
        {
            return other == null ||
                   Value != other.Value ||
                   Type != other.Type ||
                   Default != other.Default ||
                   Max != other.Max ||
                   Min != other.Min ||
                   Units != other.Units ||
                   IsGenerated != other.IsGenerated ||
                   EditUri != other.EditUri;
        }

        public Variable Clone()
        {
            return new Variable
            {
                Id = Id,
                PackageId = PackageId,
                Name = Name,
                Value = Value,
                Type = Type,
                Default = Default,
                Max = Max,
                Min = Min,
                Units = Units,
                IsGenerated = IsGenerated,
                EditUri = EditUri
            };
        }
    }

    /// <summary>
    /// Refers to a variable.
    /// </summary>
    public class VariableRef
    {
        public string PackageId { get; set; }
        public string VariableName { get; set; }
        public object DefaultValue { get; set; }

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(VariableName);

        public string ToFullName()
        {
            return $"{PackageId}::{VariableName}";
        }

        public static VariableRef Parse(string fullName, object defaultValue = null)
        {
            if (!TryParse(fullName, out var result, defaultValue))
            {
                throw new ArgumentOutOfRangeException(nameof(fullName), fullName, @"Variable name was not correctly formatted (should be PACKAGEID::VARIABLENAME)");
            }
            return result;
        }

        public static bool TryParse(string fullName, out VariableRef result, object defaultValue = null)
        {
            var parts = fullName.Split(new[] {"::"}, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                result = null;
                return false;
            }

            result = new VariableRef
            {
                PackageId = parts[0],
                VariableName = parts[1],
                DefaultValue = defaultValue
            };

            return true;
        }
    }
}
