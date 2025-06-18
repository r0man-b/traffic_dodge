using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarageMusic : MonoBehaviour
{
    // Array to store songs.
    public AudioSource songsource;
    public AudioSource[] songs;
    public int currentSongIndex = 0;
    private bool songPlayed;

    // Start is called before the first frame update
    void Start()
    {
        // Set up car song radio.
        songs = songsource.GetComponents<AudioSource>();
        foreach (AudioSource song in songs)
        {
            if (song != null)
            {
                song.volume = SaveManager.Instance.SaveData.MusicVolume;
            }
        }

        // Load the last song index from SaveData.
        SaveData saveData = SaveManager.Instance.SaveData;
        currentSongIndex = saveData.LastGarageSongIndex;

        // Increment the index by 1 and wrap around if needed.
        currentSongIndex = (currentSongIndex + 1) % songs.Length;

        // Save the updated index to SaveData for the next time.
        saveData.LastGarageSongIndex = currentSongIndex;
        SaveManager.Instance.SaveGame();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Begin playing the song.
        if (!songPlayed)
        {
            songs[currentSongIndex].Play();
            songPlayed = true;
        }

        // Check if the current song has finished playing.
        if (songPlayed && !songs[currentSongIndex].isPlaying)
        {
            currentSongIndex = (currentSongIndex + 1) % songs.Length; // Cycle to the next song.

            // Save the updated index to SaveData.
            SaveData saveData = SaveManager.Instance.SaveData;
            saveData.LastGarageSongIndex = currentSongIndex;
            SaveManager.Instance.SaveGame();

            songs[currentSongIndex].Play(); // Play the next song.
        }
    }
}
