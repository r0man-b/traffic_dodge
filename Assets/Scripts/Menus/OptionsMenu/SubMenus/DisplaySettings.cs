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
        public Toggle filmGrainToggle;   // UI Toggle for film grain.
        public Volume postProcessingVolume; // Post-processing volume.
        private LiftGammaGain liftGammaGain;
        private FilmGrain filmGrain;

        private void Awake()
        {
            if (postProcessingVolume != null && postProcessingVolume.profile != null)
            {
                // Ensure the Post-Processing volume has LiftGammaGain.
                if (postProcessingVolume.profile.TryGet(out liftGammaGain))
                {
                    // Load saved gamma value and apply it to slider & post-processing.
                    if (brightnessSlider != null)
                    {
                        brightnessSlider.value = SaveManager.Instance.SaveData.GammaValue;
                        ApplyGamma(brightnessSlider.value);
                    }
                }

                // Ensure the Post-Processing volume has FilmGrain.
                if (postProcessingVolume.profile.TryGet(out filmGrain))
                {
                    // Load saved film grain setting and apply it.
                    filmGrain.active = SaveManager.Instance.SaveData.FilmGrainEnabled;
                }
            }

            // Load saved vignette setting and apply to toggle.
            if (vignetteToggle != null)
                vignetteToggle.isOn = SaveManager.Instance.SaveData.VignetteEnabled;

            // Load saved film grain setting and apply to toggle.
            if (filmGrainToggle != null)
                filmGrainToggle.isOn = SaveManager.Instance.SaveData.FilmGrainEnabled;
        }

        private void Start()
        {
            // Listen for slider changes (real-time update).
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(ApplyGamma);

                // Listen for slider release (save only on release).
                brightnessSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveGamma)); });
                brightnessSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveGamma), 0.2f); });
            }

            // Listen for vignette toggle changes and save instantly.
            if (vignetteToggle != null)
                vignetteToggle.onValueChanged.AddListener(SaveVignette);

            // Listen for film grain toggle changes and save instantly.
            if (filmGrainToggle != null)
                filmGrainToggle.onValueChanged.AddListener(SaveFilmGrain);
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

        private void SaveFilmGrain(bool isEnabled)
        {
            // Update the film grain effect in real-time if volume is set.
            if (filmGrain != null)
            {
                filmGrain.active = isEnabled;
            }

            // Manually update the saved film grain flag.
            SaveManager.Instance.SaveData.FilmGrainEnabled = isEnabled;
            SaveManager.Instance.SaveGame(); // Persist changes.
        }
    }
}