using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ToggleGraphicsOptions : MonoBehaviour
{
    public GameObject leftButton;  // Button to go back to Basic
    public GameObject rightButton; // Button to go to Advanced
    public TMP_Text optionsText;   // TMP Text to show current mode
    public TMP_Text presetNameText; // Displays current basic graphics preset
    public GameObject basicOptions;  // Panel for Basic options
    public GameObject advancedOptions; // Panel for Advanced options

    public Settings.GraphicsSettingsMenu graphicsMenu;

    private bool isAdvanced = false; // Track current state

    public Button BasicGraphicsOptionsModeButtonLeft;
    public Button BasicGraphicsOptionsModeButtonRight;

    public RectTransform revertChangesButtonTransform;
    public RectTransform acceptChangesButtonTransform;

    private int currentPresetIndex;
    private const int maxPresetIndex = 4; // 0 to 4 (Trash to Insane)

    void Start()
    {
        // Always verify preset match instead of trusting saved isCustomGraphics blindly
        bool matched = false;
        for (int i = 0; i <= maxPresetIndex; i++)
        {
            if (graphicsMenu.MatchesPreset((Settings.GraphicsPreset)i))
            {
                currentPresetIndex = i;
                SetBasicOptions(currentPresetIndex);
                matched = true;
                break;
            }
        }

        if (!matched)
        {
            currentPresetIndex = -1;
            SetCustomOptions();
        }

        // Hook button click events
        BasicGraphicsOptionsModeButtonLeft.onClick.AddListener(OnLeftButtonClicked);
        BasicGraphicsOptionsModeButtonRight.onClick.AddListener(OnRightButtonClicked);
    }

    private void OnEnable()
    {
        ToggleToBasic();
    }

    public void ToggleToAdvanced()
    {
        if (!isAdvanced)
        {
            basicOptions.SetActive(false);
            advancedOptions.SetActive(true);

            leftButton.SetActive(true);
            rightButton.SetActive(false);

            optionsText.text = "Advanced Options";
            optionsText.fontSize = 50; // Set font size for Advanced

            isAdvanced = true;

            revertChangesButtonTransform.anchorMin = new Vector2(0.05f, 0.83f);
            revertChangesButtonTransform.anchorMax = new Vector2(0.205f, 0.94f);
            acceptChangesButtonTransform.anchorMin = new Vector2(0.795f, 0.83f);
            acceptChangesButtonTransform.anchorMax = new Vector2(0.95f, 0.94f);
        }
    }

    public void ToggleToBasic()
    {
        if (isAdvanced)
        {
            basicOptions.SetActive(true);
            advancedOptions.SetActive(false);

            leftButton.SetActive(false);
            rightButton.SetActive(true);

            optionsText.text = "Basic Options";
            optionsText.fontSize = 60;

            isAdvanced = false;

            revertChangesButtonTransform.anchorMin = new Vector2(0.095f, 0.22f);
            revertChangesButtonTransform.anchorMax = new Vector2(0.335f, 0.38f);
            acceptChangesButtonTransform.anchorMin = new Vector2(0.67f, 0.22f);
            acceptChangesButtonTransform.anchorMax = new Vector2(0.91f, 0.38f);

            // Check if current settings match any preset
            bool matched = false;
            foreach (Settings.GraphicsPreset preset in System.Enum.GetValues(typeof(Settings.GraphicsPreset)))
            {
                if (graphicsMenu.MatchesPreset(preset))
                {
                    currentPresetIndex = (int)preset;
                    presetNameText.text = preset.ToString();
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                currentPresetIndex = -1;
                presetNameText.text = "Custom";
            }

            UpdatePresetNavigationButtons();
        }
    }


    private void SetBasicOptions(int presetIndex)
    {
        basicOptions.SetActive(true);
        advancedOptions.SetActive(false);

        leftButton.SetActive(false);
        rightButton.SetActive(true);

        optionsText.text = "Basic Options";
        optionsText.fontSize = 60;

        presetNameText.text = ((Settings.GraphicsPreset)presetIndex).ToString();

        isAdvanced = false;
        currentPresetIndex = presetIndex;
        UpdatePresetNavigationButtons();
    }

    private void SetCustomOptions()
    {
        basicOptions.SetActive(true);
        advancedOptions.SetActive(false);

        leftButton.SetActive(false);
        rightButton.SetActive(true);

        optionsText.text = "Basic Options";
        optionsText.fontSize = 60;

        presetNameText.text = "Custom";

        isAdvanced = false;
        currentPresetIndex = -1;
        UpdatePresetNavigationButtons();
    }

    private void OnLeftButtonClicked()
    {
        if (currentPresetIndex == -1)
        {
            currentPresetIndex = maxPresetIndex; // Jump to Insane
        }
        else if (currentPresetIndex <= 0)
        {
            currentPresetIndex = 0;
        }
        else
        {
            currentPresetIndex--;
        }

        ApplyCurrentPreset();
    }


    private void OnRightButtonClicked()
    {
        if (currentPresetIndex == -1)
        {
            currentPresetIndex = 0; // Jump to Trash
        }
        else if (currentPresetIndex >= maxPresetIndex)
        {
            currentPresetIndex = 0;
        }
        else
        {
            currentPresetIndex++;
        }

        ApplyCurrentPreset();
    }


    private void ApplyCurrentPreset()
    {
        presetNameText.text = ((Settings.GraphicsPreset)currentPresetIndex).ToString();

        graphicsMenu.ApplyPreset((Settings.GraphicsPreset)currentPresetIndex, (bool success) =>
        {
            // After applying the preset, update button visibility
            UpdatePresetNavigationButtons();
        });
    }


    private void UpdatePresetNavigationButtons()
    {
        if (currentPresetIndex == -1)
        {
            // Custom - show both to allow entry back into preset list
            BasicGraphicsOptionsModeButtonLeft.gameObject.SetActive(true);
            BasicGraphicsOptionsModeButtonRight.gameObject.SetActive(true);
        }
        else
        {
            BasicGraphicsOptionsModeButtonLeft.gameObject.SetActive(currentPresetIndex > 0);
            BasicGraphicsOptionsModeButtonRight.gameObject.SetActive(currentPresetIndex < maxPresetIndex);
        }
    }
}
