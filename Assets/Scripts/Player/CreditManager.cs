using System.Collections;
using UnityEngine;
using TMPro;

public class CreditManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text creditsText;              // TextMeshPro text component
    public MenuSounds menuSounds;

    [Header("Animation")]
    [Tooltip("Maximum number of animation steps per change.")]
    public int maxSteps = 100;
    [Tooltip("Delay in seconds between animation steps.")]
    public float stepDelay = 0.01f;

    public bool changeDone = true;

    private Coroutine creditChangeCoroutine;
    private long _displayedCredits;           // What the UI is currently showing (as a number)
    private bool _initialized;
    private int _lastDigitCount;              // Number of digits the credits have

    private void OnEnable()
    {
        if (creditsText == null)
        {
            Debug.LogError("CreditManager: creditsText (TMP_Text) is not assigned.");
            return;
        }

        // Ensure a valid starting balance.
        long currentCredits = SaveManager.Instance.SaveData.GlobalCredits;
        if (currentCredits < 0)
        {
            currentCredits = 0;
            SaveManager.Instance.SaveData.GlobalCredits = currentCredits;
            SaveManager.Instance.SaveGame();
        }

        _displayedCredits = currentCredits;
        creditsText.text = $"{_displayedCredits:N0} cr";
        _initialized = true;
    }

    /// <summary>
    /// Reads from SaveData and snaps the UI to the authoritative value (no animation).
    /// Useful if you need to guarantee visual consistency after major scene transitions.
    /// </summary>
    public void SnapToSaved()
    {
        long saved = SaveManager.Instance.SaveData.GlobalCredits;
        _displayedCredits = saved;
        if (creditsText != null) creditsText.text = $"{_displayedCredits:N0} cr";
    }

    /// <summary>
    /// Attempts to spend a positive 'cost'. Returns false if insufficient funds.
    /// On success, the deduction is written to SaveData immediately and the UI animates down.
    /// </summary>
    public bool TrySpend(long cost)
    {
        if (cost <= 0) return true; // nothing to spend
        long current = SaveManager.Instance.SaveData.GlobalCredits;
        if (current < cost) return false;

        long target = current - cost;
        ApplyAndAnimateTo(target);
        return true;
    }

    /// <summary>
    /// Adds (or subtracts) credits by 'delta'. The new balance is persisted immediately,
    /// then the UI animates to that saved target. Negative deltas are allowed.
    /// </summary>
    public void ChangeCredits(long delta, bool stop = false)
    {
        if (stop)
        {
            // Caller explicitly wants to stop animation without applying a new change.
            if (creditChangeCoroutine != null) StopCoroutine(creditChangeCoroutine);
            changeDone = true;
            return;
        }

        long current = SaveManager.Instance.SaveData.GlobalCredits;
        // Clamp to non-negative balance; adjust delta to not go below 0.
        long target = current + delta;
        if (target < 0) target = 0;

        ApplyAndAnimateTo(target);
    }

    private void UpdateCreditText(long value)
    {
        if (creditsText == null) return;

        string formatted = $"{value:N0} cr";

        // Compute digit count without commas or suffix.
        string digitsOnly = value.ToString();
        int digitCount = digitsOnly.Length;

        bool digitCountChanged = digitCount != _lastDigitCount;
        _lastDigitCount = digitCount;

        // Enable autosize only when necessary.
        if (digitCountChanged)
        {
            creditsText.enableAutoSizing = true;
            creditsText.text = formatted;
            creditsText.ForceMeshUpdate();  // Allow TMP to resize once
            creditsText.enableAutoSizing = false;
        }
        else
        {
            creditsText.enableAutoSizing = false;
            creditsText.text = formatted;
        }
    }

    /// <summary>
    /// Returns the authoritative saved amount.
    /// </summary>
    public long GetCredits()
    {
        return SaveManager.Instance.SaveData.GlobalCredits;
    }

    // -------------------- Internals --------------------

    private void ApplyAndAnimateTo(long target)
    {
        // Persist authoritative value first.
        SaveManager.Instance.SaveData.GlobalCredits = target;
        SaveManager.Instance.SaveGame();

        // Start / restart animation toward the authoritative value.
        if (creditChangeCoroutine != null) StopCoroutine(creditChangeCoroutine);
        creditChangeCoroutine = StartCoroutine(AnimateDisplayTo(target));
    }

    private IEnumerator AnimateDisplayTo(long target)
    {
        if (!_initialized)
        {
            // If not initialized yet, just snap.
            _displayedCredits = target;
            if (creditsText != null) creditsText.text = $"{_displayedCredits:N0} cr";
            yield break;
        }

        changeDone = false;

        long start = _displayedCredits;
        if (start == target)
        {
            // Nothing to animate.
            changeDone = true;
            yield break;
        }

        long distance = System.Math.Abs(target - start);
        int steps = (int)System.Math.Min(distance, maxSteps);
        if (steps <= 0)
        {
            _displayedCredits = target;
            if (creditsText != null) UpdateCreditText(_displayedCredits);
            changeDone = true;
            yield break;
        }

        long baseStep = distance / steps;   // >= 1
        long remainder = distance % steps;  // < steps
        int direction = target > start ? 1 : -1;

        for (int i = 0; i < steps; i++)
        {
            long step = baseStep + (i < remainder ? 1L : 0L);
            _displayedCredits += direction * step;

            if (creditsText != null)
                UpdateCreditText(_displayedCredits);

            if (menuSounds != null)
                menuSounds.PlayCreditChange();

            yield return new WaitForSeconds(stepDelay);
        }

        // Snap to target to avoid rounding residue.
        _displayedCredits = target;
        if (creditsText != null)
            UpdateCreditText(_displayedCredits);

        changeDone = true;
    }
}
