using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class JsonHelper
    {
        public static string AsJson(this object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings { Converters = new List<JsonConverter>() { new DecimalConverter() } });
        }
    }
    internal class DecimalConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal) || objectType == typeof(decimal?));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            decimal? d = default;
            if (value != null)
            {
                d = value as decimal?;
                if (d.HasValue) // If value was a decimal?, then this is possible
                {
                    d = decimal.Round(d.Value, 2, MidpointRounding.AwayFromZero);
                }
            }
            JToken.FromObject(d.Value.ToString("#0.00", CultureInfo.GetCultureInfo("en-US"))).WriteTo(writer);
        }
    }
}
