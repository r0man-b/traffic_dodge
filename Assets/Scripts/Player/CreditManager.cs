using System.Collections;
using UnityEngine;
using TMPro;

public class CreditManager : MonoBehaviour
{
    public TMP_Text creditsText;          // TextMeshPro text component
    public bool changeDone = false;
    private Coroutine creditChangeCoroutine;
    public MenuSounds menuSounds;

    void Awake()
    {
        if (creditsText == null)
        {
            Debug.LogError("CreditManager: creditsText (TMP_Text) is not assigned.");
            return;
        }

        // Retrieve credits from SaveManager.
        long currentCredits = SaveManager.Instance.SaveData.GlobalCredits;

        // Initialize credits if they are zero or less.
        if (currentCredits <= 0)
        {
            currentCredits = 999_999_999;
            SaveManager.Instance.SaveData.GlobalCredits = currentCredits;
            SaveManager.Instance.SaveGame();
        }

        // Format the credits with commas and append " cr".
        creditsText.text = $"{currentCredits:N0} cr";
    }

    /// <summary>
    /// Public method to change the credit amount (supports long).
    /// </summary>
    public void ChangeCredits(long change, bool stop = false)
    {
        if (creditChangeCoroutine != null)
        {
            StopCoroutine(creditChangeCoroutine);
        }

        if (!stop)
            creditChangeCoroutine = StartCoroutine(AnimateCreditChange(change));
    }

    public long GetCredits()
    {
        return SaveManager.Instance.SaveData.GlobalCredits;
    }

    private IEnumerator AnimateCreditChange(long change)
    {
        changeDone = false;

        const int maxSteps = 100;
        const float stepDelay = 0.01f;

        long absChange = System.Math.Abs(change);
        int direction = change >= 0 ? 1 : -1;

        long currentCredits = SaveManager.Instance.SaveData.GlobalCredits;

        // Determine actual number of steps needed
        int steps = (int)System.Math.Min(absChange, maxSteps);

        // If no change is needed, exit immediately
        if (steps == 0)
        {
            changeDone = true;
            yield break;
        }

        long baseStepAmount = absChange / steps; // >= 1
        long remainder = absChange % steps;      // < steps

        for (int i = 0; i < steps; i++)
        {
            long stepAmount = baseStepAmount + (i < remainder ? 1L : 0L);

            // Apply step
            currentCredits += (long)direction * stepAmount;

            // Persist and update UI
            SaveManager.Instance.SaveData.GlobalCredits = currentCredits;
            creditsText.text = $"{currentCredits:N0} cr";

            if (menuSounds != null) menuSounds.PlayCreditChange();

            yield return new WaitForSeconds(stepDelay);
        }

        SaveManager.Instance.SaveGame();
        changeDone = true;
    }
}
