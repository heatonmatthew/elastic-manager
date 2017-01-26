using System;
using System.Text.RegularExpressions;
using DataManager.Services.Variables;
using EnumStringValues;

namespace DataManager.Api.v1
{
    public class Constant
    {
        public object Value { get; set; }
        public Units? Units { get; set; }
        public SupportedTypes? SupportedType { get; set; }

        public string ToEncoded()
        {
            return $"{Value}{Units.GetStringValue()}";
        }

        public static Constant ParseEncoded(string encoded)
        {
            if (!TryParseEncoded(encoded, out var result))
            {
                throw new ArgumentOutOfRangeException(nameof(encoded), $"Could not parse '{encoded}' as a constant.");
            }
            return result;
        }

        public static bool TryParseEncoded(string encoded, out Constant value)
        {
            value = new Constant();

            if (!string.IsNullOrWhiteSpace(encoded))
            {
                var match = Regex.Match(encoded, @"^\s*(\d+)\s*(\w+)\s*$");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var integer) &&
                        match.Groups[2].Value.TryParseStringValueToEnum(out Units units))
                    {
                        value.Value = integer;
                        value.Units = units;
                        value.SupportedType = SupportedTypes.Integer;
                        return true;
                    }
                }
            }

            value = new Constant();
            return false;
        }

        public static bool TryParseEncodedToVariable(string encoded, out Variable value)
        {
            if (TryParseEncoded(encoded, out var constant))
            {
                value = new Variable
                {
                    Value = constant.Value.ToString(),
                    Units = constant.Units?.GetStringValue(),
                    Type = constant.SupportedType?.GetStringValue()
                };
                value.Default = value.Value;
                
                // TODO - should we support max and min etc.?

                return true;
            }

            value = null;
            return false;
        }
    }
}
