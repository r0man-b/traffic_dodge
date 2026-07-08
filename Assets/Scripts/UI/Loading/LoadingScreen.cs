using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Visual controller for the loading overlay. Owns the random placeholder
/// background, the loading bar fill, and the animated "LOADING" text.
/// Driven by <see cref="SceneLoader"/>.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Bottom background layer (always opaque, shows the current image).")]
    [SerializeField] private UnityEngine.UI.Image backgroundBase;
    [Tooltip("Top background layer (used to crossfade the next image in).")]
    [SerializeField] private UnityEngine.UI.Image backgroundFade;
    [SerializeField] private UnityEngine.UI.Image progressFill;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Placeholder Backgrounds (drop your 5 images here)")]
    [SerializeField] private Sprite[] backgrounds = new Sprite[5];

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float barSmoothing = 5f;
    [SerializeField] private string baseText = "LOADING";
    [SerializeField] private float dotInterval = 0.35f;
    [Tooltip("Seconds each background is shown before crossfading to the next.")]
    [SerializeField] private float crossfadeInterval = 5f;
    [Tooltip("Duration of the crossfade between two backgrounds.")]
    [SerializeField] private float crossfadeDuration = 1f;

    private float targetProgress;
    private float displayedProgress;
    private float dotTimer;
    private int dotCount;
    private int currentBgIndex = -1;
    private Coroutine crossfadeRoutine;

    /// <summary>True once the visible bar has caught up to the requested value.</summary>
    public bool IsBarSettled => Mathf.Abs(displayedProgress - targetProgress) < 0.005f;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        // Smoothly drive the bar toward the requested target (time-independent).
        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, barSmoothing * Time.unscaledDeltaTime);
        if (progressFill != null) progressFill.fillAmount = displayedProgress;

        // Animate the trailing dots on the LOADING text.
        if (loadingText != null)
        {
            dotTimer += Time.unscaledDeltaTime;
            if (dotTimer >= dotInterval)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
                loadingText.text = baseText + new string('.', dotCount);
            }
        }
    }

    /// <summary>Reveal the overlay, reset the bar and start the background crossfade cycle.</summary>
    public void Show()
    {
        targetProgress = 0f;
        displayedProgress = 0f;
        if (progressFill != null) progressFill.fillAmount = 0f;
        dotTimer = 0f;
        dotCount = 0;
        if (loadingText != null) loadingText.text = baseText;

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        StartBackgroundCycle();
    }

    /// <summary>Hide instantly with no fade (used on startup).</summary>
    public void HideImmediate()
    {
        StopBackgroundCycle();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>Request a bar value in the 0..1 range.</summary>
    public void SetProgress(float value)
    {
        targetProgress = Mathf.Clamp01(value);
    }

    /// <summary>Fade the overlay out and disable it.</summary>
    public IEnumerator FadeOut()
    {
        StopBackgroundCycle();
        canvasGroup.blocksRaycasts = false;
        float t = 0f;
        float startAlpha = canvasGroup.alpha;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // ---- Background crossfade --------------------------------------------

    private void StartBackgroundCycle()
    {
        StopBackgroundCycle();

        int first = NextBackgroundIndex(-1);

        // Base layer shows the first image at full opacity; fade layer starts hidden.
        if (backgroundBase != null)
        {
            if (first >= 0) backgroundBase.sprite = backgrounds[first];
            backgroundBase.enabled = backgroundBase.sprite != null;
            backgroundBase.color = WithAlpha(backgroundBase.color, 1f);
        }
        if (backgroundFade != null)
        {
            backgroundFade.enabled = backgroundFade.sprite != null;
            backgroundFade.color = WithAlpha(backgroundFade.color, 0f);
        }

        currentBgIndex = first;

        // Only cycle if at least two distinct images are available.
        if (CountAssigned() >= 2)
            crossfadeRoutine = StartCoroutine(CrossfadeCycle());
    }

    private void StopBackgroundCycle()
    {
        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }
    }

    private IEnumerator CrossfadeCycle()
    {
        while (true)
        {
            // Wait the display interval (unscaled so it runs while paused).
            float wait = 0f;
            while (wait < crossfadeInterval)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            int next = NextBackgroundIndex(currentBgIndex);
            if (next < 0 || next == currentBgIndex) continue;

            // Fade the top layer in over the base, then snap the base to match.
            if (backgroundFade != null && backgroundBase != null)
            {
                backgroundFade.sprite = backgrounds[next];
                backgroundFade.enabled = true;

                float t = 0f;
                while (t < crossfadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float a = crossfadeDuration > 0f ? Mathf.Clamp01(t / crossfadeDuration) : 1f;
                    backgroundFade.color = WithAlpha(backgroundFade.color, a);
                    yield return null;
                }

                // Promote the new image onto the base layer and hide the fade layer
                // so the next transition starts clean (no visible pop).
                backgroundBase.sprite = backgrounds[next];
                backgroundBase.color = WithAlpha(backgroundBase.color, 1f);
                backgroundFade.color = WithAlpha(backgroundFade.color, 0f);
            }

            currentBgIndex = next;
        }
    }

    /// <summary>Pick a random assigned background index different from <paramref name="exclude"/>.</summary>
    private int NextBackgroundIndex(int exclude)
    {
        if (backgrounds == null || backgrounds.Length == 0) return -1;

        // Collect indices of non-null sprites (optionally excluding the current one).
        var pool = new System.Collections.Generic.List<int>();
        for (int i = 0; i < backgrounds.Length; i++)
            if (backgrounds[i] != null && i != exclude) pool.Add(i);

        // If excluding left nothing (only one image), allow repeating it.
        if (pool.Count == 0)
        {
            for (int i = 0; i < backgrounds.Length; i++)
                if (backgrounds[i] != null) pool.Add(i);
        }

        if (pool.Count == 0) return -1;
        return pool[Random.Range(0, pool.Count)];
    }

    private int CountAssigned()
    {
        int c = 0;
        if (backgrounds != null)
            for (int i = 0; i < backgrounds.Length; i++)
                if (backgrounds[i] != null) c++;
        return c;
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
