using System.Collections;
using UnityEngine;
using TMPro; // Make sure to include this namespace for TextMeshPro.

public class CreditManager : MonoBehaviour
{
    public TMP_Text creditsText; // TMP Text component.
    public bool changeDone = false;
    private Coroutine creditChangeCoroutine;
    private AudioSource creditincreasesource;
    private float originalCreditIncreaseSourceVolume;

    void Awake()
    {
        if (creditsText == null)
        {
            Debug.LogError("TMP_Text component not found on the GameObject.");
            return;
        }

        // Retrieve credits from SaveManager.
        long currentCredits = SaveManager.Instance.SaveData.GlobalCredits;

        // Get the credit increase sound component.
        creditincreasesource = GetComponent<AudioSource>();
        originalCreditIncreaseSourceVolume = creditincreasesource.volume;

        // Multiply audio source volume by global volume multiplier.
        creditincreasesource.volume = originalCreditIncreaseSourceVolume * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;

        // Initialize credits if they are zero or less.
        if (currentCredits <= 0)
        {
            currentCredits = 999999999;
            SaveManager.Instance.SaveData.GlobalCredits = currentCredits;
            SaveManager.Instance.SaveGame(); // Save the initial credits.
        }

        // Format the credits with commas and append " cr".
        creditsText.text = $"{currentCredits:n0} cr";
    }

    // Public method to change the credit amount.
    public void ChangeCredits(int change, bool stop = false)
    {
        if (creditChangeCoroutine != null)
        {
            StopCoroutine(creditChangeCoroutine); // Stop the current coroutine if it's running.
        }
        
        if (!stop) creditChangeCoroutine = StartCoroutine(AnimateCreditChange(change));
    }

    public long GetCredits()
    {
        return SaveManager.Instance.SaveData.GlobalCredits;
    }

    private IEnumerator AnimateCreditChange(int change)
    {
        changeDone = false;

        const int maxSteps = 100;                  // 2 seconds / 0.01s = 200 updates
        const float stepDelay = 0.01f;
        int absChange = Mathf.Abs(change);
        int direction = change >= 0 ? 1 : -1;

        long currentCredits = SaveManager.Instance.SaveData.GlobalCredits;

        // Determine actual number of steps needed
        int steps = Mathf.Min(absChange, maxSteps);

        // If no change is needed, exit immediately
        if (steps == 0)
        {
            changeDone = true;
            yield break;
        }

        int baseStepAmount = absChange / steps;
        int remainder = absChange % steps;

        for (int i = 0; i < steps; i++)
        {
            int stepAmount = baseStepAmount + (i < remainder ? 1 : 0);
            currentCredits += direction * stepAmount;

            SaveManager.Instance.SaveData.GlobalCredits = currentCredits;
            creditsText.text = $"{currentCredits:n0} cr";
            creditincreasesource.Play();

            yield return new WaitForSeconds(stepDelay);
        }

        SaveManager.Instance.SaveGame();
        changeDone = true;
    }





}
