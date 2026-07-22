using UnityEngine;

public class WheelRotator : MonoBehaviour
{
    // When true, wheel rotation is suppressed (used by the tutorial).
    public bool tutorialActive = false;

    void Start()
    {
        tutorialActive = !SaveManager.Instance.SaveData.tutorialCompleted;
    }
    void Update()
    {
        if (!tutorialActive) transform.Rotate(1300 * Time.deltaTime, 0, 0);
    }
}
