using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using LeanTween;

public class UIManager : MonoBehaviour
{
    // UI variables.
    public TextMeshProUGUI distance;
    public TextMeshProUGUI speedo;
    public TextMeshProUGUI countdown;
    public TextMeshProUGUI gameover;
    public TextMeshProUGUI credits;
    public TextMeshProUGUI nitrocount;
    public GameObject game_end_widgets;
    public GameObject distance_speed_stats;
    public GameObject record_stats;
    public GameObject replayButton;
    public GameObject quitButton;
    private readonly float bulletSpeedoFlashDuration = 0.5f;
    private float bulletFlashTimeElapsed = 0f;
    public int bulletFlashCounter = 1;
    private Color imageColor;
    private Color yellow = new(1f, 0.96f, 0f, 1f);
    private Color teal = new(0.47f, 0.96f, 1f, 1f);

    // Variables for lane split countdown.
    public TextMeshProUGUI lanesplitcountdown;
    private float laneSplitCooldownTimer = 5.0f;
    private bool inLaneSplitCooldown = false;
    private bool inLaneSplitCooldownBlink = false;
    private bool inLaneSplitCooldownFadeOut = false;
    private readonly float fadeOutDuration = 0.5f;
    private readonly float blinkDuration = 0.075f;  // Time spent invisible during a blink.
    private readonly float blinkVisibleDuration = 1f;  // Time spent visible during a blink.
    private float blinkTimeElapsed = 0f;
    private float fadeOutTimeElapsed = 0f;
    private Color fadeOutStartColorText;
    private Color fadeOutStartColorImage;
    private Color fadeOutEndColorText = new Color(0f, 0f, 0f, 0f);
    private Color fadeOutEndColorImage = new Color(0f, 0f, 0f, 0f);
    public GameObject lanesplit;
    public Graphic lanesplitimage;

    // Variables for life counter.
    public GameObject lives;
    public TextMeshProUGUI livesCounter;
    private bool isFlashingLives = false;
    private readonly float livesFlashDuration = 0.5f;  // Time spent invisible or visible during a flash.
    private float livesFlashTimeElapsed = 0f;

    // System variables.
    private bool inRaceCountdown = true;
    public bool countdownStarted = false;
    private bool gameEndSet = false;
    private bool skip = false;
    private bool skipHandled = false;
    public float startTime;
    public float totalDistance = 0.0f;
    public float oncomingDistance = 0.0f;
    double topSpeed = 0;
    private CreditManager creditManager;
    private PlayerController playerController;
    private SoundManager soundManager;

    void Awake()
    {
        // Find 'PlayerController' script.
        GameObject PlayerCarObject = GameObject.Find("PlayerCar");
        playerController = PlayerCarObject.GetComponent<PlayerController>();

        // Find 'SoundManager' script.
        GameObject SoundManagerObject = GameObject.Find("SoundManager");
        soundManager = SoundManagerObject.GetComponent<SoundManager>();

        // Find 'CreditManager' script.
        creditManager = credits.GetComponent<CreditManager>();
    }

    void Start()
    {
        // Start the clock.
        startTime = Time.time;

        // Get the default colour of our images.
        imageColor = new(lanesplitimage.color.r, lanesplitimage.color.g, lanesplitimage.color.b, lanesplitimage.color.a);
    }


    void FixedUpdate()
    {
        // Set the countdownStarted flag which is used by the 'SoundManager' script to know when to start playing sounds.
        if (Time.time - startTime > 0) countdownStarted = true;

        // Set the number of lives.
        livesCounter.text = playerController.numlives.ToString();

        // Start the countdown at the beginning of the race.
        StartRaceCountdown();

        // Display the lane split cooldown timer if a lane split has just been completed.
        DisplayLaneSplitTimer();

        // Flash the lives icon on/off when the player's lives are zero.
        FlashLives();

        // If game has ended, disable speedometer & display game over screen.
        if (playerController.gameEnd && !gameEndSet) StartCoroutine(EndGameSequenceCoroutine());

        // Display the player's speed & distance.
        DisplaySpeedAndDistance();
    }

    // Start the countdown at the beginning of the race.
    public void StartRaceCountdown()
    {
        if (inRaceCountdown)
        {
            if (Time.time - startTime < 1)
            {
                countdown.text = "3";
            }
            else if (Time.time - startTime < 2)
            {
                countdown.text = "2";
            }
            else if (Time.time - startTime < 3)
                countdown.text = "1";
            else
            {
                countdown.text = "GO!";
                inRaceCountdown = false;
                StartCoroutine(FadeOutCoroutine(countdown, 2));
            }
        }
    }

