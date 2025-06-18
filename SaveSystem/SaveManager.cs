using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveManager");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go); // Persist across scenes.
            }
            return _instance;
        }
    }

    public SaveData SaveData { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists.
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Make persistent.

        LoadSaveData(); // Load SaveData once.
    }

    public void LoadSaveData()
    {
        SaveData = SaveSystem.Load(); // Load from JSON.
    }

    public void SaveGame()
    {
        SaveSystem.Save(SaveData); // Save to JSON.
    }

    public void ResetSaveData()
    {
        SaveSystem.Reset(); // Delete save file.
        SaveData = new SaveData(); // Initialize new data.
    }
}
