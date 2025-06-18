using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class TupleDictionaryConverter : JsonConverter<Dictionary<(int, int), SaveData.CarData>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<(int, int), SaveData.CarData> value, JsonSerializer serializer)
    {
        var tempDict = new Dictionary<string, SaveData.CarData>();

        // Convert tuple keys to strings for serialization.
        foreach (var kvp in value)
        {
            string key = $"({kvp.Key.Item1}, {kvp.Key.Item2})";
            tempDict[key] = kvp.Value;
        }

        serializer.Serialize(writer, tempDict);
    }

    public override Dictionary<(int, int), SaveData.CarData> ReadJson(JsonReader reader, Type objectType, Dictionary<(int, int), SaveData.CarData> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var tempDict = serializer.Deserialize<Dictionary<string, SaveData.CarData>>(reader);
        var dict = new Dictionary<(int, int), SaveData.CarData>();

        // Convert string keys back to tuple keys for deserialization.
        foreach (var kvp in tempDict)
        {
            var keyParts = kvp.Key.Trim('(', ')').Split(',');
            int item1 = int.Parse(keyParts[0]);
            int item2 = int.Parse(keyParts[1]);
            dict[(item1, item2)] = kvp.Value;
        }

        return dict;
    }
}
