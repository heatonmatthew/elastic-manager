using System;
using DataManager.Api.v1;
using Newtonsoft.Json;

namespace DataManager.Services.Converters
{
    public class SettingValueConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var setting = (SettingValue) value;
            if (!string.IsNullOrWhiteSpace(setting?.FromVariable?.VariableName))
            {
                writer.WriteStartObject();

                if (!string.IsNullOrWhiteSpace(setting.FromVariable.PackageId))
                {
                    writer.WritePropertyName(Constants.PackageId);
                    writer.WriteValue(setting.FromVariable.PackageId);
                }
                
                writer.WritePropertyName(Constants.VariableName);
                writer.WriteValue(setting.FromVariable.VariableName);

                if (setting.FromVariable.DefaultValue != null)
                {
                    writer.WritePropertyName(Constants.Default);
                    writer.WriteValue("not yet implemented");
                }

                writer.WriteEndObject();
                return;
            }

            if (setting?.ConstantValue != null)
            {
                writer.WriteValue(setting.ConstantValue.ToEncoded());
                return;
            }

            writer.WriteNull();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return DeserializeString((string) reader.Value);
                case JsonToken.StartObject:
                    return DeserializeVariableRef(reader);
                case JsonToken.Null:
                    return null;
            }

            throw new Exception($"Cannot deserialize current reader value as a {typeof(SettingValue).FullName}");
        }

        private SettingValue DeserializeString(string value)
        {
            if (Constant.TryParseEncoded(value, out var constant))
            {
                return new SettingValue {ConstantValue = constant};
            }
            if (VariableRef.TryParse(value, out var varRef))
            {
                return new SettingValue {FromVariable = varRef};
            }
            return new SettingValue {FromVariable = new VariableRef {VariableName = value}};
        }

        private SettingValue DeserializeVariableRef(JsonReader reader)
        {
            var result = new SettingValue {FromVariable = new VariableRef()};
            var varRef = result.FromVariable;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;

                var propertyName = (string) reader.Value;
                reader.Read();
                switch (propertyName)
                {
                    case Constants.PackageId:
                        varRef.PackageId = (string) reader.Value;
                        break;
                    case Constants.VariableName:
                        varRef.VariableName = (string) reader.Value;
                        break;
                    case Constants.Default:
                        varRef.DefaultValue = (string) reader.Value; // default to string for now
                        break;
                }
            }

            return varRef.IsEmpty ? null : result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(SettingValue) == objectType;
        }
    }
}
