using System;
using Newtonsoft.Json;
using Version = DataManager.Api.v1.Version;

namespace DataManager.Services.Converters
{
    public class VersionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var version = (Version) value;
            writer.WriteValue(version.ToVersionString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Version.Parse((string) reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Version) == objectType;
        }
    }
}
