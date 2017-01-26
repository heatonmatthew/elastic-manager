using System;
using DataManager.Api.v1;
using EnumStringValues;

namespace DataManager.Services.Variables
{
    public enum SupportedTypes
    {
        [StringValue("integer", true),
         StringValue("int")]
        Integer,
        [StringValue("string", true),
         StringValue("str")]
        String
    }

    public enum Units
    {
        [StringValue("none", true)]
        None,
        [StringValue("days", true),
         StringValue("day"),
         StringValue("d"),
         StringValue("D")]
        Days,
        [StringValue("weeks", true),
         StringValue("week"),
         StringValue("w"),
         StringValue("W")]
        Weeks,
        [StringValue("months", true),
         StringValue("month"),
         StringValue("M"),
         StringValue("MM")]
        Months,
        [StringValue("years", true),
         StringValue("year"),
         StringValue("y"),
         StringValue("Y")]
        Years
    }

    /// <summary>
    /// Handles the understanding of extended information about variables.
    /// </summary>
    public class VariableInfo
    {
        private VariableInfo(Variable variable, string packageId)
            : this(variable, new VariableRef {PackageId = packageId, VariableName = variable.Name})
        {
        }

        private VariableInfo(Variable variable, VariableRef varRef)
        {
            if (variable.Name != varRef.VariableName) throw new ArgumentException("Name of variable and reference don't match.");

            Variable = variable;
            Ref = varRef;
        }

        public static VariableInfo Extract(Variable variable, string packageId, IErrorCache errorCache)
        {
            var result = new VariableInfo(variable, packageId);
            int preCount = errorCache.Count;

            result.SupportedType = result.Parse<SupportedTypes>(variable.Type, nameof(variable.Type), errorCache);
            result.Units = result.Parse<Units>(variable.Units, nameof(variable.Units), errorCache);
            result.ParseValues(errorCache);
            result.HasErrors = errorCache.Count != preCount;

            return result;
        }

        #region Parsing code

        private TEnum? Parse<TEnum>(string value, string fieldName, IErrorCache errorCache)
            where TEnum : struct, IConvertible
        {
            if (!string.IsNullOrWhiteSpace(value) && value.TryParseStringValueToEnum(out TEnum result))
            {
                return result;
            }

            errorCache.AddError($"Error parsing {fieldName} for variable");
            return null;
        }

        private void ParseValues(IErrorCache errorCache)
        {
            if (!SupportedType.HasValue) return;

            switch (SupportedType.Value)
            {
                case SupportedTypes.Integer:
                    Value = ParseInteger(Variable.Value, errorCache);
                    Default = ParseInteger(Variable.Default, errorCache);
                    Max = ParseInteger(Variable.Max, errorCache);
                    Min = ParseInteger(Variable.Min, errorCache);
                    break;
                case SupportedTypes.String:
                    Value = Variable.Value;
                    Default = Variable.Default;
                    break;
                default:
                    errorCache.AddError($"Unable to parse unhandled type: {SupportedType.Value}");
                    break;
            }
        }

        private int? ParseInteger(string value, IErrorCache errorCache)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (int.TryParse(value, out var result))
            {
                return result;
            }

            errorCache.AddError($"Could not parse '{value}' as an integer");
            return null;
        }

        #endregion

        public Variable Variable { get; }
        public VariableRef Ref { get; }
        public object Value { get; set; }
        public object Default { get; set; }
        public object Max { get; set; }
        public object Min { get; set; }
        public SupportedTypes? SupportedType { get; set; }
        public Units? Units { get; set; }
        public bool HasErrors { get; set; }

        public object GetValueOrDefault()
        {
            return Value ?? Default;
        }

        public T GetValueOrDefault<T>(IErrorCache errorCache = null)
        {
            var result = Value ?? Default;
            if (result == null || result.GetType() != typeof(T))
            {
                var message = $"Could not convert '{result}' to {typeof(T).Name} for variable: {Variable.Name}";
                errorCache?.AddError(message);
                throw new InvalidOperationException(message);
            }

            return (T) result;
        }
    }
}
