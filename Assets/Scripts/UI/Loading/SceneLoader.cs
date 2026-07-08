using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent, self-bootstrapping scene loader. Shows a <see cref="LoadingScreen"/>
/// overlay on game boot and during every scene transition, driving a loading bar
/// from the real async progress (with a minimum on-screen time so it never flashes).
///
/// Use <see cref="Go(int)"/> / <see cref="Go(string)"/> from anywhere to switch scenes.
/// A prefab named "SceneLoader" must exist under a Resources folder.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private LoadingScreen loadingScreen;
    [Tooltip("Minimum time the loading screen stays up during a scene change.")]
    [SerializeField] private float minDisplayTime = 1.75f;
    [Tooltip("How long the boot loading screen is shown when the game launches.")]
    [SerializeField] private float bootDisplayTime = 2.5f;

    private bool isBusy;

    // ---- Public API -------------------------------------------------------

    /// <summary>Load a scene by build index, falling back to a direct load if no loader exists.</summary>
    public static void Go(int buildIndex)
    {
        if (Instance == null) { SceneManager.LoadScene(buildIndex); return; }
        Instance.LoadScene(buildIndex);
    }

    /// <summary>Load a scene by name, falling back to a direct load if no loader exists.</summary>
    public static void Go(string sceneName)
    {
        if (Instance == null) { SceneManager.LoadScene(sceneName); return; }
        Instance.LoadScene(sceneName);
    }

    public void LoadScene(int buildIndex)
    {
        if (isBusy) return;
        StartCoroutine(LoadRoutine(buildIndex, null));
    }

    public void LoadScene(string sceneName)
    {
        if (isBusy) return;
        StartCoroutine(LoadRoutine(-1, sceneName));
    }

    // ---- Bootstrap / lifecycle -------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<GameObject>("SceneLoader");
        if (prefab == null)
        {
            Debug.LogWarning("[SceneLoader] No 'SceneLoader' prefab found in a Resources folder.");
            return;
        }
        Instantiate(prefab);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (loadingScreen != null) loadingScreen.HideImmediate();
    }

    private void Start()
    {
        // Boot splash the first time the game launches.
        if (loadingScreen != null) StartCoroutine(BootRoutine());
    }

    // ---- Routines ---------------------------------------------------------

    private IEnumerator BootRoutine()
    {
        isBusy = true;
        Time.timeScale = 1f;
        loadingScreen.Show();

        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < bootDisplayTime)
        {
            loadingScreen.SetProgress(Mathf.Clamp01((Time.unscaledTime - start) / bootDisplayTime));
            yield return null;
        }

        loadingScreen.SetProgress(1f);
        while (!loadingScreen.IsBarSettled) yield return null;
        yield return loadingScreen.FadeOut();
        isBusy = false;
    }

    private IEnumerator LoadRoutine(int buildIndex, string sceneName)
    {
        isBusy = true;
        Time.timeScale = 1f;
        loadingScreen.Show();
        yield return null; // give the overlay one frame to render

        float start = Time.unscaledTime;
        AsyncOperation op = sceneName != null
            ? SceneManager.LoadSceneAsync(sceneName)
            : SceneManager.LoadSceneAsync(buildIndex);
        op.allowSceneActivation = false;

        bool activated = false;
        while (!op.isDone)
        {
            // Bar reflects the real async load progress (0..0.9 maps to 0..1).
            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);
            loadingScreen.SetProgress(loadProgress);

            bool loadReady = op.progress >= 0.9f;
            bool timeReady = (Time.unscaledTime - start) >= minDisplayTime;
            if (loadReady && timeReady && !activated)
            {
                loadingScreen.SetProgress(1f);
                op.allowSceneActivation = true;
                activated = true;
            }
            yield return null;
        }

        loadingScreen.SetProgress(1f);
        while (!loadingScreen.IsBarSettled) yield return null;
        yield return loadingScreen.FadeOut();
        isBusy = false;
    }
}