    // Start the 5 second lane split timer after the player does a lanesplit. Fade out when timer expires.
    public void DisplayLaneSplitTimer()
    {
        if (inLaneSplitCooldown)
        {
            laneSplitCooldownTimer -= Time.deltaTime;
            if (laneSplitCooldownTimer <= 0) // Timer has reached 0, set the 'blinking' flag to true to begin blinking process.
            {
                lanesplitcountdown.text = "0";
                inLaneSplitCooldown = false;
                inLaneSplitCooldownBlink = true;
                blinkTimeElapsed = 0f;
                lanesplitcountdown.color = fadeOutEndColorText; // Make the countdown invisible.
                lanesplitimage.color = fadeOutEndColorImage; // Make the image invisible.
                soundManager.PlayBeep(); // Play the beep sound.
            }
            else // Timer is still running, update the UI text.
            {
                lanesplitcountdown.text = Mathf.CeilToInt(laneSplitCooldownTimer).ToString();
            }
        }

        // 'Blink' the lane split UI elements once the lane split cooldown timer reaches 0.
        if (inLaneSplitCooldownBlink)
        {
            blinkTimeElapsed += Time.deltaTime;
            if (blinkTimeElapsed > blinkDuration) // Once the blink has finished, begin visible portion of blink.
            {
                lanesplitcountdown.color = teal;
                lanesplitimage.color = imageColor;
                if (blinkTimeElapsed > (blinkDuration + blinkVisibleDuration)) // Visible & invisible portions of blink have finished, begin the fade out process.
                {
                    inLaneSplitCooldownBlink = false;
                    inLaneSplitCooldownFadeOut = true;
                    fadeOutTimeElapsed = 0f;  // Reset the fade timer.
                    fadeOutStartColorText = lanesplitcountdown.color;  // Store the starting colors.
                    fadeOutStartColorImage = lanesplitimage.color;
                }
            }
        }

        // Begin fading out the lane split UI elements once the blinking process is done.
        if (inLaneSplitCooldownFadeOut)
        {
            fadeOutTimeElapsed += Time.deltaTime;
            float progress = fadeOutTimeElapsed / fadeOutDuration;

            lanesplitcountdown.color = Color.Lerp(fadeOutStartColorText, fadeOutEndColorText, progress);
            lanesplitimage.color = Color.Lerp(fadeOutStartColorImage, fadeOutEndColorImage, progress);

            if (progress >= 1f) // Fade out has finished, set fadeOut flag to false & reset all parameters.
            {
                inLaneSplitCooldownFadeOut = false;
                laneSplitCooldownTimer = 5.0f;
                lanesplitcountdown.color = teal;
                lanesplitimage.color = imageColor;
                lanesplit.SetActive(false);
            }
        }
    }

    // Flash the lives icon on/off when the player's lives are zero.
    public void FlashLives()
    {
        // Flashing livesCounter when it's 0.
        if (livesCounter.text == "0" && !isFlashingLives)
        {
            isFlashingLives = true;
            livesFlashTimeElapsed = 0f;
            livesCounter.color = new Color(0, 0, 0, 0);  // Make it invisible first.
        }

        if (isFlashingLives)
        {
            livesFlashTimeElapsed += Time.deltaTime;

            if (livesFlashTimeElapsed >= livesFlashDuration)
            {
                // Toggle visibility.
                if (livesCounter.color.a == 0)  // If it's invisible.
                    livesCounter.color = teal;  // Make it visible.
                else
                    livesCounter.color = new Color(0, 0, 0, 0);  // Make it invisible again.

                // Reset timer.
                livesFlashTimeElapsed = 0f;
            }
        }

        // Reset the flashing mechanism if livesCounter is not 0.
        if (livesCounter.text != "0" && isFlashingLives)
        {
            isFlashingLives = false;
            livesCounter.color = teal;  // Ensure it's visible
        }
    }

