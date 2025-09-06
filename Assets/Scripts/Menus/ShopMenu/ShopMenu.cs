using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// using UnityEngine.Purchasing;

public class ShopMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject topLevelButtons;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject popups;
    [SerializeField] private GameObject nitroImage;              // Shows a nitro-can icon when buying Credits
    [SerializeField] private GameObject notEnoughNitroPopUp;     // Shown when player lacks nitros for a Credits purchase
    [SerializeField] private CreditManager CreditManager;           // Object holding a `CreditManager` component
    [SerializeField] private GameObject garageUI;
    [SerializeField] private GarageUIManager garageUIManager;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private CarDisplay carDisplay;
    public TextMeshProUGUI nitrocount;

    [Header("Popup UI")]
    [SerializeField] private TMP_Text packTitleText;       // e.g., "NITRO PACK OF X" or "X CR"
    [SerializeField] private TMP_Text quantityHeaderText;  // "QUANTITY:" (optional)
    [SerializeField] private TMP_Text quantityValueText;   // number between +/- buttons
    [SerializeField] private TMP_Text totalText;           // e.g., "TOTAL: X NITROS" or "TOTAL: X CR"
    [SerializeField] private TMP_Text buyButtonText;       // main buy label
    [SerializeField] private TMP_Text buyButtonTextOutline;// outline label (mirrors main)
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button buyButton;

    [Header("Crate Open Confirmation UI")]
    [SerializeField] private GameObject openLootCratePopup;
    [SerializeField] private TMP_Text queryText;
    [SerializeField] private TMP_Text leftInfoText;
    [SerializeField] private TMP_Text rightInfoText;
    [SerializeField] private TMP_Text openButtonText;       // main buy label
    [SerializeField] private TMP_Text openButtonTextOutline;// outline label (mirrors main)
    [SerializeField] private RawImage crateImage;

    [Header("Hold-to-Repeat")]
    [Tooltip("Delay (seconds) before repeating starts while holding +/-")]
    [SerializeField] private float holdInitialDelay = 0.35f;
    [Tooltip("Repeat interval (seconds) while holding")]
    [SerializeField] private float holdRepeatInterval = 0.05f;

    [Header("IAP")]
    [Tooltip("Product ID configured in Unity IAP for Nitro packs")]
    [SerializeField] private string nitroProductId = "nitro_pack"; // set this to your actual IAP id

    public enum MenuType
    {
        DEFAULT = -1,
        CAR_BUY_MENU = 0,
        PART_BUY_MENU = 1,
        PAINT_BUY_MENU = 2
    }
    private bool cameFromGarageMenu;
    private MenuType previousMenuType = MenuType.DEFAULT;

    private ShopItem _currentItem;
    private int _selectedQuantity = 1;                       // starts at 1
    private const int MinSelected = 1;
    private const int MaxSelected = 9999;

    // hold state
    private bool _holdingPlus;
    private bool _holdingMinus;
    private float _plusTimer;
    private float _minusTimer;

    private float _plusHoldDuration;
    private float _minusHoldDuration;

    private void Awake()
    {
        if (nitrocount != null && SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
            nitrocount.text = SaveManager.Instance.SaveData.NitroCount.ToString();
    }

    #region Public entry point (called by your Shop button)
    /// <summary>
    /// Show/populate the popup for a given ShopItem.
    /// </summary>
    public void Show(ShopItem item)
    {
        if (notEnoughNitroPopUp != null) notEnoughNitroPopUp.SetActive(false);

        menu.SetActive(false);
        popups.SetActive(true);

        _currentItem = item;
        _selectedQuantity = 1;

        switch (_currentItem.typeOfItem)
        {
            case ShopItem.ItemType.Nitro:
                ConfigureUIForNitro();
                break;

            case ShopItem.ItemType.Credits:
                ConfigureUIForCredits();
                break;
        }

        // Ensure listeners are set only once
        minusButton.onClick.RemoveAllListeners();
        plusButton.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();

        minusButton.onClick.AddListener(() => ChangeSelected(-1));
        plusButton.onClick.AddListener(() => ChangeSelected(+1));
        buyButton.onClick.AddListener(OnBuyClicked);

        // Add pointer (press/hold) handlers once
        EnsureHoldHandlers(minusButton.gameObject, isDown => { _holdingMinus = isDown; ResetMinusTimer(); });
        EnsureHoldHandlers(plusButton.gameObject, isDown => { _holdingPlus = isDown; ResetPlusTimer(); });

        // Final UI refresh
        RefreshAllTexts();
    }
    #endregion

    public void ConfirmOpenLootCrate(ShopItem lootCrate)
    {
        menu.SetActive(false);
        openLootCratePopup.SetActive(true);

        // Set the query text with underline on itemDescription
        queryText.text = $"Open <u>{lootCrate.itemDescription}</u> Loot Crate?";

        // Set the left and right info texts
        leftInfoText.text = lootCrate.leftDescription;
        rightInfoText.text = lootCrate.rightDescription;

        // Set the crate image texture
        crateImage.texture = lootCrate.crateTexture;

        // Set the open button texts
        string priceLabel = $"OPEN: {(int)lootCrate.priceOfItem}";
        openButtonText.text = priceLabel;
        openButtonTextOutline.text = priceLabel;

        // Store the current item reference
        _currentItem = lootCrate;
    }

    public void OpenLootCrate()
    {
        int nitroCost = Mathf.RoundToInt(_currentItem.priceOfItem);
        int playerNitro = (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
            ? SaveManager.Instance.SaveData.NitroCount
            : 0;

        // Check if the player has enough nitros
        if (playerNitro < nitroCost)
        {
            notEnoughNitroPopUp.SetActive(true);
            return;
        }

        // Deduct nitros
        SaveManager.Instance.SaveData.NitroCount = Mathf.Max(0, playerNitro - nitroCost);
        SaveManager.Instance.SaveGame();

        // Update nitro count UI
        RefreshAllTexts();

        // Continue to open the crate
        mainMenuUI.SetActive(false);
        topLevelButtons.SetActive(true);
        gameObject.SetActive(false);
        garageUI.SetActive(true);
        carDisplay.RandomizeCar();
    }

    #region UI Configuration
    private void ConfigureUIForNitro()
    {
        if (nitroImage != null) nitroImage.SetActive(false);

        if (packTitleText != null)
            packTitleText.text = $"NITRO PACK OF {_currentItem.quantityOfItem}";

        // Center alignment and larger auto-size cap for Nitro buy text
        ApplyBuyTextStyle(TextAlignmentOptions.Center, 72f);
    }

    private void ConfigureUIForCredits()
    {
        if (nitroImage != null) nitroImage.SetActive(true);

        if (packTitleText != null)
            packTitleText.text = $"{_currentItem.quantityOfItem:N0} CR";

        // Left alignment and smaller auto-size cap for Credits buy text
        ApplyBuyTextStyle(TextAlignmentOptions.Center, 45f);
    }

    private void ApplyBuyTextStyle(TextAlignmentOptions alignment, float maxAutoSize)
    {
        if (buyButtonText != null)
        {
            buyButtonText.enableAutoSizing = true;
            buyButtonText.fontSizeMax = maxAutoSize;
            buyButtonText.alignment = alignment;
        }
        if (buyButtonTextOutline != null)
        {
            buyButtonTextOutline.enableAutoSizing = true;
            buyButtonTextOutline.fontSizeMax = maxAutoSize;
            buyButtonTextOutline.alignment = alignment;
        }
    }
    #endregion

    #region Update loop (hold-to-repeat with acceleration)
    private void Update()
    {
        if (_currentItem == null) return;

        // Track hold durations
        if (_holdingPlus) _plusHoldDuration += Time.unscaledDeltaTime;
        else _plusHoldDuration = 0f;

        if (_holdingMinus) _minusHoldDuration += Time.unscaledDeltaTime;
        else _minusHoldDuration = 0f;

        if (_holdingPlus)
        {
            _plusTimer -= Time.unscaledDeltaTime;
            if (_plusTimer <= 0f)
            {
                int step = StepForHold(_plusHoldDuration);                 // 1, 5, 25, 100
                ChangeSelected(+step);
                _plusTimer = IntervalForHold(_plusHoldDuration);           // speeds up over time
            }
        }

        if (_holdingMinus)
        {
            _minusTimer -= Time.unscaledDeltaTime;
            if (_minusTimer <= 0f)
            {
                int step = StepForHold(_minusHoldDuration);                // 1, 5, 25, 100
                ChangeSelected(-step);
                _minusTimer = IntervalForHold(_minusHoldDuration);         // speeds up over time
            }
        }
    }
    #endregion

    #region Quantity & UI refresh
    private void ChangeSelected(int delta)
    {
        int prev = _selectedQuantity;
        _selectedQuantity = Mathf.Clamp(_selectedQuantity + delta, MinSelected, MaxSelected);
        if (_selectedQuantity != prev)
            RefreshAllTexts();
    }

    private void RefreshAllTexts()
    {
        if (nitrocount != null && SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
            nitrocount.text = SaveManager.Instance.SaveData.NitroCount.ToString();

        if (_currentItem == null) return;

        // Quantity numeric field
        if (quantityValueText != null)
            quantityValueText.text = _selectedQuantity.ToString();

        if (_currentItem.typeOfItem == ShopItem.ItemType.Nitro)
        {
            // Use long for total
            long totalNitros = (long)_currentItem.quantityOfItem * _selectedQuantity;
            if (totalText != null)
                totalText.text = $"TOTAL: {totalNitros:N0} NITROS";

            // BUY price (money), 2 decimals with thousands separators
            double price = _selectedQuantity * _currentItem.priceOfItem;
            SetBuyTexts($"BUY: ${price:N2}");
        }
        else // Credits
        {
            // TOTAL line (credits) as long
            long totalCredits = (long)_currentItem.quantityOfItem * _selectedQuantity;
            if (totalText != null)
                totalText.text = $"TOTAL: {totalCredits:N0} CR";

            // BUY cost in nitro cans (no commas, rounded to int)
            int nitroCost = Mathf.RoundToInt(_selectedQuantity * _currentItem.priceOfItem);
            SetBuyTexts($"BUY: {nitroCost:0}");
        }
    }

    private void SetBuyTexts(string value)
    {
        if (buyButtonText != null) buyButtonText.text = value;
        if (buyButtonTextOutline != null) buyButtonTextOutline.text = value;
    }
    #endregion

    #region Buy flow
    private void OnBuyClicked()
    {
        if (_currentItem == null) return;

        switch (_currentItem.typeOfItem)
        {
            case ShopItem.ItemType.Nitro:
                // Example codeless call:
                // CodelessIAPStoreListener.instance.InitiatePurchase(nitroProductId);
                // Or custom manager:
                // IAPManager.Instance.BuyProductId(nitroProductId, OnNitroPurchaseSucceeded, OnPurchaseFailed);

                // Simulation:
                OnNitroPurchaseSucceeded();
                break;

            case ShopItem.ItemType.Credits:
                OnCreditsPurchaseAttempt();
                break;
        }
    }

    // Nitro fulfillment after verified purchase
    public void OnNitroPurchaseSucceeded()
    {
        long grant = (long)_currentItem.quantityOfItem * _selectedQuantity;

        if (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
        {
            // Clamp to int range if SaveData.NitroCount is an int
            long newValue = (long)SaveManager.Instance.SaveData.NitroCount + grant;
            if (newValue > int.MaxValue) newValue = int.MaxValue;
            if (newValue < int.MinValue) newValue = int.MinValue;
            SaveManager.Instance.SaveData.NitroCount = (int)newValue;

            SaveManager.Instance.SaveGame();
        }

        RefreshAllTexts();
        HandleBackButton();
    }

    private void OnCreditsPurchaseAttempt()
    {
        // Cost is in nitro cans; must have enough nitro to proceed.
        int nitroCost = Mathf.RoundToInt(_selectedQuantity * _currentItem.priceOfItem);
        int playerNitro = (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
            ? SaveManager.Instance.SaveData.NitroCount
            : 0;

        if (playerNitro < nitroCost)
        {
            if (notEnoughNitroPopUp != null) notEnoughNitroPopUp.SetActive(true);
            return;
        }

        // Deduct nitro and grant credits
        long grantCreditsLong = (long)_currentItem.quantityOfItem * _selectedQuantity;

        if (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
        {
            SaveManager.Instance.SaveData.NitroCount = Mathf.Max(0, playerNitro - nitroCost);
            SaveManager.Instance.SaveGame();
        }

        // Update credits via CreditManager (method expects int)
        if (CreditManager != null)
        {
            CreditManager.ChangeCredits(grantCreditsLong);
        }
        else
        {
            Debug.LogWarning("ShopMenu: CreditManager GameObject is not assigned.");
        }

        RefreshAllTexts();
        HandleBackButton();
    }

    private void OnPurchaseFailed()
    {
        // Handle purchase failure if needed.
    }
    #endregion

    #region Back navigation
    public void HandleBackButton()
    {
        if((popups != null && popups.activeSelf) || (openLootCratePopup != null && openLootCratePopup.activeSelf))
        {
            notEnoughNitroPopUp.SetActive(false);
            popups.SetActive(false);
            openLootCratePopup.SetActive(false);
            menu.SetActive(true);
        }
        else if (cameFromGarageMenu)
        {
            mainMenuUI.SetActive(false);
            topLevelButtons.SetActive(true);
            gameObject.SetActive(false);
            garageUI.SetActive(true);
            cameFromGarageMenu = false;
            switch (previousMenuType)
            {
                case MenuType.CAR_BUY_MENU:
                    carDisplay.ConfirmBuy();
                    break;
                case MenuType.PART_BUY_MENU:
                    garageUIManager.ConfirmBuyPart(garageUIManager.previousPartType, garageUIManager.previousPartIndex);
                    break;
                case MenuType.PAINT_BUY_MENU:
                    garageUIManager.UseSavedPaintButton();
                    garageUIManager.SetColor(garageUIManager.previousPaintPrice);
                    break;
                default:
                    break;
            }
            previousMenuType = MenuType.DEFAULT;
        }
        else
        {
            gameObject.SetActive(false);
            if (topLevelButtons != null) topLevelButtons.SetActive(true);
        }
    }
    #endregion

    public void HandleEntranceFromGarageMenu(int previousMenu)
    {
        cameFromGarageMenu = true;
        previousMenuType = (MenuType) previousMenu;
    }

    #region Utility: attach hold handlers to buttons
    private void EnsureHoldHandlers(GameObject go, Action<bool> setHolding)
    {
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => setHolding(true));
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => setHolding(false));
        trigger.triggers.Add(up);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => setHolding(false));
        trigger.triggers.Add(exit);
    }

    // Initialize first delay when a hold starts
    private void ResetPlusTimer()
    {
        _plusTimer = _holdingPlus ? holdInitialDelay : 0f;
        if (_holdingPlus) _plusHoldDuration = 0f; // restart ramp each new hold
    }

    private void ResetMinusTimer()
    {
        _minusTimer = _holdingMinus ? holdInitialDelay : 0f;
        if (_holdingMinus) _minusHoldDuration = 0f; // restart ramp each new hold
    }

    // Acceleration helpers
    private int StepForHold(float secondsHeld)
    {
        if (secondsHeld >= 6f) return 100;
        if (secondsHeld >= 4f) return 25;
        if (secondsHeld >= 2f) return 5;
        return 1;
    }

    private float IntervalForHold(float secondsHeld)
    {
        if (secondsHeld >= 6f) return Mathf.Max(0.008f, holdRepeatInterval * 0.16f);
        if (secondsHeld >= 4f) return Mathf.Max(0.018f, holdRepeatInterval * 0.36f);
        if (secondsHeld >= 2f) return Mathf.Max(0.030f, holdRepeatInterval * 0.60f);
        return holdRepeatInterval;
    }
    #endregion
}
