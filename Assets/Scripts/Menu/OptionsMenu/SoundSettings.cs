using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    public class SoundSettings : MonoBehaviour
    {
        public Slider musicVolumeSlider;  // Music volume slider.
        public Slider effectsVolumeSlider; // Effects volume slider.

        public MainMenu mainMenu; // Main menu object containing the MenuMusic AudioSource.
        public MenuSounds menuSounds; // Menu sounds object containing menuSourceSounds AudioSource array.

        private void Awake()
        {
            // Load saved volume settings and apply to sliders.
            musicVolumeSlider.value = SaveManager.Instance.SaveData.MusicVolume;
            effectsVolumeSlider.value = SaveManager.Instance.SaveData.EffectsVolumeMultiplier;

            // Apply saved volumes on start
            UpdateMusicVolume(musicVolumeSlider.value);
            UpdateEffectsVolume(effectsVolumeSlider.value);

            // Listen for slider changes (real-time effect).
            musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
            effectsVolumeSlider.onValueChanged.AddListener(UpdateEffectsVolume);

            // Save only when user releases the slider.
            musicVolumeSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveMusicVolume)); });
            musicVolumeSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveMusicVolume), 0.2f); });

            effectsVolumeSlider.onValueChanged.AddListener(delegate { CancelInvoke(nameof(SaveEffectsVolume)); });
            effectsVolumeSlider.onValueChanged.AddListener(delegate { Invoke(nameof(SaveEffectsVolume), 0.2f); });
        }

        private void UpdateMusicVolume(float value)
        {
            if (mainMenu != null && mainMenu.MenuMusic != null)
            {
                mainMenu.MenuMusic.volume = value;
            }
        }

        private void UpdateEffectsVolume(float value)
        {
            // Update menu Sounds volume.
            if (menuSounds != null && menuSounds.menuSourceSounds != null)
            {
                for (int i = 0; i < menuSounds.menuSourceSounds.Length; i++)
                {
                    if (menuSounds.menuSourceSounds[i] != null)
                    {
                        // Apply scaled volume using the original volume.
                        menuSounds.menuSourceSounds[i].volume = menuSounds.originalMenuSourceVolumes[i] * value;
                    }
                }
            }
        }


        private void SaveMusicVolume()
        {
            SaveManager.Instance.SaveData.MusicVolume = musicVolumeSlider.value;
            SaveManager.Instance.SaveGame();
        }

        private void SaveEffectsVolume()
        {
            SaveManager.Instance.SaveData.EffectsVolumeMultiplier = effectsVolumeSlider.value;
            SaveManager.Instance.SaveGame();
        }
    }
}


