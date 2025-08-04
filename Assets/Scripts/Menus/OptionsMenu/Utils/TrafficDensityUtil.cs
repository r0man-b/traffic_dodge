using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrafficDensityUtil : MonoBehaviour
{
    public Toggle[] toggleButtons;
    public ToggleGroup toggleGroup;  // Reference to the ToggleGroup component.
    public TMP_Text[] creditMultiplierTexts;

    // Start is called before the first frame update.
    void Start()
    {
        // Assign the toggle group to all toggle buttons.
        foreach (Toggle toggle in toggleButtons)
        {
            toggle.group = toggleGroup;
        }

        // Access SaveData through SaveManager.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Get the traffic density, default to 3 if not set
        int trafficDensity = saveData.TrafficDensity;
        if (trafficDensity == -1) // Check if it's the default uninitialized value
        {
            trafficDensity = 3; // Set default value
            saveData.TrafficDensity = trafficDensity;
            SaveManager.Instance.SaveGame(); // Save default value
        }

        // Set the corresponding toggle based on the traffic density.
        int selectedToggleIndex = trafficDensity - 1;
        for (int i = 0; i < toggleButtons.Length; i++)
        {
            toggleButtons[i].isOn = (i == selectedToggleIndex);
        }

    }

    public void SetTrafficDensity(int density)
    {
        Debug.Log(density);
        foreach (TMP_Text tmp in creditMultiplierTexts)
        {
            if (tmp != null)
                tmp.gameObject.SetActive(false);
        }
        creditMultiplierTexts[5 - density].gameObject.SetActive(true);
        // Update traffic density in SaveData and save to JSON.
        SaveManager.Instance.SaveData.TrafficDensity = density;
        SaveManager.Instance.SaveGame();
    }
}