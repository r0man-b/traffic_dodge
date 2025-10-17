using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSounds : MonoBehaviour
{
    public AudioSource menuSoundObject;
    public AudioSource[] menuSourceSounds;
    public float[] originalMenuSourceVolumes; // Stores original volume levels.

    // Constants for woosh pitch
    private const float WOOSH_PITCH_START = 1.1f;
    private const float WOOSH_PITCH_MIN = 1.0f;
    private const int WOOSH_STEPS = 26;
    // exact step so 2.5 -> 1.0 in 26 calls
    private const float WOOSH_STEP_DELTA = (WOOSH_PITCH_START - WOOSH_PITCH_MIN) / WOOSH_STEPS;

    private int _wooshCalls = 0; // how many times we've reduced (0..26)

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
    public void PlayRouletteSpin()
    {
        menuSourceSounds[6].Play();
    }
    public void StopRouletteSpin()
    {
        menuSourceSounds[6].Stop();
    }
    public void PlayLootcrateOpen()
    {
        menuSourceSounds[7].Play();
    }
    
    public void PlayLootcrateAward()
    {
        menuSourceSounds[8].Play();
    }
    
    public void PlayLootcrateTick()
    {
        menuSourceSounds[9].Play();
    }
    
    public void PlayPartRouletteSpin()
    {
        menuSourceSounds[10].Play();
    }
    
    public void StopPartRouletteSpin()
    {
        menuSourceSounds[10].Stop();
    }

    public void PlayWoosh()
    {
        // Reduce first (so the 26th call plays at exactly 1.0)
        _wooshCalls = Mathf.Min(_wooshCalls + 1, WOOSH_STEPS);

        // Clamp calls to [0, WOOSH_STEPS]
        int callsClamped = Mathf.Clamp(_wooshCalls, 0, WOOSH_STEPS);
        float pitch = WOOSH_PITCH_START - WOOSH_STEP_DELTA * callsClamped;

        // Force exact min at the end to avoid float drift
        if (callsClamped >= WOOSH_STEPS) pitch = WOOSH_PITCH_MIN;

        menuSourceSounds[11].pitch = pitch;
        menuSourceSounds[11].Play();
    }

    public void ResetWooshPitch()
    {
        _wooshCalls = 0;

        int callsClamped = Mathf.Clamp(_wooshCalls, 0, WOOSH_STEPS);
        float pitch = WOOSH_PITCH_START - WOOSH_STEP_DELTA * callsClamped;

        // Force exact min at the end to avoid float drift
        if (callsClamped >= WOOSH_STEPS) pitch = WOOSH_PITCH_MIN;

        menuSourceSounds[11].pitch = pitch;
    }
}

