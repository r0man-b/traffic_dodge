using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    // Lists to track all audio sources and their states (true if playing, false otherwise).
    private List<AudioSource> audioSources = new();
    private List<bool> audioSourceStates = new();

    // Toggle to keep track of pause state.
    private bool isPaused = false;

    // Variables for audio sources.
    public AudioSource idlesource;
    public AudioSource explossource;
    public AudioSource songsource;
    public AudioSource beepsource;
    public AudioSource crashsource;
    public AudioSource trafficexplossource;
    public AudioSource livesource;
    public AudioSource aggrosource;
    public AudioSource zoomoutsource;
    public AudioSource bulletsource;
    public AudioSource tornadoexplossource;
    public AudioSource leftwooshsource;
    public AudioSource rightwooshsource;
    public AudioSource windaccelsource;
    public AudioSource windscreamsource;
    public AudioSource numberincreasesource;
    public AudioSource newrecordsource;

    // Variables for individual audio clips.
    public AudioClip explos;
    public AudioClip crash1;
    public AudioClip woosh;

    // Array to store songs.
    public AudioSource[] songs;
    public int currentSongIndex = 0;

    // Array to store engine sounds.
    public AudioSource[] enginesounds;
    private int currentEngineSoundIndex = 0;

    // State variables.
    private bool engineplayed = false;
    public bool engineScreamPlayed = false;
    private bool windScreamPlayed = false;
    private bool songplayed = false;
    private bool explosplayed = false;
    private bool inAggro = false;
    private bool inBullet = false;

    // Misc. variables.
    public float drop = 3f;
    private float startTime;
    private float volumeModifier = 2f;
    private float originalEnginePitch = 1f;
    private Coroutine bulletEffectsCoroutine;
    public float[] originalSoundManagerAudioSourceVolumes; // Stores original volume levels.
    private AudioSource[] specificAudioSources;
    public Dictionary<AudioSource, float> originalPitches = new Dictionary<AudioSource, float>();
    private PlayerController playerController;
    private UIManager uiManager;


    void Start()
    {
        // Find scripts.
        GameObject PlayerCarObject = GameObject.Find("PlayerCar");
        playerController = PlayerCarObject.GetComponent<PlayerController>();
        GameObject CanvasObject = GameObject.Find("Canvas");
        uiManager = CanvasObject.GetComponent<UIManager>();

        // Set up car song radio.
        songs = songsource.GetComponents<AudioSource>();
        SaveData saveData = SaveManager.Instance.SaveData;
        currentSongIndex = saveData.LastRaceSongIndex;
        currentSongIndex = (currentSongIndex + 1) % songs.Length;
        saveData.LastRaceSongIndex = currentSongIndex;
        foreach (AudioSource songsource in songs)
        {
            songsource.volume = saveData.MusicVolume;
        }
        SaveManager.Instance.SaveGame();

        // Set up engine sounds.
        enginesounds = playerController.carObject.GetComponents<AudioSource>();

        // Find audio sources and store pitch.
        specificAudioSources = new AudioSource[]
        {
            enginesounds[1],
            enginesounds[2],
            songs[currentSongIndex],
            beepsource//,
            //crashsource//,
            //trafficexplossource
        };
        foreach (AudioSource audioSource in specificAudioSources)
        {
            originalPitches[audioSource] = audioSource.pitch;
        }

        // Save original audio source volumes.
        originalSoundManagerAudioSourceVolumes = new float[]
        {
            idlesource.volume,
            explossource.volume,
            songsource.volume,
            beepsource.volume,
            crashsource.volume,
            trafficexplossource.volume,
            livesource.volume,
            aggrosource.volume,
            zoomoutsource.volume,
            bulletsource.volume,
            tornadoexplossource.volume,
            leftwooshsource.volume,
            rightwooshsource.volume,
            windaccelsource.volume,
            windscreamsource.volume,
            numberincreasesource.volume,
            newrecordsource.volume,
            enginesounds[0].volume,
            enginesounds[1].volume,
            enginesounds[2].volume,
        };

        // Add all audio sources and their initial states to the list.
        RegisterAudioSource(idlesource,             0);
        RegisterAudioSource(explossource,           1);
        RegisterAudioSource(songsource,             2);
        RegisterAudioSource(beepsource,             3);
        RegisterAudioSource(crashsource,            4);
        RegisterAudioSource(trafficexplossource,    5);
        RegisterAudioSource(livesource,             6);
        RegisterAudioSource(aggrosource,            7);
        RegisterAudioSource(zoomoutsource,          8);
        RegisterAudioSource(bulletsource,           9);
        RegisterAudioSource(tornadoexplossource,    10);
        RegisterAudioSource(leftwooshsource,        11);
        RegisterAudioSource(rightwooshsource,       12);
        RegisterAudioSource(windaccelsource,        13);
        RegisterAudioSource(windscreamsource,       14);
        RegisterAudioSource(numberincreasesource,   15);
        RegisterAudioSource(newrecordsource,        16);
        int startingEngineIndex = 17;
        foreach (AudioSource engineSource in enginesounds)
        {
            RegisterAudioSource(engineSource, startingEngineIndex);
            startingEngineIndex += 1;
        }

        // Start clock.
        startTime = Time.time;
    }

    void Update()
    {
        windaccelsource.volume = Mathf.Min(playerController.accel / 4, 0.4f) * SaveManager.Instance.SaveData.EffectsVolumeMultiplier; ;
        windscreamsource.volume = Mathf.Min(playerController.accel / 4, 0.25f) * SaveManager.Instance.SaveData.EffectsVolumeMultiplier; ;
        // Ensure song pitches return to normal after aggro or bullet powerups expire.
        if (!playerController.aggro && !playerController.bullet)
        {
            songs[currentSongIndex].pitch = 1;
        }

        // Begin playing the song & engine idle sound.
        if (!songplayed && uiManager.countdownStarted)
        {
            enginesounds[0].Play();
            songs[currentSongIndex].Play();
            songplayed = true;
        }

        // Check if the song is nearing its end (less than 6 seconds left).
        if (songplayed && songs[currentSongIndex].time > songs[currentSongIndex].clip.length - 6)
        {
            int nextSongIndex = (currentSongIndex + 1) % songs.Length; // Cycle to the next song, or wrap around to the first.
            StartCoroutine(CrossfadeCoroutine(songs[currentSongIndex], songs[nextSongIndex], 5.5f, true)); // 5.5 seconds crossfade to give a half-second buffer.
            currentSongIndex = nextSongIndex; // Update the index to the next song.
            SaveManager.Instance.SaveData.LastRaceSongIndex = currentSongIndex;
            SaveManager.Instance.SaveGame();
        }

        // Stop idle engine noise & start acceleration noise once song drops.
        if (!engineplayed && Time.time - startTime >= drop)
        {
            enginesounds[0].Stop();
            enginesounds[1].Play();
            engineplayed = true;
            currentEngineSoundIndex = 1;
        }

        // Crossfade the engine acceleration sound into the engine scream sound.
        if (enginesounds[1].clip.length - enginesounds[1].time <= 1.0f && !engineScreamPlayed)
        {
            StartCoroutine(CrossfadeCoroutine(enginesounds[1], enginesounds[2], 3.0f, false));
            engineScreamPlayed = true;
            currentEngineSoundIndex = 2;
        }

        // Crossfade the wind sound into the wind scream sound.
        if (windaccelsource.clip.length - windaccelsource.time <= 5.0f && !windScreamPlayed)
        {
            StartCoroutine(CrossfadeCoroutine(windaccelsource, windscreamsource, 0.5f, false));
            windScreamPlayed = true;
        }

        // If the game has ended, stop playing engine noise & play the explosion noise.
        if (playerController.gameEnd)
        {
            enginesounds[1].Stop();
            enginesounds[2].Stop();
            windaccelsource.Stop();
            windscreamsource.Stop();
            if (!explosplayed)
            {
                explossource.PlayOneShot(explos);
                explosplayed = true;
            }
        }
    }


    /*------------------------------------- MISC. AUDIO FUNCTIONS -------------------------------------*/
    public void PlayBeep()
    {
        beepsource.Play();
    }

    public void PlayCrash()
    {
        float minPitch = Mathf.Min(0.7f, 0.3f + 0.8f * playerController.accel * 0.5f);
        float maxPitch = Mathf.Min(0.9f, 0.3f + 1.2f * playerController.accel * 0.5f);
        //crashsource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        crashsource.PlayOneShot(crash1);
    }

    public void PlayTrafficExplosion(bool aggro, bool tornado)
    {
        trafficexplossource.pitch =  aggro ? UnityEngine.Random.Range(0.7f, 0.9f) : UnityEngine.Random.Range(0.8f, 1.2f);
        //trafficexplossource.volume = aggro ? 1 : 0.5f;
        if (tornado) tornadoexplossource.Play();
        else trafficexplossource.Play();
    }

    public void PlayLivesPowerupSound()
    {
        livesource.Play();
    }

    public void PlayAggro(bool zoomingOut)
    {
        if (zoomingOut) zoomoutsource.Play();
        else aggrosource.Play();
    }

    public void PlayWoosh(bool left)
    {
        leftwooshsource.pitch = Mathf.Min(playerController.accel + 0.5f, 2.5f);
        rightwooshsource.pitch = Mathf.Min(playerController.accel + 0.5f, 2.5f);
        leftwooshsource.outputAudioMixerGroup.audioMixer.SetFloat("pitchBend", 1f / leftwooshsource.pitch);
        if (left) leftwooshsource.PlayOneShot(woosh);
        else rightwooshsource.PlayOneShot(woosh);
    }

    public void TogglePauseGameAudio()
    {
        if (!isPaused)
        {
            // Pausing logic: Update states and pause currently playing audio.
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSources[i].isPlaying)
                {
                    Debug.Log(audioSources[i].name);
                    audioSourceStates[i] = true; // Mark as playing.
                    audioSources[i].Pause();
                }
                else
                {
                    audioSourceStates[i] = false; // Mark as not playing.
                }
            }

            // Pause current song.
            songs[currentSongIndex].Pause();

            // Pause current engine sound.
            //enginesounds[currentEngineSoundIndex].Pause();

            isPaused = true;
        }
        else
        {
            // Unpausing logic: Resume audio sources that were playing before pausing.
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSourceStates[i])
                {
                    audioSources[i].UnPause();
                }
            }

            // Unpause current song.
            songs[currentSongIndex].UnPause();

            // Unpause current engine sound.
            //enginesounds[currentEngineSoundIndex].UnPause();

            isPaused = false;
        }
    }


    /*------------------------------------ POWERUP AUDIO MANAGEMENT -----------------------------------*/
    public void ToggleBulletSound(float accel)
    {
        inBullet = !inBullet;

        if (inBullet)
        {
            //songs[currentSongIndex].pitch *= 1.2f;
            //songs[currentSongIndex].volume *= 1.5f;
            bulletsource.Play();

            originalEnginePitch = enginesounds[2].pitch;
            if (bulletEffectsCoroutine != null) StopCoroutine(bulletEffectsCoroutine);
            bulletEffectsCoroutine = StartCoroutine(BulletEffects());
            
            if (!engineScreamPlayed)
            {
                enginesounds[2].Play();
            }
        }
        else
        {
            songs[currentSongIndex].pitch /= 1.5f;
            songs[currentSongIndex].volume /= 2f;

            if (bulletEffectsCoroutine != null) StopCoroutine(bulletEffectsCoroutine);
            enginesounds[2].pitch = originalEnginePitch;
            enginesounds[2].volume /= 2f;
            if (!engineScreamPlayed)
            {
                enginesounds[2].Pause();
            }
        }
    }

    private IEnumerator BulletEffects()
    {
        float duration = 5.0f; // Duration for pitch and volume increase


        float startPitch = enginesounds[2].pitch;
        if (engineScreamPlayed) startPitch *= 1.05f;
        float targetPitch = startPitch * 1.5f;
        float startVolume = enginesounds[2].volume;
        float targetVolume = startVolume * 2f;

        float songStartPitch = songs[currentSongIndex].pitch;
        float songTargetPitch = songStartPitch * 1.5f;
        float songStartVolume = songs[currentSongIndex].volume;
        float songTargetVolume = songStartVolume * 2f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            enginesounds[2].pitch = Mathf.Lerp(startPitch, targetPitch, t);
            enginesounds[2].volume = Mathf.Lerp(startVolume, targetVolume, t);

            songs[currentSongIndex].pitch = Mathf.Lerp(songStartPitch, songTargetPitch, t); ;
            songs[currentSongIndex].volume = Mathf.Lerp(songStartVolume, songTargetVolume, t); ;

            yield return null;
        }

        yield return new WaitForSeconds(5.0f); // Hold high pitch and volume briefly

        // Reset to original pitch and volume
        //enginesounds[2].pitch = originalEnginePitch;
        //
    }


    public void ToggleAggroEffects()
    {
        inAggro = !inAggro;
        enginesounds[1].volume *= volumeModifier;
        enginesounds[2].volume *= volumeModifier;

        if (volumeModifier == 2f) volumeModifier = 0.5f;
        else if (volumeModifier == 0.5f) volumeModifier = 2f;

        if (inAggro)
        {
            trafficexplossource.volume *= 1.25f;
            crashsource.volume *= 2f;
            foreach (AudioSource audioSource in specificAudioSources)
            {
                if (audioSource == beepsource)
                {
                    audioSource.pitch *= 0.6f;
                }
                else audioSource.pitch *= 0.8f;
            }
            if (!engineScreamPlayed) enginesounds[2].Play();

        }
        else
        {
            trafficexplossource.volume /= 1.25f;
            crashsource.volume /= 2f;
            foreach (AudioSource audioSource in specificAudioSources)
            {
                // Return to the original pitch.
                if (originalPitches.ContainsKey(audioSource))
                    audioSource.pitch = originalPitches[audioSource];
            }
            if (!engineScreamPlayed) enginesounds[2].Stop();
        }

        AudioLowPassFilter lowPass = FindObjectOfType<AudioLowPassFilter>();
        if (lowPass) lowPass.enabled = inAggro;
    }


    /*---------------------------------------- OTHER FUNCTIONS ----------------------------------------*/
    private IEnumerator CrossfadeCoroutine(AudioSource sourceFrom, AudioSource sourceTo, float duration, bool isSong)
    {
        float elapsedTime = 0f;
        float originalSourceFromVolume = sourceFrom.volume;
        float originalSourceToVolume = sourceTo.volume;

        sourceTo.volume = 0f;
        sourceTo.Play();

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            if (isSong)
            {
                // Even crossfade for songs
                float t = elapsedTime / duration;
                sourceFrom.volume = Mathf.Lerp(originalSourceFromVolume, 0f, t);
                sourceTo.volume = Mathf.Lerp(0f, originalSourceToVolume, t);
            }
            else
            {
                float t = elapsedTime / duration;
                float tFadeIn = Mathf.Clamp01(elapsedTime / (duration / 3)); // This will fade in over 1/3 of the duration.
                sourceFrom.volume = Mathf.Lerp(originalSourceFromVolume, 0f, t);
                sourceTo.volume = Mathf.Lerp(0f, originalSourceToVolume, tFadeIn);
            }

            yield return null;
        }

        sourceFrom.Stop();
        sourceFrom.volume = originalSourceFromVolume; // Reset the volume of the old source.
        specificAudioSources[1] = sourceTo;
        originalPitches.Remove(sourceFrom);
        originalPitches[sourceTo] = sourceTo.pitch;
    }

    // Add audio source to main list along with its state (false).
    public void RegisterAudioSource(AudioSource source, int index)
    {
        audioSources.Add(source);
        audioSourceStates.Add(false);
        source.volume = originalSoundManagerAudioSourceVolumes[index] * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;
    }

    public void UpdateSoundManagerMusicVolumes(float value)
    {
        foreach (AudioSource songsource in songs)
        {
            songsource.volume = value;
        }
    }

    public void UpdateSoundManagerEffectsVolumes(float value)
    {
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null)
            {
                // Apply scaled volume using the original volume.
                audioSources[i].volume = originalSoundManagerAudioSourceVolumes[i] * value;
            }
        }
    }
}
