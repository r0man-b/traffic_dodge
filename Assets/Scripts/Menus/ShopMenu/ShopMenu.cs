using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// If you use Unity IAP, uncomment the next line and add the package.
// using UnityEngine.Purchasing;

public class ShopMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject topLevelButtons;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject popups;
    public TextMeshProUGUI nitrocount;

    [Header("Popup UI")]
    [SerializeField] private TMP_Text packTitleText;       // "NITRO PACK OF X"
    [SerializeField] private TMP_Text quantityHeaderText;  // "QUANTITY:" label (optional; can be null)
    [SerializeField] private TMP_Text quantityValueText;   // numeric value between +/- buttons
    [SerializeField] private TMP_Text totalText;           // "TOTAL: X NITROS"
    [SerializeField] private TMP_Text buyButtonText;       // "BUY: $xxxxx.xx" (outline handled in your button component, if any)
    [SerializeField] private TMP_Text buyButtonTextOutline;// "BUY: $xxxxx.xx" (outline handled in your button component, if any)

    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button buyButton;

    [Header("Hold-to-Repeat")]
    [Tooltip("Delay (seconds) before repeating starts while holding +/-")]
    [SerializeField] private float holdInitialDelay = 0.35f;
    [Tooltip("Repeat interval (seconds) while holding")]
    [SerializeField] private float holdRepeatInterval = 0.05f;

    [Header("IAP")]
    [Tooltip("Product ID configured in Unity IAP for Nitro packs")]
    [SerializeField] private string nitroProductId = "nitro_pack"; // set this to your actual IAP id

    private ShopItem _currentItem;
    private int _selectedQuantity = 1;                       // starts at 1
    private const int MinSelected = 1;
    private const int MaxSelected = 9999;

    // hold state
    private bool _holdingPlus;
    private bool _holdingMinus;
    private float _plusTimer;
    private float _minusTimer;

    private void Awake()
    {
        nitrocount.text = SaveManager.Instance.SaveData.NitroCount.ToString();
    }

    #region Public entry point (called by your Shop button)
    /// <summary>
    /// Call this to show/populate the popup for a given ShopItem. Only Nitro is implemented.
    /// </summary>
    public void Show(ShopItem item)
    {
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
                // Intentionally not implemented in this version.
                ConfigureUIForCreditsPlaceholder();
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

    #region UI Configuration
    private void ConfigureUIForNitro()
    {
        if (packTitleText != null)
            packTitleText.text = $"NITRO PACK OF {_currentItem.quantityOfItem}";
        // quantityHeaderText may already say "QUANTITY:" in the art; leave it if null.
    }

    private void ConfigureUIForCreditsPlaceholder()
    {
        packTitleText.text = "CREDITS (NOT IMPLEMENTED)";
        quantityValueText.text = "-";
        totalText.text = "-";
        buyButtonText.text = "BUY";
        buyButtonTextOutline.text = "BUY";
    }
    #endregion

    #region Update loop (hold-to-repeat)
    // Add fields
    private float _plusHoldDuration;
    private float _minusHoldDuration;

    // Replace Update() with this accelerated version
    private void Update()
    {
        if (_currentItem == null || _currentItem.typeOfItem != ShopItem.ItemType.Nitro)
            return;

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
        nitrocount.text = SaveManager.Instance.SaveData.NitroCount.ToString();

        if (_currentItem == null) return;

        if (_currentItem.typeOfItem == ShopItem.ItemType.Nitro)
        {
            // Quantity numeric field
            if (quantityValueText != null)
                quantityValueText.text = _selectedQuantity.ToString();

            // Total nitros = quantityOfItem * selectedQuantity
            int totalNitros = _currentItem.quantityOfItem * _selectedQuantity;
            if (totalText != null)
                totalText.text = $"TOTAL: {totalNitros:N0} NITROS";

            // Buy price = quantityOfItem * selectedQuantity * priceOfItem  (as requested)
            double price = _selectedQuantity * _currentItem.priceOfItem;
            buyButtonText.text = $"BUY: ${price:N2}";
            buyButtonTextOutline.text = $"BUY: ${price:N2}";
        }
        else
        {
            // Credits path intentionally not implemented
        }
    }
    #endregion

    #region Buy flow (Unity IAP integration point)
    private void OnBuyClicked()
    {
        // Initiate purchase with your configured IAP product id.
        // You must have this product configured in the IAP catalog.
        // The actual fulfillment (granting items) should occur **after** a successful purchase.
        //
        // Example using Codeless IAP:
        // CodelessIAPStoreListener.instance.InitiatePurchase(nitroProductId);
        //
        // If you have a custom IAP manager, replace this call accordingly and make sure
        // that it invokes OnNitroPurchaseSucceeded() upon confirmed success.

        // Placeholder: call your IAP manager here.
        // IAPManager.Instance.BuyProductId(nitroProductId, OnNitroPurchaseSucceeded, OnPurchaseFailed);

        // Remove the following line in production. It simulates an immediate success to demonstrate wiring.
        // --- Simulation start ---
        OnNitroPurchaseSucceeded();
        // --- Simulation end ---
    }

    /// <summary>
    /// Call this from your IAP success callback (after the transaction is confirmed).
    /// This method performs the actual fulfillment: add nitros to saved data.
    /// </summary>
    public void OnNitroPurchaseSucceeded()
    {
        int grant = _currentItem.quantityOfItem * _selectedQuantity;

        // Guard for your SaveManager singleton.
        if (SaveManager.Instance != null && SaveManager.Instance.SaveData != null)
        {
            SaveManager.Instance.SaveData.NitroCount += grant;
            SaveManager.Instance.SaveGame();
        }

        // Optionally close the popup or refresh other UI here.
        RefreshAllTexts();
    }

    // Optional failure hook
    private void OnPurchaseFailed()
    {
        // Handle failure UI/telemetry as needed.
    }
    #endregion

    #region Utility: attach hold handlers to buttons
    private void EnsureHoldHandlers(GameObject go, Action<bool> setHolding)
    {
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        // Clear existing entries we may have added earlier
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

        // PointerDown
        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => setHolding(true));
        trigger.triggers.Add(down);

        // PointerUp
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => setHolding(false));
        trigger.triggers.Add(up);

        // PointerExit (stop if the user drags off the button)
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => setHolding(false));
        trigger.triggers.Add(exit);
    }

    public void HandleBackButton()
    {
        if (popups != null && popups.activeSelf)
        {
            // Case: User is inside the quantity-purchase popup
            popups.SetActive(false);
            menu.SetActive(true);
        }
        else
        {
            // Case: User is not in the popup, go back to "Buttons"
            gameObject.SetActive(false);
            topLevelButtons.SetActive(true);
        }
    }


    // Update Reset*Timer to initialize the first delay when a hold starts
    private void ResetPlusTimer()
    {
        _plusTimer = _holdingPlus ? holdInitialDelay : 0f;
        if (_holdingPlus) _plusHoldDuration = 0f;          // restart ramp each new hold
    }

    private void ResetMinusTimer()
    {
        _minusTimer = _holdingMinus ? holdInitialDelay : 0f;
        if (_holdingMinus) _minusHoldDuration = 0f;        // restart ramp each new hold
    }

    // Add these helpers (tune thresholds/values as desired)
    private int StepForHold(float secondsHeld)
    {
        if (secondsHeld >= 6f) return 100;
        if (secondsHeld >= 4f) return 25;
        if (secondsHeld >= 2f) return 5;
        return 1;
    }

    private float IntervalForHold(float secondsHeld)
    {
        // Start from your base holdRepeatInterval and reduce it as the hold continues.
        // Example: base=0.05s → 0.05, 0.03, 0.018, 0.008
        if (secondsHeld >= 6f) return Mathf.Max(0.008f, holdRepeatInterval * 0.16f);
        if (secondsHeld >= 4f) return Mathf.Max(0.018f, holdRepeatInterval * 0.36f);
        if (secondsHeld >= 2f) return Mathf.Max(0.030f, holdRepeatInterval * 0.60f);
        return holdRepeatInterval;
    }
    #endregion
}
