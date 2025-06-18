using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    public class UnitsSettings : MonoBehaviour
    {
        public Toggle mphToggle; // Toggle for MPH.
        public Toggle kphToggle; // Toggle for KPH.

        private void Awake()
        {
            // Load saved unit setting and apply to toggles.
            bool isImperial = SaveManager.Instance.SaveData.ImperialUnits;

            // Ensure only one toggle is set correctly.
            mphToggle.SetIsOnWithoutNotify(isImperial);
            kphToggle.SetIsOnWithoutNotify(!isImperial);

            // Add listeners.
            mphToggle.onValueChanged.AddListener(OnMPHToggleChanged);
            kphToggle.onValueChanged.AddListener(OnKPHToggleChanged);
        }

        private void OnMPHToggleChanged(bool isOn)
        {
            if (isOn)
            {
                kphToggle.SetIsOnWithoutNotify(false); // Ensure KPH is turned off.
                SaveManager.Instance.SaveData.ImperialUnits = true;
                SaveManager.Instance.SaveGame();
            }
            else if (!kphToggle.isOn) // Prevent both from being off.
            {
                mphToggle.SetIsOnWithoutNotify(true);
            }
        }

        private void OnKPHToggleChanged(bool isOn)
        {
            if (isOn)
            {
                mphToggle.SetIsOnWithoutNotify(false); // Ensure MPH is turned off.
                SaveManager.Instance.SaveData.ImperialUnits = false;
                SaveManager.Instance.SaveGame();
            }
            else if (!mphToggle.isOn) // Prevent both from being off.
            {
                kphToggle.SetIsOnWithoutNotify(true);
            }
        }
    }
}
