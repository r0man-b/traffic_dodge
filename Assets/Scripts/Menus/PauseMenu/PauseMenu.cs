using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public bool gamePaused = false;

    public void TogglePause()
    {
        gamePaused = !gamePaused;

        if (gamePaused)
        {
            // Pause the game.
            this.gameObject.SetActive(true);
            Time.timeScale = 0f;
            //AudioListener.pause = true;
        }
        else
        {
            // Resume the game.
            this.gameObject.SetActive(false);
            Time.timeScale = 1f;
            //AudioListener.pause = false;
        }
    }

}
