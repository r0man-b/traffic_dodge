using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSounds : MonoBehaviour
{
    public AudioSource menuSoundObject;
    public AudioSource[] menuSourceSounds;
    public float[] originalMenuSourceVolumes; // Stores original volume levels.

    // Start is called before the first frame update.
    void Start()
    {
        menuSourceSounds = menuSoundObject.GetComponents<AudioSource>();

        // Initialize and store original volume levels.
        originalMenuSourceVolumes = new float[menuSourceSounds.Length];
        for (int i = 0; i < menuSourceSounds.Length; i++)
        {
            if (menuSourceSounds[i] != null)
            {
                originalMenuSourceVolumes[i] = menuSourceSounds[i].volume; // Save original volume.
                menuSourceSounds[i].volume = originalMenuSourceVolumes[i] * SaveManager.Instance.SaveData.EffectsVolumeMultiplier; // Apply saved volume.
            }
        }
    }

    public void PlayClick()
    {
        menuSourceSounds[0].Play();
    }

    public void PlaySprayCan()
    {
        menuSourceSounds[1].Play();
    }

    public void PlayAirWrenchSound()
    {
        menuSourceSounds[2].Play();
    }

    public void PlayButtonSwitch()
    {
        menuSourceSounds[3].Play();
    }
    
    public void PlayChaChing()
    {
        menuSourceSounds[4].Play();
    }

    public void PlayCreditChange()
    {
        menuSourceSounds[5].Play();
    }
}

