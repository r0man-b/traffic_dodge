using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Drives the one-time interactive tutorial on the player's first Game run.
// Flow:
//   * Freeze before the countdown, show an intro popup (START / SKIP TUTORIAL) styled like
//     the end-game rewards popup.
//   * On START: spawn 5 big-rig walls (100 z apart) with specific lane gaps, run the race,
//     and guide the player through 5 paused popup steps (tap left, tap right, hold left,
//     hold right, lane-split), each with an OK button and an animated hand indicator.
//   * Finale: a popup showcasing the 4 powerups as floating icons + a FINISH TUTORIAL
//     button that reloads the scene.
//   * On SKIP (or FINISH): mark the tutorial complete, save, and reload the scene so a
//     normal race (with traffic) begins.
// While active it suppresses normal traffic (via PrefabManager.tutorialActive) and keeps
// the player invincible so a mistimed input can't end the run mid-lesson.
public class TutorialManager : MonoBehaviour
{
    [Header("Hand indicator")]
    [SerializeField] private Sprite handSprite;

    [Header("Popup style (match end-game rewards popup)")]
    [SerializeField] private Sprite panelFrameSprite;   // Sci-Fi frame border
    [SerializeField] private Sprite panelBackSprite;    // sliced window background
    [SerializeField] private Sprite buttonFrameSprite;  // button frame

    [Header("Powerup showcase icons (Lives, Aggro, Tornado, Bullet)")]
    [SerializeField] private Texture2D[] powerupIcons = new Texture2D[4];

    // --- Scene references ---
    private PlayerController playerController;
    private PrefabManager prefabManager;
    private UIManager uiManager;
    private Transform canvasTransform;

    // --- UI ---
    private GameObject overlayRoot;
    private TextMeshProUGUI bodyText;
    private RectTransform buttonRow;
    private RectTransform powerupRow;
    private Image handLeft;
    private Image handRight;
    private TMP_FontAsset font;

    private readonly Color teal = new(0.467f, 0.957f, 1f, 1f);

    // --- Flow state ---
    private enum HandMode { None, TapLeft, TapRight, HoldLeft, HoldRight, SplitBoth }
    private HandMode handMode = HandMode.None;

    // Desired pause state, enforced every frame in LateUpdate so other scripts' timeScale
    // writes (e.g. PauseMenu resetting timeScale in Start) can't override the tutorial.
    private bool wantPaused = false;
    private bool tutorialRunning = false;

    private const float FirstGroupDistance = 175f;
    private const float GroupSpacing = 100f;
    private const float PassMargin = 6f;
    private const float DriveBeforeFirstStep = 1f;

    private float PlayerZ =>
        (playerController != null && playerController.carObject != null)
            ? playerController.carObject.transform.position.z
            : (playerController != null ? playerController.transform.position.z : 0f);

    private void Awake()
    {
        // Freeze as early as possible so the intro popup appears before the race countdown.
        if (!SaveManager.Instance.SaveData.tutorialCompleted)
        {
            wantPaused = true;
            tutorialRunning = true;
            Time.timeScale = 0f;
        }
    }

    // Enforce the tutorial's desired pause state after every other script has run this frame,
    // so a script that resets Time.timeScale in Start/Update (e.g. PauseMenu, SceneLoader boot)
    // can't override it.
    private void LateUpdate()
    {
        if (tutorialRunning)
            Time.timeScale = wantPaused ? 0f : 1f;
    }

    private void Start()
    {
        if (SaveManager.Instance.SaveData.tutorialCompleted)
        {
            enabled = false;
            return;
        }

        GameObject playerObj = GameObject.Find("PlayerCar");
        GameObject prefabObj = GameObject.Find("PrefabManager");
        GameObject canvasObj = GameObject.Find("Canvas");

        if (playerObj == null || prefabObj == null || canvasObj == null)
        {
            Debug.LogError("TutorialManager: missing required scene objects (PlayerCar / PrefabManager / Canvas). Disabling.");
            Time.timeScale = 1f;
            enabled = false;
            return;
        }

        playerController = playerObj.GetComponent<PlayerController>();
        prefabManager = prefabObj.GetComponent<PrefabManager>();
        uiManager = canvasObj.GetComponent<UIManager>();
        canvasTransform = canvasObj.transform;

        font = (uiManager != null && uiManager.countdown != null)
            ? uiManager.countdown.font
            : TMP_Settings.defaultFontAsset;

        prefabManager.tutorialActive = true;

        BuildUI();
        StartCoroutine(RunTutorial());
    }