    // Display all game end UI.
    private IEnumerator EndGameSequenceCoroutine()
    {
        gameEndSet = true;
        SaveData saveData = SaveManager.Instance.SaveData;
        bool isImperial = saveData.ImperialUnits;
        float metricMultiplier = isImperial ? 1f : 1.60934f;

        // Award nitro if player drives another 100 miles.
        int nitrosEarned = 0;
        if (totalDistance >= saveData.DistanceUntilNextNitro)
        {
            float excessDistance = totalDistance - saveData.DistanceUntilNextNitro;

            // Award one Nitro for every 100 units of excess distance
            nitrosEarned = 1 + Mathf.FloorToInt(excessDistance / 100f);
            saveData.NitroCount += nitrosEarned;

            // Set DistanceUntilNextNitro to the remainder to reach next 100-mile threshold
            float remainder = excessDistance % 100f;
            saveData.DistanceUntilNextNitro = 100f - remainder;
        }
        else
        {
            // Decrease the countdown by the distance driven this race
            saveData.DistanceUntilNextNitro -= totalDistance;
        }

        // Load & save player stats.
        nitrocount.text = saveData.NitroCount.ToString();
        long current_credits = saveData.GlobalCredits;
        float oldRecordDistance = saveData.RecordDistance;
        double oldRecordSpeed = saveData.RecordSpeed;
        if (totalDistance > oldRecordDistance)
        {
            saveData.RecordDistance = totalDistance;
        }
        if (topSpeed > oldRecordSpeed)
        {
            saveData.RecordSpeed = topSpeed;
        }

        // Add a new text if nitro earned this race was over than zero.
        if (nitrosEarned > 0)
        {
            TextMeshProUGUI nitroEarnedText = Instantiate(nitrocount, nitrocount.transform.parent);
            RectTransform nitroEarnedTextRectTransform = nitroEarnedText.GetComponent<RectTransform>();
            nitroEarnedTextRectTransform.sizeDelta = new Vector2(400f, nitroEarnedTextRectTransform.sizeDelta.y);
            nitroEarnedTextRectTransform.anchoredPosition += new Vector2(150f, 0f);
            nitroEarnedText.lineSpacing = -35f;
            nitroEarnedText.fontSize = 45f;
            nitroEarnedText.text = "<b><i>+" + nitrosEarned + " nitro earned !</i></b>";
        }

        // Display game end widgets and disable in-race widgets.
        game_end_widgets.SetActive(true);
        distance.color = new Color(0, 0, 0, 0);
        speedo.color = new Color(0, 0, 0, 0);
        lives.SetActive(false);
        lanesplit.SetActive(false);

        // Display Game Over & wait for 2 seconds unless skipped.
        gameover.gameObject.SetActive(true);
        yield return StartCoroutine(WaitOrSkip(2f));

        // Update distance & top speed statistics.
        TextMeshProUGUI totalDistanceText = distance_speed_stats.transform.Find("Distance/DistanceValue").GetComponent<TextMeshProUGUI>();
        totalDistanceText.text = distance.text;
        TextMeshProUGUI oncomingDistanceText = distance_speed_stats.transform.Find("OncomingDistance/OncomingDistanceValue").GetComponent<TextMeshProUGUI>();
        oncomingDistanceText.text = isImperial ? System.Math.Round(oncomingDistance, 1).ToString("F1") + " mi" : System.Math.Round(oncomingDistance * metricMultiplier, 1).ToString("F1") + " km";
        TextMeshProUGUI topSpeedText = distance_speed_stats.transform.Find("TopSpeed/TopSpeedValue").GetComponent<TextMeshProUGUI>();
        topSpeedText.text = isImperial ? topSpeed + " mph" : System.Math.Round(topSpeed * metricMultiplier)  + " kph";

        // Animate Game Over off screen and bring up distance & top speed statistics.
        float animationDuration = 1f;
        Vector3 gameoverTargetPosition = gameover.transform.position + new Vector3(0, Screen.height, 0);
        Vector3 distanceStatsStartPosition = distance_speed_stats.transform.position - new Vector3(0, Screen.height, 0);
        distance_speed_stats.transform.position = distanceStatsStartPosition;
        Vector3 distanceStatsTargetPosition = new Vector3(distance_speed_stats.transform.position.x, distance_speed_stats.transform.position.y + Screen.height, distance_speed_stats.transform.position.z);
        distance_speed_stats.SetActive(true);
        if (skip)
        {
            // Instantly set positions to the final state.
            gameover.gameObject.SetActive(false);
            distance_speed_stats.transform.position = distanceStatsTargetPosition;
        }
        else
        {
            LeanTween.move(gameover.gameObject, gameoverTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
            LeanTween.move(distance_speed_stats, distanceStatsTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
        }

        // Wait for animation to complete unless skipped.
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration && !skip)
        {
            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
            {
                skip = true;
                
                // Cancel any active LeanTween animations to stop them immediately
                LeanTween.cancel(gameover.gameObject);
                LeanTween.cancel(distance_speed_stats);

                gameover.gameObject.SetActive(false);
                distance_speed_stats.transform.position = distanceStatsTargetPosition;
                
                break;
            }
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        gameover.gameObject.SetActive(false);
        if (skip) // This is the first tap to skip position.
        {
            skip = false;
            yield return new WaitForSeconds(0.1f);
        }
         

        // Calculate earned credits.
        int totalDistanceCredits = (int)(totalDistance * 1000 / SaveManager.Instance.SaveData.TrafficDensity);
        int oncomingDistanceCredits = (int)(oncomingDistance * 1000 / SaveManager.Instance.SaveData.TrafficDensity);

        // Create earned credit UI element for credits earned due to total distance. This TMP object will then move over to the player's total credits in the top right of screen.
        TextMeshProUGUI totalDistanceCreditsText = Instantiate(totalDistanceText, totalDistanceText.transform.parent);
        totalDistanceCreditsText.GetComponent<RectTransform>().anchoredPosition += new Vector2(200f, 0f);
        totalDistanceCreditsText.text = "+0 CR";
        totalDistanceCreditsText.gameObject.SetActive(true);

        // Create a copy of totalDistanceCreditsText which will stay stationary.
        TextMeshProUGUI staticTotalDistanceCreditsText = Instantiate(totalDistanceCreditsText, totalDistanceText.transform.parent);
        staticTotalDistanceCreditsText.gameObject.SetActive(false);

        // Copy position, size, color, and other attributes.
        RectTransform originalRect = totalDistanceCreditsText.GetComponent<RectTransform>();
        RectTransform duplicateRect = staticTotalDistanceCreditsText.GetComponent<RectTransform>();
        duplicateRect.anchoredPosition = originalRect.anchoredPosition;
        duplicateRect.sizeDelta = originalRect.sizeDelta;
        staticTotalDistanceCreditsText.color = totalDistanceCreditsText.color;
        staticTotalDistanceCreditsText.text = totalDistanceCreditsText.text;
        staticTotalDistanceCreditsText.fontSize = totalDistanceCreditsText.fontSize;
        staticTotalDistanceCreditsText.alignment = totalDistanceCreditsText.alignment;

        // Animate credit increase.
        yield return StartCoroutine(AnimateNumberIncrease(totalDistanceCreditsText, totalDistanceCredits, 1f, "credits")); // 'skip' could be set to true in here.

        // Activate the copy.
        staticTotalDistanceCreditsText.text = $"+{Mathf.RoundToInt(totalDistanceCredits)} CR";
        staticTotalDistanceCreditsText.gameObject.SetActive(true);

        // Move earned credits to top right and update player's total credits.
        if (skip)
        {
            Destroy(totalDistanceCreditsText.gameObject);
            saveData.GlobalCredits = current_credits + totalDistanceCredits;
            credits.text = $"{saveData.GlobalCredits:n0} cr";
        }
        else
        {
            LeanTween.move(totalDistanceCreditsText.gameObject, credits.transform.position, 1f).setEase(LeanTweenType.easeInOutQuad).setOnComplete(() =>
            {
                totalDistanceCreditsText.gameObject.SetActive(false);
                creditManager.ChangeCredits(totalDistanceCredits);
            });

            // Wait until the total distance credits moves to the credits text's positon and the credits text updates with the new credits, or skip.
            while (!creditManager.changeDone && !skip)
            {
                if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                {
                    skip = true;

                    LeanTween.cancel(totalDistanceCreditsText.gameObject);
                    Destroy(totalDistanceCreditsText.gameObject);

                    creditManager.ChangeCredits(0, true); // Stop the credits coroutine.
                    creditManager.changeDone = true; // Force completion.
                    saveData.GlobalCredits = current_credits + totalDistanceCredits;
                    credits.text = $"{saveData.GlobalCredits:n0} cr";
                    break;
                }
                yield return null;
            }

            // Wait 0.5 seconds, or skip.
            if (!skip)
            {
                creditManager.changeDone = false;
                elapsedTime = 0f;
                while (elapsedTime < 0.5f)
                {
                    if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                    {
                        skip = true;
                        break;
                    }
                    yield return null;
                    elapsedTime += Time.deltaTime;
                }
            }
        }

        // Handle credits earned for oncoming distance. Only update if it's over zero.
        if (oncomingDistanceCredits > 0)
        {
            // Create a TMP object that will show the amount of credits the player earned due to oncoming distance. This TMP object will then move over to the player's total credits in the top right of screen.
            TextMeshProUGUI oncomingDistanceCreditsText = Instantiate(oncomingDistanceText, oncomingDistanceText.transform.parent);
            oncomingDistanceCreditsText.GetComponent<RectTransform>().anchoredPosition += new Vector2(200f, 0f);
            oncomingDistanceCreditsText.text = "+0 CR";

            // Create a copy of oncomingDistanceCreditsText which will stay stationary.
            TextMeshProUGUI staticOncomingDistanceCreditsText = Instantiate(oncomingDistanceCreditsText, oncomingDistanceText.transform.parent);
            staticOncomingDistanceCreditsText.gameObject.SetActive(false);

            // Copy position, size, color, and other attributes.
            originalRect = oncomingDistanceCreditsText.GetComponent<RectTransform>();
            duplicateRect = staticOncomingDistanceCreditsText.GetComponent<RectTransform>();
            duplicateRect.anchoredPosition = originalRect.anchoredPosition;
            duplicateRect.sizeDelta = originalRect.sizeDelta;
            staticOncomingDistanceCreditsText.color = oncomingDistanceCreditsText.color;
            staticOncomingDistanceCreditsText.text = oncomingDistanceCreditsText.text;
            staticOncomingDistanceCreditsText.fontSize = oncomingDistanceCreditsText.fontSize;
            staticOncomingDistanceCreditsText.alignment = oncomingDistanceCreditsText.alignment;

            current_credits = saveData.GlobalCredits;
            if (skip)
            {
                oncomingDistanceCreditsText.gameObject.SetActive(false);
                saveData.GlobalCredits += oncomingDistanceCredits;
                credits.text = $"{saveData.GlobalCredits:n0} cr";

                // Activate the copy.
                staticOncomingDistanceCreditsText.text = $"+{Mathf.RoundToInt(oncomingDistanceCredits)} CR";
                staticOncomingDistanceCreditsText.gameObject.SetActive(true);
            }
            else
            {
                oncomingDistanceCreditsText.gameObject.SetActive(true);

                yield return StartCoroutine(AnimateNumberIncrease(oncomingDistanceCreditsText, oncomingDistanceCredits, 1f, "credits")); // 'skip' could be set to true in here.

                // Activate the copy.
                staticOncomingDistanceCreditsText.text = $"+{Mathf.RoundToInt(oncomingDistanceCredits)} CR";
                staticOncomingDistanceCreditsText.gameObject.SetActive(true);

                if (skip)
                {
                    Destroy(oncomingDistanceCreditsText.gameObject);
                    saveData.GlobalCredits = current_credits + oncomingDistanceCredits;
                    credits.text = $"{saveData.GlobalCredits:n0} cr";
                }
                else
                {
                    LeanTween.move(oncomingDistanceCreditsText.gameObject, credits.transform.position + new Vector3(200, 0, 0), 1f).setEase(LeanTweenType.easeInOutQuad).setOnComplete(() =>
                    {
                        oncomingDistanceCreditsText.gameObject.SetActive(false);
                        creditManager.ChangeCredits(oncomingDistanceCredits);
                    });

                    // Wait until the total distance credits moves to the credits text's positon and the credits text updates with the new credits, or skip.
                    while (!creditManager.changeDone && !skip)
                    {
                        if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                        {
                            skip = true;

                            LeanTween.cancel(oncomingDistanceCreditsText.gameObject);
                            Destroy(oncomingDistanceCreditsText.gameObject);

                            creditManager.ChangeCredits(0, true); // Stop the credits coroutine.
                            creditManager.changeDone = true; // Force completion.
                            saveData.GlobalCredits = current_credits + totalDistanceCredits;
                            credits.text = $"{saveData.GlobalCredits:n0} cr";
                            break;
                        }
                        yield return null;
                    }

                    // Wait 0.5 seconds, or skip.
                    if (!skip)
                    {
                        creditManager.changeDone = false;
                        elapsedTime = 0f;
                        while (elapsedTime < 0.5f)
                        {
                            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                            {
                                skip = true;
                                break;
                            }
                            yield return null;
                            elapsedTime += Time.deltaTime;
                        }
                    }
                }

                creditManager.changeDone = false;
            }
        }
        
        SaveManager.Instance.SaveGame();

        // Wait for 0.5 seconds.
        yield return StartCoroutine(WaitOrSkip(0.5f));

        // Animate distance speed stats to the top.
        float moveUpAmount = Screen.height / 4f;
        animationDuration = 0.75f;
        distanceStatsTargetPosition = distance_speed_stats.transform.position + new Vector3(0, moveUpAmount, 0);
        if (skip)
        {
            // Instantly set positions to the final state.
            distance_speed_stats.transform.position = distanceStatsTargetPosition;
        }
        else
        {
            LeanTween.move(distance_speed_stats, distanceStatsTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
        }

        // Wait for distance speed stats to reach the top (unless skipped) before moving record stats.
        elapsedTime = 0f;
        while (elapsedTime < animationDuration * 0.5f && !skip)
        {
            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
            {
                skip = true;

                // Cancel any active LeanTween animations to stop them immediately
                LeanTween.cancel(distance_speed_stats);

                distance_speed_stats.transform.position = distanceStatsTargetPosition;

                break;
            }
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        // Set record distance & speed statistics.
        TextMeshProUGUI recordDistanceText = record_stats.transform.Find("RecordDistance/RecordDistanceValue").GetComponent<TextMeshProUGUI>();
        recordDistanceText.text = isImperial ? oldRecordDistance.ToString("F1") + " mi" : (oldRecordDistance * metricMultiplier).ToString("F1") + " km";

        TextMeshProUGUI recordSpeedText = record_stats.transform.Find("RecordSpeed/RecordSpeedValue").GetComponent<TextMeshProUGUI>();
        recordSpeedText.text = isImperial ? oldRecordSpeed.ToString() + " mph" : (oldRecordSpeed * metricMultiplier).ToString("F0") + " kph";

        // Animate record stats + buttons to bottom of screen.
        moveUpAmount = Screen.height / 2;
        Vector3 recordStatsStartPosition = record_stats.transform.position - new Vector3(0, moveUpAmount, 0);
        Vector3 replayButtonStartPosition = replayButton.transform.position - new Vector3(0, moveUpAmount, 0);
        Vector3 quitButtonStartPosition = quitButton.transform.position - new Vector3(0, moveUpAmount, 0);

        record_stats.transform.position = recordStatsStartPosition;
        replayButton.transform.position = replayButtonStartPosition;
        quitButton.transform.position = quitButtonStartPosition;

        Vector3 recordStatsTargetPosition = record_stats.transform.position + new Vector3(0, moveUpAmount, 0);
        Vector3 replayButtonTargetPosition = replayButton.transform.position + new Vector3(0, moveUpAmount, 0);
        Vector3 quitButtonTargetPosition = quitButton.transform.position + new Vector3(0, moveUpAmount, 0);

        record_stats.SetActive(true);
        replayButton.SetActive(true);
        quitButton.SetActive(true);

        if (skip)
        {
            // Instantly set positions to the final state.
            record_stats.transform.position = recordStatsTargetPosition;
            replayButton.transform.position = replayButtonTargetPosition;
            quitButton.transform.position = quitButtonTargetPosition;
        }
        else
        {
            LeanTween.move(record_stats, recordStatsTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
            LeanTween.move(replayButton, replayButtonTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
            LeanTween.move(quitButton, quitButtonTargetPosition, animationDuration).setEase(LeanTweenType.easeInOutQuad);
        }

        // Wait for animation to complete (unless skipped).
        elapsedTime = 0f;
        while (elapsedTime < animationDuration && !skip)
        {
            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
            {
                skip = true;

                // Cancel any active LeanTween animations to stop them immediately
                LeanTween.cancel(record_stats);
                LeanTween.cancel(replayButton);
                LeanTween.cancel(record_stats);

                record_stats.transform.position = recordStatsTargetPosition;
                replayButton.transform.position = replayButtonTargetPosition;
                quitButton.transform.position = quitButtonTargetPosition;

                break;
            }
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        // Update record distance & speed statistics.
        if (totalDistance > oldRecordDistance)
        {
            // Calculate difference between new record and old record.
            float distanceRecordDifference = (totalDistance - oldRecordDistance) * metricMultiplier;

            // Create distance difference UI element.
            TextMeshProUGUI distanceRecordDifferenceText = Instantiate(recordDistanceText, recordDistanceText.transform.parent);
            distanceRecordDifferenceText.GetComponent<RectTransform>().anchoredPosition += new Vector2(200f, 0f);
            distanceRecordDifferenceText.text = isImperial ? "+0 MI" : "+0 KM";
            distanceRecordDifferenceText.gameObject.SetActive(true);

            // Animate distance increase.
            yield return StartCoroutine(AnimateNumberIncrease(distanceRecordDifferenceText, distanceRecordDifference, 0.5f, "distance")); // 'skip' could be set to true in here.

            // Move new record text to the right and add a text object.
            animationDuration = 0.5f;
            if (skip)
            {
                // Instantly set positions to the final state.
                distanceRecordDifferenceText.transform.position = recordDistanceText.transform.position;
            }
            else
            {
                LeanTween.move(distanceRecordDifferenceText.gameObject, recordDistanceText.transform.position, animationDuration).setEase(LeanTweenType.easeInOutQuad);
            }

            // Wait for animation to complete (unless skipped).
            elapsedTime = 0f;
            while (elapsedTime < animationDuration && !skip)
            {
                if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                {
                    skip = true;

                    // Cancel any active LeanTween animations to stop them immediately.
                    LeanTween.cancel(distanceRecordDifferenceText.gameObject);

                    break;
                }
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            // Set ending record values.
            distanceRecordDifferenceText.gameObject.SetActive(false);
            TextMeshProUGUI newRecordText = Instantiate(recordDistanceText, recordDistanceText.transform.parent);
            newRecordText.GetComponent<RectTransform>().anchoredPosition += new Vector2(300f, 0f);
            newRecordText.text = "NEW RECORD!";
            soundManager.newrecordsource.Play();
            newRecordText.gameObject.SetActive(true);
            recordDistanceText.text = isImperial ? totalDistance.ToString("F1") + " mi" : (totalDistance * metricMultiplier).ToString("F1") + " km";

            // Wait for 0.5 seconds.
            yield return StartCoroutine(WaitOrSkip(0.5f));
        }

        // Wait for 0.5 seconds.
        yield return StartCoroutine(WaitOrSkip(0.5f));

        if (topSpeed > oldRecordSpeed)
        {
            // Calculate difference between new record and old record.
            double speedRecordDifference = (topSpeed - oldRecordSpeed) * metricMultiplier;

            // Create distance difference UI element.
            TextMeshProUGUI speedRecordDifferenceText = Instantiate(recordSpeedText, recordSpeedText.transform.parent);
            speedRecordDifferenceText.GetComponent<RectTransform>().anchoredPosition += new Vector2(250f, 0f);
            speedRecordDifferenceText.text = isImperial ? "+0 MPH" : "+0 KPH";
            speedRecordDifferenceText.gameObject.SetActive(true);

            yield return StartCoroutine(AnimateNumberIncrease(speedRecordDifferenceText, (float)speedRecordDifference, 0.5f, "speed")); // 'skip' could be set to true in here.

            // Move new record text to the right and add a text object. ADD SKIP LOGIC HERE.
            animationDuration = 0.5f;
            if (skip)
            {
                // Instantly set positions to the final state.
                speedRecordDifferenceText.transform.position = recordSpeedText.transform.position;
            }
            else
            {
                LeanTween.move(speedRecordDifferenceText.gameObject, recordSpeedText.transform.position, animationDuration).setEase(LeanTweenType.easeInOutQuad);
            }

            // Wait for animation to complete (unless skipped).
            elapsedTime = 0f;
            while (elapsedTime < animationDuration && !skip)
            {
                if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
                {
                    skip = true;

                    // Cancel any active LeanTween animations to stop them immediately.
                    LeanTween.cancel(speedRecordDifferenceText.gameObject);

                    break;
                }
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            // Set ending record values.
            speedRecordDifferenceText.gameObject.SetActive(false);
            TextMeshProUGUI newRecordText = Instantiate(recordSpeedText, recordSpeedText.transform.parent);
            newRecordText.GetComponent<RectTransform>().anchoredPosition += new Vector2(300f, 0f);
            newRecordText.text = "NEW RECORD!";
            soundManager.newrecordsource.Play();
            newRecordText.gameObject.SetActive(true);
            recordSpeedText.text = isImperial ? topSpeed.ToString() + " mph" : (topSpeed * metricMultiplier).ToString("F0") + " kph";

            // Wait for 0.5 seconds.
            yield return StartCoroutine(WaitOrSkip(0.5f));
        }
    }

    private IEnumerator AnimateNumberIncrease(TextMeshProUGUI tmp_object, float finalAmount, float duration, string type, Action onComplete = null)
    {
        float currentAmount = 0f; // Use float for precise rounding.
        float previousAmount = 0f; // Store the previous amount.
        float elapsedTime = 0f;
        bool isImperial = SaveManager.Instance.SaveData.ImperialUnits;

        while (elapsedTime < duration && !skip)
        {
            elapsedTime += Time.deltaTime;
            currentAmount = Mathf.Lerp(0, finalAmount, elapsedTime / duration);

            if (Mathf.RoundToInt(currentAmount) > Mathf.RoundToInt(previousAmount)) soundManager.numberincreasesource.Play();

            if (type == "credits")
                tmp_object.text = $"+{Mathf.RoundToInt(currentAmount)} CR";
            else if (type == "distance")
                tmp_object.text = isImperial ? $"+{currentAmount:F1} MI" : $"+{currentAmount:F1} KM";
            else if (type == "speed")
                tmp_object.text = isImperial ? $"+{Mathf.RoundToInt(currentAmount)} MPH" : $"+{Mathf.RoundToInt(currentAmount)} KPH";
            else
                tmp_object.text = $"+{currentAmount}"; // Default formatting.

            // Check for skip input
            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
            {
                skip = true;
                break;
            }

            previousAmount = currentAmount;

            yield return null;
        }

        // Set the final value.
        if (skip || elapsedTime >= duration)
        {
            if (type == "credits")
                tmp_object.text = $"+{Mathf.RoundToInt(finalAmount)} CR";
            else if (type == "distance")
                tmp_object.text = isImperial ? $"+{finalAmount:F1} MI" : $"+{finalAmount:F1} KM";
            else if (type == "speed")
                tmp_object.text = isImperial ? $"+{Mathf.RoundToInt(finalAmount)} MPH" : $"+{Mathf.RoundToInt(finalAmount)} KPH";
            else
                tmp_object.text = $"+{finalAmount}";
        }

        onComplete?.Invoke();
    }

    private IEnumerator WaitOrSkip(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration && !skip)
        {
            if (Input.GetMouseButtonUp(0) || Input.touchCount > 0)
            {
                skip = true;
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Display the player's speed & distance.
    public void DisplaySpeedAndDistance()
    {
        bool isImperial = SaveManager.Instance.SaveData.ImperialUnits;

        float metricMultiplier = isImperial ? 1f : 1.60934f;
        float currentSpeedMPH = 85.36585365f * playerController.accel + 12.3170731707f;
        double currentSpeed = System.Math.Round(currentSpeedMPH);
            
        if (!playerController.bullet)
        {
            speedo.text = isImperial ? currentSpeed + " mph" : System.Math.Round(currentSpeed * metricMultiplier) + " kph";
            if (currentSpeed > topSpeed) topSpeed = currentSpeed;
        }
        else
        {
            DisplayBulletSpeedo();
        }

        // Convert speed from mph to miles per second.
        float speedMilesPerSecond = currentSpeedMPH / 3600f;

        // Calculate distance traveled during this frame.
        float distanceThisFrame = speedMilesPerSecond * Time.fixedDeltaTime;

        // Add this frame's distance to the total distance.
        if (!playerController.gameEnd) totalDistance += distanceThisFrame;
        if (!playerController.gameEnd && playerController.currentLane < 4) oncomingDistance += distanceThisFrame;

        // Update the distance display.
        distance.text = isImperial ? System.Math.Round(totalDistance, 1).ToString("F1") + " mi" : System.Math.Round(totalDistance * metricMultiplier, 1).ToString("F1") + " km";
        
    }

    private void DisplayBulletSpeedo()
    {
        bulletFlashTimeElapsed += Time.deltaTime;

        if (bulletFlashTimeElapsed >= bulletSpeedoFlashDuration)
        {
            // Toggle visibility.
            if (speedo.color.a == 0)
            {
                if (bulletFlashCounter % 2 == 0) speedo.text = "TOO FAST";
                else speedo.text = "ERROR";
                speedo.color = teal;  // Make it visible.
                bulletFlashCounter += 1;
            }
            else
            {
                speedo.color = new Color(0, 0, 0, 0);  // Make it invisible again.
            }

            // Reset timer.
            bulletFlashTimeElapsed = 0f;
        }
    }

    // Activate the lane split UI countdown timer.
    public void BeginLaneSplitCooldown()
    {
        lanesplit.SetActive(true);

        // If a lane split countdown is in process, cancel it and reset all parameters.
        if (inLaneSplitCooldown || inLaneSplitCooldownBlink || inLaneSplitCooldownFadeOut)
        {
            inLaneSplitCooldown = false;
            inLaneSplitCooldownBlink = false;
            inLaneSplitCooldownFadeOut = false;
            lanesplitcountdown.color = teal;
            lanesplitimage.color = imageColor;
        }

        // Begin a new countdown.
        inLaneSplitCooldown = true;
        laneSplitCooldownTimer = 5.0f;  // Reset the timer.
    }

    // Fade out a text object.
    private IEnumerator FadeOutCoroutine(TextMeshProUGUI textToFadeOut, float time)
    {
        Color startColor = textToFadeOut.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            textToFadeOut.color = Color.Lerp(startColor, endColor, t / time);
            yield return null;
        }
    }

    // Exit out of level into the main menu.
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
