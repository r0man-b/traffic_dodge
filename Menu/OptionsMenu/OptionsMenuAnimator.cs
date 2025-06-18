using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OptionsMenuAnimator : MonoBehaviour
{
    public RectTransform buttonPanel; // Assign your button panel
    public RectTransform optionsPanel; // Assign your options panel
    public List<GameObject> optionGroups; // Assign option sets per button (index-matched)
    public float moveDuration = 0.5f; // Smooth transition duration
    public Vector2 hiddenPos; // Position when buttons are moved left
    public Vector2 centerPos; // Position when buttons are centered
    public Vector2 optionsHiddenPos; // Position where options panel is off-screen
    public Vector2 optionsVisiblePos; // Position where options appear

    private int currentOpenIndex = -1; // Tracks which button's options are open

    public Settings.GraphicsSettingsMenu graphicsMenu;

    void Start()
    {
        // Start with all options hidden and options panel out of view
        foreach (var group in optionGroups)
        {
            group.SetActive(false);
        }
        optionsPanel.anchoredPosition = optionsHiddenPos;
    }

    public bool IsGraphicsMenuOpen()
    {
        return currentOpenIndex == 2; // Assuming index 2 is the Graphics menu
    }

    public void OnButtonClick(int buttonIndex)
    {
        StartCoroutine(OnButtonClickRoutine(buttonIndex));
    }

    private IEnumerator OnButtonClickRoutine(int buttonIndex)
    {
        bool waitForPopup = currentOpenIndex == 2;

        bool userFinishedPopup = false;

        if (waitForPopup)
        {
            graphicsMenu.DisplayPopup((_) =>
            {
                userFinishedPopup = true;
            });

            // Wait until user has responded to the popup
            yield return new WaitUntil(() => userFinishedPopup);
        }

        if (currentOpenIndex == buttonIndex)
        {
            StartCoroutine(MovePanels(centerPos, optionsHiddenPos));
            optionGroups[currentOpenIndex].SetActive(false);
            currentOpenIndex = -1;
        }
        else
        {
            StartCoroutine(MovePanels(hiddenPos, optionsVisiblePos));

            if (currentOpenIndex != -1)
                optionGroups[currentOpenIndex].SetActive(false);

            optionGroups[buttonIndex].SetActive(true);
            currentOpenIndex = buttonIndex;
        }
    }

    IEnumerator MovePanels(Vector2 buttonTargetPos, Vector2 optionsTargetPos)
    {
        float elapsedTime = 0;
        Vector2 buttonStartPos = buttonPanel.anchoredPosition;
        Vector2 optionsStartPos = optionsPanel.anchoredPosition;

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            buttonPanel.anchoredPosition = Vector2.Lerp(buttonStartPos, buttonTargetPos, t);
            optionsPanel.anchoredPosition = Vector2.Lerp(optionsStartPos, optionsTargetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        buttonPanel.anchoredPosition = buttonTargetPos;
        optionsPanel.anchoredPosition = optionsTargetPos;
    }
}