    // ---------------------------------------------------------------- UI construction
    private void BuildUI()
    {
        // Full-screen overlay with a dim backdrop + centered popup panel.
        overlayRoot = new GameObject("TutorialOverlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(canvasTransform, false);
        StretchFull(overlayRoot.GetComponent<RectTransform>());

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(overlayRoot.transform, false);
        StretchFull(dim.GetComponent<RectTransform>());
        Image dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.6f);
        dimImg.raycastTarget = true;

        // Panel.
        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(overlayRoot.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.22f, 0.26f);
        prt.anchorMax = new Vector2(0.78f, 0.74f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        if (panelBackSprite != null)
        {
            GameObject back = new GameObject("Background", typeof(RectTransform), typeof(Image));
            back.transform.SetParent(panel.transform, false);
            StretchFull(back.GetComponent<RectTransform>());
            Image backImg = back.GetComponent<Image>();
            backImg.sprite = panelBackSprite;
            backImg.type = Image.Type.Sliced;
            backImg.color = Color.white;
        }
        if (panelFrameSprite != null)
        {
            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(panel.transform, false);
            StretchFull(frame.GetComponent<RectTransform>());
            Image frameImg = frame.GetComponent<Image>();
            frameImg.sprite = panelFrameSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;
        }

        // Body text.
        GameObject textGO = new GameObject("BodyText", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        bodyText = textGO.AddComponent<TextMeshProUGUI>();
        bodyText.font = font;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.enableWordWrapping = true;
        bodyText.fontSize = 40f;
        bodyText.color = teal;
        bodyText.raycastTarget = false;
        RectTransform trt = bodyText.rectTransform;
        trt.anchorMin = new Vector2(0.08f, 0.42f);
        trt.anchorMax = new Vector2(0.92f, 0.92f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Powerup showcase row (finale only).
        GameObject prow = new GameObject("PowerupRow", typeof(RectTransform));
        prow.transform.SetParent(panel.transform, false);
        powerupRow = prow.GetComponent<RectTransform>();
        powerupRow.anchorMin = new Vector2(0.08f, 0.24f);
        powerupRow.anchorMax = new Vector2(0.92f, 0.42f);
        powerupRow.offsetMin = Vector2.zero;
        powerupRow.offsetMax = Vector2.zero;
        prow.SetActive(false);

        // Button row (bottom-middle).
        GameObject brow = new GameObject("ButtonRow", typeof(RectTransform));
        brow.transform.SetParent(panel.transform, false);
        buttonRow = brow.GetComponent<RectTransform>();
        buttonRow.anchorMin = new Vector2(0.1f, 0.06f);
        buttonRow.anchorMax = new Vector2(0.9f, 0.2f);
        buttonRow.offsetMin = Vector2.zero;
        buttonRow.offsetMax = Vector2.zero;

        // Hands (siblings of overlay, rendered on top so visible over the dim backdrop and during steps).
        handLeft = CreateHand("TutorialHandLeft", new Vector2(0.28f, 0.34f));
        handRight = CreateHand("TutorialHandRight", new Vector2(0.72f, 0.34f));

        overlayRoot.SetActive(false);
    }

    private Image CreateHand(string objName, Vector2 anchor)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasTransform, false);
        Image img = go.GetComponent<Image>();
        img.sprite = handSprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 200f);
        rt.anchoredPosition = Vector2.zero;
        go.SetActive(false);
        return img;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    // Build a styled button in the button row. Returns the created GameObject.
    private GameObject CreateButton(string label, Action onClick)
    {
        GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(buttonRow, false);
        Image img = go.GetComponent<Image>();
        if (buttonFrameSprite != null)
        {
            img.sprite = buttonFrameSprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.05f, 0.2f, 0.25f, 0.9f);
        }

        Button btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.85f, 0.98f, 1f, 1f);
        cb.pressedColor = new Color(0.47f, 0.9f, 1f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.05f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 30f;
        tmp.color = teal;
        tmp.raycastTarget = false;
        StretchFull(tmp.rectTransform);

        return go;
    }

    private void ClearButtons()
    {
        for (int i = buttonRow.childCount - 1; i >= 0; i--)
            Destroy(buttonRow.GetChild(i).gameObject);
    }

