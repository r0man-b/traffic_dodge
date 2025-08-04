using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoRaceMenu : MonoBehaviour
{
    [SerializeField] private Transform environmentPanel;

    private List<Transform> environmentButtons = new List<Transform>();
    private int currentSelectedIndex = -1;
    private int environmentToBuyIndex = -1;

    public CreditManager creditManager;
    public GameObject popUps;
    public GameObject buyConfirmationPopUp;
    public TextMeshProUGUI buyConfirmationPopUpText;
    public GameObject notEnoughCreditsPopUp;
    public TextMeshProUGUI notEnoughCreditsPopUpText;
    public AudioSource audioSource;

    private bool enteredFromMainMenu;

    public GameObject buttons;
    public GameObject garageUI;
    public GameObject mainMenuUI;

    void Start()
    {
        for (int i = 0; i < environmentPanel.childCount; i++)
        {
            Transform child = environmentPanel.GetChild(i);
            environmentButtons.Add(child);

            int index = i;

            Button btn = child.GetComponent<Button>();
            Transform border = child.Find("Border");
            Transform lockObj = child.Find("Lock");

            bool isPurchased = IsEnvironmentPurchased(index);

            if (lockObj != null)
                lockObj.gameObject.SetActive(!isPurchased);

            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (IsEnvironmentPurchased(index))
                        OnEnvironmentSelected(index);
                    else
                        ConfirmBuy(index);
                });

                btn.interactable = true; // Allow interaction for all buttons
            }

            if (border != null)
                border.gameObject.SetActive(false);
        }

        int savedIndex = SaveManager.Instance.SaveData.CurrentEnvironment;
        if (savedIndex >= 0 && savedIndex < environmentButtons.Count && IsEnvironmentPurchased(savedIndex))
        {
            OnEnvironmentSelected(savedIndex);
        }
    }

    void OnEnvironmentSelected(int index)
    {
        if (index == currentSelectedIndex)
            return;

        if (!IsEnvironmentPurchased(index))
            return;

        if (currentSelectedIndex >= 0 && currentSelectedIndex < environmentButtons.Count)
        {
            Transform previous = environmentButtons[currentSelectedIndex].Find("Border");
            if (previous != null)
                previous.gameObject.SetActive(false);
        }

        Transform selected = environmentButtons[index].Find("Border");
        if (selected != null)
            selected.gameObject.SetActive(true);

        currentSelectedIndex = index;
        SaveManager.Instance.SaveData.CurrentEnvironment = index;
    }

    private bool IsEnvironmentPurchased(int index)
    {
        var save = SaveManager.Instance.SaveData;
        return index switch
        {
            0 => save.City77EnvironmentPurchased,
            1 => save.ApocalypticWastelandEnvironmentPurchased,
            2 => save.GalacticHighwayEnvironmentPurchased,
            3 => save.TransatlanticTunnelEnvironmentPurchased,
            _ => false,
        };
    }

    private int GetEnvironmentPrice(int index)
    {
        return index switch
        {
            0 => 0,
            1 => 20000,
            2 => 50000,
            3 => 100000,
            _ => 0,
        };
    }

    private string GetEnvironmentName(int index)
    {
        return index switch
        {
            0 => "City 77",
            1 => "Apocalyptic Wasteland",
            2 => "Intergalactic Highway",
            3 => "Trans-Atlantic Tunnel",
            _ => "Unknown",
        };
    }

    // Called when selecting a locked environment
    public void ConfirmBuy(int index)
    {
        int price = GetEnvironmentPrice(index);
        string envName = GetEnvironmentName(index);

        environmentToBuyIndex = index;

        popUps.SetActive(true);

        if (creditManager.GetCredits() < price)
        {
            notEnoughCreditsPopUpText.text = $"You don't have enough credits to unlock <u>{envName}</u>.\nRequired: {price:N0} credits.";
            notEnoughCreditsPopUp.SetActive(true);
            buyConfirmationPopUp.SetActive(false);
        }
        else
        {
            buyConfirmationPopUpText.text = $"Unlock <u>{envName}</u> for {price:N0} credits?";
            buyConfirmationPopUp.SetActive(true);
            notEnoughCreditsPopUp.SetActive(false);
        }
    }

    public void BuyEnvironment()
    {
        if (environmentToBuyIndex < 0 || environmentToBuyIndex >= environmentButtons.Count)
            return;

        int price = GetEnvironmentPrice(environmentToBuyIndex);
        string envName = GetEnvironmentName(environmentToBuyIndex);

        creditManager.ChangeCredits(-price);

        // Update purchase state
        var save = SaveManager.Instance.SaveData;
        switch (environmentToBuyIndex)
        {
            case 0: save.City77EnvironmentPurchased = true; break;
            case 1: save.ApocalypticWastelandEnvironmentPurchased = true; break;
            case 2: save.GalacticHighwayEnvironmentPurchased = true; break;
            case 3: save.TransatlanticTunnelEnvironmentPurchased = true; break;
        }

        // Update UI
        Transform envButton = environmentButtons[environmentToBuyIndex];
        Transform lockObj = envButton.Find("Lock");
        if (lockObj != null)
            lockObj.gameObject.SetActive(false);

        popUps.SetActive(false);
        audioSource.Play();

        SaveManager.Instance.SaveGame();
        OnEnvironmentSelected(environmentToBuyIndex); // Auto-select after purchase
    }

    public void SetEnteredFromMainMenu(bool value)
    {
        enteredFromMainMenu = value;
    }

    public void ExitGoRaceMenu()
    {
        if (enteredFromMainMenu)
        {
            buttons.SetActive(true);
        }
        else
        {
            buttons.SetActive(true);
            garageUI.SetActive(true);
            mainMenuUI.SetActive(false);
        }
    }
}
