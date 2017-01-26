using DataManager.Services.Converters;
using Newtonsoft.Json;

namespace DataManager.Api.v1
{
    public enum SettingValueType
    {
        Null,
        Constant,
        Variable
    }

    // TODO - Should I change the name of this to PackageSetting ?!?!
    [JsonConverter(typeof(SettingValueConverter))]
    public class SettingValue
    {
        public VariableRef FromVariable { get; set; }
        public Constant ConstantValue { get; set; }

        public SettingValueType ValueType
        {
            get
            {
                if (FromVariable != null)
                {
                    return SettingValueType.Variable;
                }

                return ConstantValue != null 
                    ? SettingValueType.Constant 
                    : SettingValueType.Null;
            }
        }

        public bool IsNull => ValueType == SettingValueType.Null;
    }
}
