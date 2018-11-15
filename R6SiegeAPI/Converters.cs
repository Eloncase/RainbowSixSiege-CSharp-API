using Newtonsoft.Json;
using System;
using static R6SiegeAPI.Enums;

namespace R6SiegeAPI
{
    static class Converters
    {
        public class PlatformConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.Value)
                {
                    case "uplay":
                        return Platform.UPLAY;
                    case "xbn":
                        return Platform.XBOX;
                    case "psn":
                        return Platform.PLAYSTATION;
                    default:
                        throw new Exception($"Couldn't convert {reader.ValueType} to {objectType}");
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class OperatorCategoryConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.Value)
                {
                    case "def":
                        return OperatorCategory.Defense;
                    case "atk":
                        return OperatorCategory.Attack;
                    default:
                        throw new Exception($"Couldn't convert {reader.ValueType} to {objectType}");
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class RegionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch (reader.Value)
                {
                    case "emea":
                        return RankedRegion.EU;
                    case "ncsa":
                        return RankedRegion.NA;
                    case "apac":
                        return RankedRegion.ASIA;
                    default:
                        throw new Exception($"Couldn't convert {reader.ValueType} to {objectType}");
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class TimeSpanFromSecondsConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return TimeSpan.FromSeconds((Int64)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class SeasonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var t = API.GetAPI().GetSeasons();
                t.Wait();
                return t.Result[int.Parse(reader.Value.ToString())];
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
