using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class AudioVisualizer : MonoBehaviour
{
    // Variables for the visualizer bars.
    private float minHeight = 24.0f;
    private float maxHeight = 425.0f;
    public float sensitivity = 0.025f;
    public Color visualizerColor = Color.yellow;
    public int visualizerSamples = 64;
    AudioVisualizerObject[] visualizerObjects;

    // Cached rectTransforms and images for optimization.
    private RectTransform[] visualizerRects;
    private Image[] visualizerImages;

    // Variables for the audio.
    public AudioSource songsource;
    public AudioClip audioClip;
    public VolumeProfile profile;

    // Misc. variables.
    private PlayerController playerController;
    private SoundManager soundManager;

    void Start()
    {
        // Initialize visualizer bars.
        visualizerObjects = GetComponentsInChildren<AudioVisualizerObject>();

        // Cache RectTransforms and Images for optimization.
        visualizerRects = new RectTransform[visualizerObjects.Length];
        visualizerImages = new Image[visualizerObjects.Length];
        for (int i = 0; i < visualizerObjects.Length; i++)
        {
            visualizerRects[i] = visualizerObjects[i].GetComponent<RectTransform>();
            visualizerImages[i] = visualizerObjects[i].GetComponent<Image>();
        }

        if (!audioClip) return;

        // Find 'PlayerController' script which contains variable 'gameEnd' so we know when the game has ended.
        GameObject PlayerCarObject = GameObject.Find("PlayerCar");
        playerController = PlayerCarObject.GetComponent<PlayerController>();

        // Find 'SoundManager' script which we need access to for playing sounds.
        GameObject SoundManagerObject = GameObject.Find("SoundManager");
        soundManager = SoundManagerObject.GetComponent<SoundManager>();
    }

    void FixedUpdate()
    {
        // Get the current song.
        songsource = soundManager.songs[soundManager.currentSongIndex];

        // Get the spectrum data from the currently playing song.
        float[] spectrumData = songsource.GetSpectrumData(visualizerSamples, 0, FFTWindow.Rectangular);

        for (int i = 0; i < visualizerObjects.Length; i++)
        {
            // Get current size of the visualizer bars.
            Vector2 newSize = visualizerRects[i].rect.size;

            // Set the current size of the visualizer bars to the current spectrum data.
            float size = spectrumData[i] * (maxHeight - minHeight) * 10.0f;
            newSize.y = Mathf.Clamp(Mathf.Lerp(newSize.y, minHeight + size, sensitivity), minHeight, maxHeight);
            visualizerRects[i].sizeDelta = newSize;

            // Make the first two visualizer bars invisible as they are too large.
            if (playerController.gameEnd || i == 0 || i == 1) visualizerImages[i].color = new Color(0, 0, 0, 0);

            // Set the colour of the rest of the bars.
            else visualizerImages[i].color = visualizerColor;
        }
    }
}
