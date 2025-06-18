using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateSetter : MonoBehaviour
{
    // Set the frame rate limit to 60 to bypass the default 30 fps setting on mobile.
    void Start()
    {
        Application.targetFrameRate = SaveManager.Instance.SaveData.frameRate;
    }
}
