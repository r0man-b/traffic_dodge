using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSounds : MonoBehaviour
{
    public AudioSource menuSoundObject;
    public AudioSource[] menuSourceSounds;
    public float[] originalMenuSourceVolumes; // Stores original volume levels.

    public AudioSource[] engineRevSounds;
    public float[] originalEngineRevVolumes; // Stores original volume levels.

    // --- Added: store original pitches and running coroutines for engine sounds ---
    private float[] _originalEngineRevPitches;
    private Coroutine[] _revCoroutines;

    // Optional: configurable ramp time (seconds) for the "up" phase.
    private float revUpDuration = 0.75f; // down phase = 3 * this

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

        // Initialize and store original volume levels for menu sources.
        originalMenuSourceVolumes = new float[menuSourceSounds.Length];
        for (int i = 0; i < menuSourceSounds.Length; i++)
        {
            if (menuSourceSounds[i] != null)
            {
                originalMenuSourceVolumes[i] = menuSourceSounds[i].volume; // Save original volume.
                menuSourceSounds[i].volume = originalMenuSourceVolumes[i] * SaveManager.Instance.SaveData.EffectsVolumeMultiplier; // Apply saved volume.
            }
        }

        // --- Initialize engine rev sources: volumes, pitches, and coroutine slots ---
        if (engineRevSounds != null && engineRevSounds.Length > 0)
        {
            originalEngineRevVolumes = new float[engineRevSounds.Length];
            _originalEngineRevPitches = new float[engineRevSounds.Length];
            _revCoroutines = new Coroutine[engineRevSounds.Length];

            for (int i = 0; i < engineRevSounds.Length; i++)
            {
                var src = engineRevSounds[i];
                if (src == null) continue;

                // Store original volume and apply saved multiplier.
                originalEngineRevVolumes[i] = src.volume;
                src.volume = originalEngineRevVolumes[i] * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;

                // Store original pitch.
                _originalEngineRevPitches[i] = src.pitch;
            }
        }
    }

    public void PlayClick() { menuSourceSounds[0].Play(); }
    public void PlaySprayCan() { menuSourceSounds[1].Play(); }
    public void PlayAirWrenchSound() { menuSourceSounds[2].Play(); }
    public void PlayButtonSwitch() { menuSourceSounds[3].Play(); }
    public void PlayChaChing() { menuSourceSounds[4].Play(); }
    public void PlayCreditChange() { menuSourceSounds[5].Play(); }
    public void PlayRouletteSpin() { menuSourceSounds[6].Play(); }
    public void StopRouletteSpin() { menuSourceSounds[6].Stop(); }
    public void PlayLootcrateOpen() { menuSourceSounds[7].Play(); }
    public void PlayLootcrateAward() { menuSourceSounds[8].Play(); }
    public void PlayLootcrateTick() { menuSourceSounds[9].Play(); }
    public void PlayPartRouletteSpin() { menuSourceSounds[10].Play(); }
    public void StopPartRouletteSpin() { menuSourceSounds[10].Stop(); }
    public void PlaySliderChange() { menuSourceSounds[12].Play(); }

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

    public void PlayEngineRev(int currentCarType)
    {
        // Validate index and source.
        if (engineRevSounds == null || currentCarType < 0 || currentCarType >= engineRevSounds.Length)
            return;

        var src = engineRevSounds[currentCarType];
        if (src == null)
            return;

        // Ensure volumes reflect the latest saved multiplier (in case settings changed at runtime).
        if (originalEngineRevVolumes != null && currentCarType < originalEngineRevVolumes.Length)
        {
            src.volume = originalEngineRevVolumes[currentCarType] * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;
        }

        // Stop any existing rev coroutine for this index.
        if (_revCoroutines != null && _revCoroutines[currentCarType] != null)
        {
            StopCoroutine(_revCoroutines[currentCarType]);
            _revCoroutines[currentCarType] = null;

            // Restore pitch if a prior routine was interrupted.
            if (_originalEngineRevPitches != null && currentCarType < _originalEngineRevPitches.Length)
            {
                src.pitch = _originalEngineRevPitches[currentCarType];
            }
        }

        // Start a new rev coroutine.
        _revCoroutines[currentCarType] = StartCoroutine(RevRoutine(currentCarType));
    }

    private IEnumerator RevRoutine(int index)
    {
        var src = engineRevSounds[index];
        if (src == null) yield break;

        // Capture originals
        float originalPitch = (_originalEngineRevPitches != null && index < _originalEngineRevPitches.Length)
            ? _originalEngineRevPitches[index]
            : src.pitch;

        bool originalLoop = src.loop;
        bool wasPlayingInitially = src.isPlaying;

        // Capture the applied volume (already multiplied in Start() or at call site)
        float originalVolumeApplied = src.volume;

        // Pitch targets
        float pitchStart = 0.1f;  // start at 1/4
        float pitchPeak = 1.0f;   // up to 1.0 (original)
        float pitchEnd = 0.1f;   // down to 1/2

        // Durations (keep your chosen ratio)
        float upTime = Mathf.Max(0.0001f, revUpDuration);
        float downTime = upTime * 8f;

        // Prepare: loop during the effect and set initial pitch
        src.loop = true;
        src.pitch = pitchStart;

        // Start playback if needed
        if (!wasPlayingInitially)
        {
            src.time = 0f;
            src.Play();
        }

        // ---- Ramp up: 0.25x -> 1.0x (volume unchanged) ----
        float t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / upTime);
            src.pitch = Mathf.Lerp(pitchStart, pitchPeak, a);
            yield return null;
        }
        src.pitch = pitchPeak;

        // ---- Ramp down: 1.0x -> 0.5x, fade volume to 0 ----
        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / downTime);
            src.pitch = Mathf.Lerp(pitchPeak, pitchEnd, a);
            src.volume = Mathf.Lerp(originalVolumeApplied, 0f, a);
            yield return null;
        }
        src.pitch = pitchEnd;
        src.volume = 0f;

        // ---- Restore original state ----
        src.pitch = originalPitch;
        src.volume = originalVolumeApplied;
        src.loop = originalLoop;

        // Stop playback only if we started it
        if (!wasPlayingInitially)
        {
            src.Stop();
            src.time = 0f;
        }

        _revCoroutines[index] = null;
    }
}