    // Lay out current button-row children horizontally (1 or 2 buttons, centered).
    private void LayoutButtons()
    {
        int n = buttonRow.childCount;
        if (n == 0) return;
        float gap = 0.06f;
        float w = (1f - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            RectTransform rt = buttonRow.GetChild(i).GetComponent<RectTransform>();
            float x0 = i * (w + gap);
            rt.anchorMin = new Vector2(x0, 0f);
            rt.anchorMax = new Vector2(x0 + w, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    // ---------------------------------------------------------------- Tutorial flow
    private IEnumerator RunTutorial()
    {
        // 1) Intro popup (game frozen from Awake).
        bool started = false;
        bool skipped = false;
        ShowPopup("<size=52><b>WELCOME TO TRAFFIC DODGE</b></size>\n\n<size=34>This quick tutorial will teach you how to weave through traffic. Placeholder text for now.</size>");
        ClearButtons();
        CreateButton("START TUTORIAL", () => started = true);
        CreateButton("SKIP TUTORIAL", () => skipped = true);
        LayoutButtons();
        yield return new WaitUntil(() => started || skipped);

        if (skipped)
        {
            CompleteAndReload();
            yield break;
        }

        HidePopup();

        // Resume so the countdown runs. Keep the player safe throughout the tutorial.
        SetPaused(false);
        playerController.numlives = 5;
        if (playerController.pauseButton != null) playerController.pauseButton.SetActive(false);

        // 2) Spawn the 5 big-rig walls ahead of the player (100 z apart) with specific gaps.
        float baseZ = PlayerZ + FirstGroupDistance;
        float g1 = prefabManager.SpawnBigRigWall(baseZ + 0 * GroupSpacing, 3);   // gap lane 3
        float g2 = prefabManager.SpawnBigRigWall(baseZ + 1 * GroupSpacing, 4);   // gap lane 4
        float g3 = prefabManager.SpawnBigRigWall(baseZ + 3 * GroupSpacing, 0);   // gap lane 0
        float g4 = prefabManager.SpawnBigRigWall(baseZ + 5 * GroupSpacing, 7);   // gap lane 7
        float g5 = prefabManager.SpawnBigRigWall(baseZ + 5.75f * GroupSpacing, -1);  // no gap (lane-split)

        // Wait for the race to actually start, then let the player drive a moment.
        yield return new WaitUntil(() => playerController.raceStarted);
        yield return new WaitForSecondsRealtime(DriveBeforeFirstStep);


        // 3) Guided steps.
        yield return DoStep(
            "<size=44><b>STEP 1</b></size>\n\n<size=34>Tap the LEFT side of the screen to change over one lane to the left.</size>",
            HandMode.TapLeft, g1);

        yield return DoStep(
            "<size=44><b>STEP 2</b></size>\n\n<size=34>Tap the RIGHT side of the screen to change over one lane to the right.</size>",
            HandMode.TapRight, g2);

        yield return DoStep(
            "<size=44><b>STEP 3</b></size>\n\n<size=34>Hold the LEFT side of the screen to change over multiple lanes to the left.</size>",
            HandMode.HoldLeft, g3);

        yield return DoStep(
            "<size=44><b>STEP 4</b></size>\n\n<size=34>Hold the RIGHT side of the screen to change over multiple lanes to the right.</size>",
            HandMode.HoldRight, g4);

        yield return DoStep(
            "<size=44><b>STEP 5</b></size>\n\n<size=34>Tap BOTH sides of the screen at once to lane-split between traffic. Lane-splits have a 5 second cooldown after being activated.</size>",
            HandMode.SplitBoth, g5);

        // 4) Finale — powerup showcase.
        yield return new WaitForSecondsRealtime(1f);
        SetPaused(true);
        SetHands(HandMode.None);
        ShowFinalePopup();
        bool finished = false;
        ClearButtons();
        CreateButton("FINISH TUTORIAL", () => finished = true);
        LayoutButtons();
        yield return new WaitUntil(() => finished);

        CompleteAndReload();
    }

    // One guided step: pause + popup(OK) + hand indicator, then wait for the player to pass the group.
    private IEnumerator DoStep(string message, HandMode mode, float groupZ)
    {
        SetPaused(true);
        ShowPopup(message);
        bool ok = false;
        ClearButtons();
        CreateButton("OK", () => ok = true);
        LayoutButtons();
        SetHands(mode);
        yield return new WaitUntil(() => ok);

        HidePopup();
        SetPaused(false);

        // Hand stays visible during the maneuver; wait until the player clears the wall.
        yield return new WaitUntil(() => PlayerZ > groupZ + PassMargin);
        SetHands(HandMode.None);
    }

    // ---------------------------------------------------------------- Popup helpers
    private void ShowPopup(string message)
    {
        overlayRoot.SetActive(true);
        powerupRow.gameObject.SetActive(false);
        bodyText.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
        bodyText.text = message;
    }

    private void ShowFinalePopup()
    {
        overlayRoot.SetActive(true);
        // Raise the body text to make room for the powerup row.
        bodyText.rectTransform.anchorMin = new Vector2(0.08f, 0.5f);
        bodyText.text = "<size=48><b>TUTORIAL COMPLETE!</b></size>\n\n<size=32>Grab these powerups during a race:\nLives  \u2022  Aggro  \u2022  Tornado  \u2022  Bullet</size>";

        // Clear any previous icons.
        for (int i = powerupRow.childCount - 1; i >= 0; i--)
            Destroy(powerupRow.GetChild(i).gameObject);

        powerupRow.gameObject.SetActive(true);
        int count = powerupIcons != null ? powerupIcons.Length : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject ball = new GameObject("Powerup" + i, typeof(RectTransform), typeof(RawImage));
            ball.transform.SetParent(powerupRow, false);
            RawImage raw = ball.GetComponent<RawImage>();
            raw.texture = powerupIcons[i];
            raw.raycastTarget = false;
            RectTransform rt = raw.rectTransform;
            float w = 1f / count;
            float cx = (i + 0.5f) * w;
            rt.anchorMin = new Vector2(cx, 0.5f);
            rt.anchorMax = new Vector2(cx, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 200f);
            ball.AddComponent<TutorialFloatBob>().Init(i * 0.5f);
        }
    }

    private void HidePopup()
    {
        overlayRoot.SetActive(false);
    }

    // ---------------------------------------------------------------- Hands
    private void SetHands(HandMode mode)
    {
        handMode = mode;
        bool left = mode == HandMode.TapLeft || mode == HandMode.HoldLeft || mode == HandMode.SplitBoth;
        bool right = mode == HandMode.TapRight || mode == HandMode.HoldRight || mode == HandMode.SplitBoth;
        handLeft.gameObject.SetActive(left);
        handRight.gameObject.SetActive(right);
    }

    private void Update()
    {
        if (handMode == HandMode.None) return;
        float t = Time.unscaledTime;

        // Tap = quick in-place pulse. Hold = pressed + drift in the hold direction.
        if (handLeft.gameObject.activeSelf) AnimateHand(handLeft, t, -1);
        if (handRight.gameObject.activeSelf) AnimateHand(handRight, t, +1);
    }

    private void AnimateHand(Image hand, float t, int side /* -1 left, +1 right */)
    {
        bool hold = handMode == HandMode.HoldLeft || handMode == HandMode.HoldRight;
        RectTransform rt = hand.rectTransform;

        if (hold)
        {
            // Pressed-down scale with a slow breathe, drifting in the hold direction.
            float breathe = (Mathf.Sin(t * 4f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.8f, 0.92f, breathe);
            rt.localScale = new Vector3(scale, scale, 1f);
            float drift = Mathf.Repeat(t * 0.6f, 1f); // 0..1 loop
            rt.anchoredPosition = new Vector2(side * drift * 70f, 0f);
        }
        else
        {
            // Quick tap pulse.
            float pulse = (Mathf.Sin(t * 8f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.82f, 1.15f, pulse);
            rt.localScale = new Vector3(scale, scale, 1f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    // ---------------------------------------------------------------- State helpers
    private void SetPaused(bool paused)
    {
        wantPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void CompleteAndReload()
    {
        tutorialRunning = false;
        wantPaused = false;
        SaveManager.Instance.SaveData.tutorialCompleted = true;
        SaveManager.Instance.SaveGame();
        Time.timeScale = 1f;
        SceneLoader.Go(SceneManager.GetActiveScene().buildIndex);
    }
}

// Small floating bob for the powerup showcase icons.
// Uses unscaled time so it continues animating while paused.
public class TutorialFloatBob : MonoBehaviour
{
    private RectTransform rt;
    private float phase;
    private float baseY;
    private float i = 0f;

    public void Init(float phaseOffset)
    {
        rt = GetComponent<RectTransform>();
        phase = phaseOffset;
        baseY = rt.anchoredPosition.y;
    }

    private void Update()
    {
        if (rt == null)
            return;

        float y = baseY + Mathf.Sin(Time.unscaledTime * 2f + phase) * 10f;

        Vector2 position = rt.anchoredPosition;
        position.y = y;
        rt.anchoredPosition = position;

        rt.localRotation = Quaternion.Euler(0f, 0f, i);
        i += 2.5f;
    }
}
