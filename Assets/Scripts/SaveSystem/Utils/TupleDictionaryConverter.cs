using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class TupleDictionaryConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType) return false;
        if (objectType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) return false;

        var keyType = objectType.GetGenericArguments()[0];
        if (!keyType.IsValueType || !keyType.FullName.StartsWith("System.ValueTuple`2")) return false;

        var args = keyType.GetGenericArguments();
        return args.Length == 2 && args[0] == typeof(string) && args[1] == typeof(int);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // NEW FORMAT: array of entries
        // [
        //   { "CarType": "Ferret", "CarIndex": 0, "Value": { ... CarData ... } },
        //   ...
        // ]
        var dict = (IDictionary)value;
        writer.WriteStartArray();

        foreach (DictionaryEntry entry in dict)
        {
            var keyObj = entry.Key;
            // Read ValueTuple<string,int>.Item1 / Item2 via reflection
            var item1 = keyObj.GetType().GetField("Item1", BindingFlags.Public | BindingFlags.Instance);
            var item2 = keyObj.GetType().GetField("Item2", BindingFlags.Public | BindingFlags.Instance);
            if (item1 == null || item2 == null)
                throw new JsonSerializationException("Tuple key does not expose Item1/Item2.");

            string carType = (string)item1.GetValue(keyObj);
            int carIndex = (int)item2.GetValue(keyObj);

            writer.WriteStartObject();
            writer.WritePropertyName("CarType");
            writer.WriteValue(carType);
            writer.WritePropertyName("CarIndex");
            writer.WriteValue(carIndex);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, entry.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var valueType = objectType.GetGenericArguments()[1];
        var result = (IDictionary)Activator.CreateInstance(objectType);

        if (reader.TokenType == JsonToken.Null) return result;

        if (reader.TokenType == JsonToken.StartArray)
        {
            // New format
            var arr = JArray.Load(reader);
            foreach (var item in arr)
            {
                string carType = item["CarType"]?.ToObject<string>()
                                 ?? throw new JsonSerializationException("Missing CarType");
                int carIndex = item["CarIndex"]?.ToObject<int>() ?? 0;
                var valueToken = item["Value"] ?? throw new JsonSerializationException("Missing Value");
                var valueObj = valueToken.ToObject(valueType, serializer);

                var key = ValueTuple.Create(carType, carIndex);
                result.Add(key, valueObj);
            }
            return result;
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            // Legacy format: { "(Ferret, 0)": { ... }, ... }
            var obj = JObject.Load(reader);
            foreach (var prop in obj.Properties())
            {
                var (carType, carIndex) = ParseLegacyKey(prop.Name);
                var valueObj = prop.Value.ToObject(valueType, serializer);
                var key = ValueTuple.Create(carType, carIndex);
                result.Add(key, valueObj);
            }
            return result;
        }

        throw new JsonSerializationException("Unexpected token for tuple dictionary: " + reader.TokenType);
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;

    private static (string carType, int carIndex) ParseLegacyKey(string key)
    {
        string s = key.Trim();
        if (s.StartsWith("(") && s.EndsWith(")"))
            s = s.Substring(1, s.Length - 2);

        int comma = s.IndexOf(',');
        if (comma < 0) throw new JsonSerializationException($"Invalid legacy key '{key}'.");

        string left = s.Substring(0, comma).Trim();
        string right = s.Substring(comma + 1).Trim();

        if ((left.Length >= 2) &&
            ((left[0] == '"' && left[left.Length - 1] == '"') ||
             (left[0] == '\'' && left[left.Length - 1] == '\'')))
        {
            left = left.Substring(1, left.Length - 2);
        }

        if (!int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
            throw new JsonSerializationException($"Invalid index in legacy key '{key}'.");

        return (left, idx);
    }
}
