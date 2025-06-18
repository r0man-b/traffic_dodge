using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Settings
{
    public class DisplaySettings : MonoBehaviour
    {
        public Slider brightnessSlider;  // UI Slider for brightness.
        public Toggle vignetteToggle;    // UI Toggle for vignette.
        public Volume postProcessingVolume; // Post-processing volume.
        private LiftGammaGain liftGammaGain;

        private void Awake()
        {
            // Ensure the Post-Processing volume has LiftGammaGain.
            if (postProcessingVolume.profile.TryGet(out liftGammaGain))
            {
                // Load saved gamma value and apply it to slider & post-processing.
                brightnessSlider.value = SaveManager.Instance.SaveData.GammaValue;
                ApplyGamma(brightnessSlider.value);
            }

            // Load saved vignette setting and apply to toggle.
            vignetteToggle.isOn = SaveManager.Instance.SaveData.VignetteEnabled;
        }

        private void Start()
        {
            // Listen for slider changes (real-time update).
            brightnessSlider.onValueChanged.AddListener(ApplyGamma);

            // Listen for slider release (save only on release).
            brightnessSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveGamma)); });
            brightnessSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveGamma), 0.2f); });

            // Listen for vignette toggle changes and save instantly.
            vignetteToggle.onValueChanged.AddListener(SaveVignette);
        }

        private void ApplyGamma(float value)
        {
            if (liftGammaGain != null)
            {
                Vector4 gamma = liftGammaGain.gamma.value;
                gamma.w = value * 5f; // Modify only the gamma (w) component.
                liftGammaGain.gamma.value = gamma;
            }
        }

        private void SaveGamma()
        {
            // Manually update the saved gamma value.
            SaveManager.Instance.SaveData.GammaValue = brightnessSlider.value;
            SaveManager.Instance.SaveGame(); // Persist changes.
        }

        private void SaveVignette(bool isEnabled)
        {
            // Manually update the saved vignette flag.
            SaveManager.Instance.SaveData.VignetteEnabled = isEnabled;
            SaveManager.Instance.SaveGame(); // Persist changes.
        }
    }
}