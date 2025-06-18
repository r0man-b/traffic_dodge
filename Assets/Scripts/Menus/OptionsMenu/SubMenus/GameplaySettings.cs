using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    public class GameplaySettings : MonoBehaviour
    {
        public Slider cameraTypeSlider;       // Discrete camera mode index
        public Slider senseOfSpeedSlider;     // Float multiplier for visual speed effects

        private void Awake()
        {
            // Initialize sliders from saved values
            cameraTypeSlider.value = SaveManager.Instance.SaveData.cameraType;
            senseOfSpeedSlider.value = SaveManager.Instance.SaveData.senseOfSpeedModifier;
        }

        private void Start()
        {
            // Real-time updates (optional preview logic can go here)
            cameraTypeSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveCameraType)); });
            cameraTypeSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveCameraType), 0.2f); });

            senseOfSpeedSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveSenseOfSpeed)); });
            senseOfSpeedSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveSenseOfSpeed), 0.2f); });
        }

        private void SaveCameraType()
        {
            SaveManager.Instance.SaveData.cameraType = Mathf.RoundToInt(cameraTypeSlider.value);
            SaveManager.Instance.SaveGame();
        }

        private void SaveSenseOfSpeed()
        {
            SaveManager.Instance.SaveData.senseOfSpeedModifier = senseOfSpeedSlider.value;
            SaveManager.Instance.SaveGame();
        }
    }
}
