using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioSource MenuMusic;
    public GameObject MainMenuUI;
    public GameObject GarageUI;
    public GameObject CarSelectorUI;
    private void Awake()
    {
        // Set the volume of MenuMusic based on saved settings
        if (MenuMusic != null)
        {
            MenuMusic.volume = SaveManager.Instance.SaveData.MusicVolume;
        }
    }

    // Start the game from the main menu.
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        MenuMusic.Stop();
    }

    // Replay the current level.
    public void Replay()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Enter/exit the garage.
    public void EnterGarage(bool entering)
    {
        if (entering)
        {
            MainMenuUI.SetActive(false);
            GarageUI.SetActive(true);
            CarSelectorUI.SetActive(true);
        }
        else
        {
            MainMenuUI.SetActive(true);
            GarageUI.SetActive(false);
            CarSelectorUI.SetActive(false);
        }
    }

    // Enter the shop.
    public void Shop()
    {
        SaveSystem.Reset(); // Delete all JSON saved data.
        Debug.Log("All JSON saved data deleted.");
    }

    // Exit the game entirely.
    public void QuitGame()
    {
        Application.Quit();
    }
}
