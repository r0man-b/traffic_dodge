using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem
{
    private static string saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");

    public static void Save(SaveData data)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new TupleDictionaryConverter()); // Add custom converter.

        string json = JsonConvert.SerializeObject(data, settings);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved to JSON!");
    }

    public static SaveData Load()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new TupleDictionaryConverter()); // Add custom converter.

            return JsonConvert.DeserializeObject<SaveData>(json, settings);
        }

        Debug.Log("No save file found, initializing new save data.");
        return new SaveData();
    }

    public static void Reset()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save data reset.");
        }
    }
}
