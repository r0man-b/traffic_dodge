using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI carName;
    [SerializeField] private TextMeshProUGUI carPrice;
    [SerializeField] private TextMeshProUGUI carTopSpeed;
    [SerializeField] private TextMeshProUGUI carHorsepower;
    [SerializeField] private TextMeshProUGUI carZerotosixty;
    [SerializeField] private TextMeshProUGUI carLives;
    [SerializeField] private TextMeshProUGUI carPowerplant;
    [SerializeField] private Transform carHolder;

    [Header("Car Prefabs")]
    [SerializeField] private GameObject[] carPrefabs; // index aligned with car type index
    private readonly Dictionary<string, int> typeIndexByName = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

    [Header("Custom Objects")]
    public CreditManager creditManager;
    public GarageUIManager garageUIManager;

    [Header("Lock UI image/background")]
    public GameObject lockUiElement;
    public GameObject lockImage;

    [Header("Bottom UI Button Sets")]
    public GameObject buttonSet1;
    public GameObject buttonSet2;
    public GameObject buttonSet3;
    public GameObject leftButton;
    public GameObject rightButton;

    [Header("Buy/sell Confirmation Popups")]
    public GameObject popUps;
    public GameObject buyConfirmationPopUp;
    public TextMeshProUGUI buyConfirmationPopUpText;
    public GameObject notEnoughCreditsPopUp;
    public TextMeshProUGUI notEnoughCreditsPopUpText;
    public GameObject sellConfirmationPopUp;
    public TextMeshProUGUI sellConfirmationPopUpText;
    public GameObject cannotSellPopUp;
    public TextMeshProUGUI cannotSellPopUpText;

    [Header("Loot Crate Popups")]
    public GameObject lootCratePopUps;
    public GameObject addOrSellPopUp;
    public TextMeshProUGUI addOrSellPopUpText;
    public GameObject addButton;
    public GameObject replaceButton;
    public TextMeshProUGUI sellButtonText;
    public GameObject returnOrSpinAgainPopUp;
    public TextMeshProUGUI returnOrSpinAgainPopUpText;

    [Header("UI elements that get disabled/enabled during/after lootbox spin")]
    public GameObject nitroObject;
    public GameObject backButton;
    public GameObject goRaceButton;
    public GameObject shopMenu;
    public ShopMenu shopMenuScript; // Need to also call the ResetUI() function in the shop menu script
    public GameObject mainMenuUI;
    public GameObject topLevelMainMenuButtons;
    public GameObject garageUI;
    public GarageUIManager garageUIscript; // Need to also call the ChangeCar() function in the garage menu script

    [Header("Sound")]
    public MenuSounds menuSounds;

    [Header("Car Management")]
    private Car currentCar;
    private GameObject _spawnedModel;
    private string currentCarType;
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;
    private int sellPrice;
    private const string carsOwned = "CARS_OWNED";
    public bool typeNameIndexBuilt = false;

    [Header("Lootbox Spin Parameters")]
    private readonly int spinCount = 100;
    private readonly float startDelay = 0.2f;   // fast at start
    private readonly float endDelay = 0.8228f;   // slow at end
    private readonly float slowDownBias = 2f;
    public CarCollection carCollection;
    Coroutine _spinCo;
    [SerializeField] private GameObject carContainer;
    [SerializeField] private float spinMaxSpeed = 360f;  // deg/sec at start of spin
    [SerializeField] private float spinMinSpeed = 60f;  // deg/sec near the end
    private float tapMaxDuration = 0.175f; // How long user has to hold tap until lootbox spin is skipped
    private Coroutine _turntableCo;
    private Quaternion _turntableStartRot;
    private bool skipRequested;
    private bool _listenForSkip;
    private float _pressStart = -1f;
    private int _skipFingerId = -1;
    private int _cachedLootboxSellPrice = -1;

    /// <summary>
    /// Update function used solely for skipping a lootbox animation.
    /// </summary>
    private void Update()
    {
        if (!_listenForSkip || skipRequested) return;

        // ----- Mouse (Editor/Standalone) -----
        if (Input.GetMouseButtonDown(0))
        {
            _pressStart = Time.unscaledTime;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (_pressStart >= 0f)
            {
                float held = Time.unscaledTime - _pressStart;
                _pressStart = -1f;
                if (held <= tapMaxDuration)
                {
                    skipRequested = true;  // quick tap => skip
                    EndSkipListen();
                    return;
                }
                // longer hold => ignore (keep listening)
            }
        }

        // ----- Touch (Mobile) -----
        if (Input.touchCount > 0)
        {
            // use the first active finger (or the one we captured)
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (_skipFingerId == -1 && t.phase == TouchPhase.Began)
                {
                    _skipFingerId = t.fingerId;
                    _pressStart = Time.unscaledTime;
                }

                if (t.fingerId != _skipFingerId) continue;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (_pressStart >= 0f)
                    {
                        float held = Time.unscaledTime - _pressStart;
                        _pressStart = -1f;
                        _skipFingerId = -1;

                        if (held <= tapMaxDuration)
                        {
                            skipRequested = true;  // quick tap => skip
                            EndSkipListen();
                            return;
                        }
                        // longer hold => ignore (keep listening)
                    }
                }
            }
        }
    }


    /*------------------------------------------------------------------------------------------------*/
    /*----------------------------- CAR DISPLAY & MANAGEMENT FUNCTIONS -------------------------------*/
    /*------------------------------------------------------------------------------------------------*/

    // Activate and display car on turntable in garage.
    public GameObject DisplayCar(Car _car, string carType, int carIndex, bool lootboxCar)
    {
        currentCar = _car;
        currentCarType = carType;
        currentCarIndex = carIndex;

        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Get the count of cars owned for the current car type (string).
        numOfThisCarTypeOwned = saveData.Cars.Count(car => car.Key.CarType == currentCarType);

        // Resolve prefab index by type name.
        if (!typeIndexByName.TryGetValue(currentCarType, out int prefabIndex))
        {
            Debug.LogWarning($"CarDisplay: No prefab index found for type '{currentCarType}'.");
            return null;
        }
        if (carPrefabs == null || prefabIndex < 0 || prefabIndex >= carPrefabs.Length || carPrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"CarDisplay: carPrefabs[{prefabIndex}] is not assigned.");
            return null;
        }

        // Deactivate previously shown instance.
        if (_spawnedModel != null)
            _spawnedModel.SetActive(false);

        // Select the pre-placed instance for this car type.
        _spawnedModel = carPrefabs[prefabIndex];

        if (lootboxCar)
        {
            carName.text = currentCar.car_name;

            _car.RandomizeCar(currentCarType, currentCarIndex, _spawnedModel.transform, false);

            // For lootbox path we place using local offsets under holder.
            // Your previous code overwrote the local position with the turntable values directly.
            _spawnedModel.transform.localPosition = new Vector3(
                currentCar.turntablePositon.x,
                currentCar.turntablePositon.y,
                currentCar.turntablePositon.z
            );
            _spawnedModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            carName.text = currentCar.car_name + (currentCarIndex > 0 ? " (" + currentCarIndex + ")" : "");
            carPrice.text = currentCar.price.ToString("N0") + " cr";
            carPowerplant.text = currentCar.powerplant;

            bool isOwned = saveData.Cars.ContainsKey((currentCarType, currentCarIndex));
            if (isOwned)
            {
                lockUiElement.SetActive(false);
                lockImage.SetActive(false);
                buttonSet1.SetActive(false);
                buttonSet2.SetActive(true); // Set all bottom buttons to be visible if car is owned.
            }
            else
            {
                lockUiElement.SetActive(true);
                lockImage.SetActive(true);
                buttonSet1.SetActive(true); // Set only the 'buy' button to be visible if car is not owned.
                buttonSet2.SetActive(false);
            }

            _car.InitializeCar(currentCarType, currentCarIndex, _spawnedModel.transform, isOwned);

            // Non-lootbox path previously used world pose; mirror that logic.
            // Parent is already carHolder; set world position/rotation accordingly.
            _spawnedModel.transform.SetParent(carHolder, true); // keep world space while changing parent
            _spawnedModel.transform.SetPositionAndRotation(currentCar.turntablePositon, carHolder.rotation);
        }


        _spawnedModel.SetActive(true);
        return _spawnedModel;
    }

    // Display car purchase confirmation dialogue, check if player has enough currency.
    public void ConfirmBuy()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        popUps.SetActive(true);
        lockImage.SetActive(false);
        numOfThisCarTypeOwned = saveData.Cars.Count(car => car.Key.CarType == currentCarType);

        if (creditManager.GetCredits() < currentCar.price)
        {
            notEnoughCreditsPopUpText.text = "You  do  not  have  enough  credits  to   purchase  this  car";
            notEnoughCreditsPopUp.SetActive(true);
            buyConfirmationPopUp.SetActive(false);
            cannotSellPopUp.SetActive(false);
            sellConfirmationPopUp.SetActive(false);
        }
        else if (numOfThisCarTypeOwned >= 100)
        {
            cannotSellPopUpText.text = "You  cannot  buy  anymore  cars  of  this  type";
            cannotSellPopUp.SetActive(true);
            notEnoughCreditsPopUp.SetActive(false);
            buyConfirmationPopUp.SetActive(false);
            sellConfirmationPopUp.SetActive(false);
        }
        else
        {
            notEnoughCreditsPopUp.SetActive(false);
            sellConfirmationPopUp.SetActive(false);
            cannotSellPopUp.SetActive(false);

            if (numOfThisCarTypeOwned > 0)
            {
                // Strip "The" from the car name if it starts with it.
                string displayName = carName.text.StartsWith("The ", System.StringComparison.OrdinalIgnoreCase) ? currentCar.car_name.Substring(4) : currentCar.car_name;
                buyConfirmationPopUpText.text = "Buy  another  <u>" + displayName + "</u>  for  " + carPrice.text + "?";
            }
            else
            {
                buyConfirmationPopUpText.text = "Buy " + (carName.text.StartsWith("The", System.StringComparison.OrdinalIgnoreCase) ? "" : " a ") + " <u>" + carName.text + "</u>  for  " + carPrice.text + "?";
            }

            buyConfirmationPopUp.SetActive(true);
        }
    }

    // Buy the car. Set car to owned, adjust player currency.
    public void BuyCar()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        numOfThisCarTypeOwned += 1;

        var newCarKey = (CarType: currentCarType, CarIndex: numOfThisCarTypeOwned - 1);

        // Check if the car is already owned, and if not, mark it as owned.
        if (!saveData.Cars.ContainsKey(newCarKey))
        {
            SaveData.CarData newCarData = new SaveData.CarData();
            saveData.Cars[newCarKey] = newCarData; // Add the new car to the save data.
        }

        // Deduct the car price from the player's credits.
        creditManager.ChangeCredits(-currentCar.price);

        // Update the UI accordingly.
        lockUiElement.SetActive(false);
        popUps.SetActive(false);
        buttonSet1.SetActive(false);
        buttonSet2.SetActive(true);
        menuSounds.PlayChaChing();

        // Save the last owned car and update the saved data.
        garageUIManager.SetLastOwnedCar();
        SaveManager.Instance.SaveGame(); // Save the updated data.

        // Change the displayed car if multiple cars of this type are owned.
        if (numOfThisCarTypeOwned > 1)
        {
            garageUIManager.ChangeCar(numOfThisCarTypeOwned - currentCarIndex - 1);
        }

        buyConfirmationPopUp.SetActive(false);
        notEnoughCreditsPopUp.SetActive(false);
        sellConfirmationPopUp.SetActive(false);
        cannotSellPopUp.SetActive(false);
    }

    // Display car sell confirmation dialogue, check if player has at least one car.
    public void ConfirmSell()
    {
        popUps.SetActive(true);

        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Check if the player has only one car in total.
        if (saveData.Cars.Count == 1)
        {
            cannotSellPopUpText.text = "You   cannot   sell   your   last   car ! ! !";
            cannotSellPopUp.SetActive(true);
            buyConfirmationPopUp.SetActive(false);
            notEnoughCreditsPopUp.SetActive(false);
            sellConfirmationPopUp.SetActive(false);
        }
        else
        {
            buyConfirmationPopUp.SetActive(false);
            notEnoughCreditsPopUp.SetActive(false);
            cannotSellPopUp.SetActive(false);
            sellPrice = garageUIManager.GetSellPrice();

            // Count how many cars of the current type the player owns.
            numOfThisCarTypeOwned = saveData.Cars.Count(car => car.Key.CarType == currentCarType);

            if (numOfThisCarTypeOwned > 1)
            {
                // Strip "The" from the car name if it starts with it.
                string displayName = carName.text.StartsWith("The ", System.StringComparison.OrdinalIgnoreCase) ? currentCar.car_name.Substring(4) : currentCar.car_name;
                sellConfirmationPopUpText.text = "Sell  one  of  your  <u>" + displayName + "s</u>  for  " + sellPrice.ToString("N0") + "  CR?";
            }
            else
            {
                sellConfirmationPopUpText.text = "Sell " + (carName.text.StartsWith("The", System.StringComparison.OrdinalIgnoreCase) ? "" : " your ") + " <u>" + carName.text + "</u>   for  " + sellPrice.ToString("N0") + "  CR?";
            }

            sellConfirmationPopUp.SetActive(true);
        }
    }

    // Sell the car. Set car to unowned, add car's value to player currency.
    public void SellCar()
    {
        numOfThisCarTypeOwned -= 1;

        if (currentCarIndex == numOfThisCarTypeOwned && numOfThisCarTypeOwned > 0) // Selling the last type of this car.
        {
            Debug.Log(Time.time + " SELLING A CAR AT THE END");
            garageUIManager.ClearOwnedParts(currentCarIndex);
            creditManager.ChangeCredits(sellPrice);
            lockUiElement.SetActive(true);
            lockImage.SetActive(true);
            popUps.SetActive(false);
            menuSounds.PlayChaChing();
            garageUIManager.ChangeCar(-1);
        }
        else if (numOfThisCarTypeOwned > 0) // If selling a car in the middle, use different logic.
        {
            Debug.Log(Time.time + " SELLING A CAR IN THE MIDDLE");
            // Shift each car's customizations to the car to the left.
            garageUIManager.AdjustCarIndices(currentCarType, currentCarIndex);
            garageUIManager.ClearOwnedParts(numOfThisCarTypeOwned); // Clear the last car in the list.
            creditManager.ChangeCredits(sellPrice);
            lockUiElement.SetActive(true);
            lockImage.SetActive(true);
            popUps.SetActive(false);
            menuSounds.PlayChaChing();
            if (currentCarIndex == 0) garageUIManager.ChangeCar(0);
            else garageUIManager.ChangeCar(-1);
        }
        else
        {
            Debug.Log(Time.time + " SELLING LAST CAR");
            garageUIManager.ClearOwnedParts(currentCarIndex);
            creditManager.ChangeCredits(sellPrice);
            lockUiElement.SetActive(true);
            lockImage.SetActive(true);
            popUps.SetActive(false);
            buttonSet1.SetActive(true);
            buttonSet2.SetActive(false);
            menuSounds.PlayChaChing();
            // DisplayCar(currentCar, currentCarType, currentCarIndex);
        }

        garageUIManager.UpdatePerformanceStats();
        SaveManager.Instance.SaveGame(); // Save changes to the data.
        buyConfirmationPopUp.SetActive(false);
        notEnoughCreditsPopUp.SetActive(false);
        sellConfirmationPopUp.SetActive(false);
        cannotSellPopUp.SetActive(false);
    }

    // Update performance stats text.
    public void UpdateStats(float accelMaxValue, float accelIncreaseRate, int numlives)
    {
        bool isImperial = SaveManager.Instance.SaveData.ImperialUnits;
        double topSpeed = isImperial
            ? System.Math.Round(85.36585365f * accelMaxValue + 12.3170731707f)
            : System.Math.Round((85.36585365f * accelMaxValue + 12.3170731707f) * 1.60934f);

        carTopSpeed.text = isImperial ? topSpeed + " mph" : topSpeed + " kph";
        carHorsepower.text = System.Math.Round(513.57616f * accelMaxValue - 608.44812f).ToString() + " hp";
        carZerotosixty.text = System.Math.Round(Mathf.Max(-12.28856f * accelIncreaseRate + 23.2393f, -5.484f * accelIncreaseRate + 12.068f), 1).ToString() + "s";
        carLives.text = numlives.ToString();
    }


    /*------------------------------------------------------------------------------------------------*/
    /*----------------------------------- CAR LOOTBOX FUNCTIONS --------------------------------------*/
    /*------------------------------------------------------------------------------------------------*/

    // Start randomize car & spin turntable coroutine for lootboxes.
    public void RandomizeCar()
    {
        // Set all buttons and widges to be inactive.
        lockUiElement.SetActive(false);
        lockImage.SetActive(false);
        buttonSet1.SetActive(false);
        buttonSet2.SetActive(false);
        leftButton.SetActive(false);
        rightButton.SetActive(false);
        nitroObject.SetActive(false);
        backButton.SetActive(false);
        goRaceButton.SetActive(false);

        skipRequested = false;     // reset each run
        EndSkipListen();           // just to be safe
        if (_spinCo != null) StopCoroutine(_spinCo);
        _spinCo = StartCoroutine(RandomizeRoutine());
    }

    // Car randomization coroutine.
    IEnumerator RandomizeRoutine()
    {
        List<float> delays = BuildDelaySchedule();
        float totalDuration = 0f;
        for (int i = 0; i < delays.Count; i++) totalDuration += delays[i];

        _turntableStartRot = (carHolder != null) ? carHolder.rotation : Quaternion.identity;
        _turntableCo = StartCoroutine(SpinTurntable(totalDuration));

        BeginSkipListen();  // <-- start listening for a quick tap here

        bool earlySkipTriggered = false;
        bool finalCarSpawned = false;

        for (int i = 0; i < spinCount - 3; i++)
        {
            if (skipRequested)
            {
                earlySkipTriggered = true;
                break;
            }

            SpawnWeightedRandomCar();
            yield return new WaitForSecondsRealtime(delays[i]);
        }

        if (!earlySkipTriggered)
            finalCarSpawned = true;

        if (skipRequested)
        {
            // stop spinning and snap to final pose
            if (_turntableCo != null)
            {
                StopCoroutine(_turntableCo);
                _turntableCo = null;
            }
            if (carHolder != null)
                carHolder.rotation = _turntableStartRot;

            if (!finalCarSpawned)
                SpawnWeightedRandomCar();

            EndSkipListen();   // <-- stop listening
            _spinCo = null;
            HandlePostSpin();
            yield break;
        }
        else
        {
            if (_turntableCo != null)
                yield return _turntableCo; // wait until turntable finishes/returns
        }

        EndSkipListen();       // <-- stop listening
        _spinCo = null;
        HandlePostSpin();
    }

    // Turntable spin coroutine.
    IEnumerator SpinTurntable(float totalDuration)
    {
        // Spin the container (falls back to carHolder if not set)
        var target = carHolder;
        if (target == null || totalDuration <= 0f) yield break;

        // Remember the exact starting WORLD rotation so we can return to it smoothly.
        Quaternion startWorldRot = target.rotation;

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            // 0..1 time, eased with the same slowDownBias as your car picks
            float u = Mathf.Clamp01(elapsed / totalDuration);
            float uBias = Mathf.Pow(u, slowDownBias);

            // Decaying angular speed (deg/sec)
            float omega = Mathf.Lerp(spinMaxSpeed, spinMinSpeed, uBias);

            // Rotate around WORLD up so it’s always a horizontal spin
            target.Rotate(Vector3.up, omega * dt, Space.World);
            yield return null;
        }

        // --- Smoothly return to the exact starting rotation ---
        const float returnDuration = 0.35f;  // tweak to taste
        float t = 0f;
        Quaternion from = target.rotation;

        while (t < returnDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / returnDuration;
            // Ease-out (cubic): fast -> slow
            k = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);
            target.rotation = Quaternion.Slerp(from, startWorldRot, k);
            yield return null;
        }

        // Ensure perfect final alignment
        target.rotation = startWorldRot;
    }

    // Post spin UI.
    private void HandlePostSpin()
    {
        // Compute & cache the sell price from the currently displayed (randomized) car
        _cachedLootboxSellPrice = ComputeLootboxSellPrice();
        string name = currentCar != null ? currentCar.car_name : "car";

        // Activate post-spin UI popups
        lootCratePopUps.SetActive(true);
        addOrSellPopUp.SetActive(true);
        addOrSellPopUpText.text =
            $"Congratulations! You won a <u>{name}</u>. " +
            $"You can now choose to add it to your garage or sell it for { _cachedLootboxSellPrice.ToString("N0") } CR.";
        sellButtonText.text = $"SELL FOR { _cachedLootboxSellPrice.ToString("N0") } CR";

        // Deactivate car name string
        carName.gameObject.SetActive(false);
    }

    // Add randomized lootbox car to garage.
    public void AddLootboxCarToGarage()
    {
        var saveData = SaveManager.Instance.SaveData;

        // Enforce the same per-type cap used in ConfirmBuy()
        const int maxPerType = 100;
        int ownedOfThisType = saveData.Cars.Count(c => c.Key.CarType == currentCarType);

        if (ownedOfThisType >= maxPerType)
        {
            if (lootCratePopUps != null) lootCratePopUps.SetActive(true);
            if (addOrSellPopUp != null) addOrSellPopUp.SetActive(true);

            EnsureLootboxSellPriceCached();

            string displayName = string.IsNullOrWhiteSpace(currentCar?.car_name) ? currentCar?.name : currentCar.car_name;
            if (addOrSellPopUpText != null)
            {
                addOrSellPopUpText.text =
                    $"You cannot add another <u>{displayName}</u> to your garage. " +
                    $"You already own the maximum of {maxPerType} for this car type. " +
                    $"You may sell it for { _cachedLootboxSellPrice.ToString("N0") } CR.";
            }
            addButton.SetActive(false);
            replaceButton.SetActive(true);
            return;
        }

        // Determine next index for this type and create/overwrite record
        int nextIndex = saveData.Cars.Count(c => c.Key.CarType == currentCarType);
        var newCarKey = (CarType: currentCarType, CarIndex: nextIndex);

        if (!saveData.Cars.TryGetValue(newCarKey, out SaveData.CarData carData))
        {
            carData = new SaveData.CarData();
            saveData.Cars[newCarKey] = carData;
        }

        // Snapshot from the displayed, randomized model
        var snapshot = BuildCarDataFromDisplayedModel();
        saveData.Cars[newCarKey] = snapshot;

        // Persist selection pointers
        saveData.LastOwnedCarType = currentCarType;
        saveData.LastOwnedCarIndex = nextIndex;
        saveData.CurrentCarType = currentCarType;   // optional
        saveData.CurrentCarIndex = nextIndex;       // optional

        SaveManager.Instance.SaveGame();

        lootCratePopUps.SetActive(true);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(true);
        returnOrSpinAgainPopUpText.text = $"You have added a <u>{currentCar.car_name}</u> to your garage.";
    }

    // Sell randomized lootbox car for credits.
    public void SellLootboxCarForCredits()
    {
        int amount = (_cachedLootboxSellPrice >= 0) ? _cachedLootboxSellPrice : ComputeLootboxSellPrice();

        // Award credits
        creditManager.ChangeCredits(amount);
        SaveManager.Instance.SaveGame();

        // UI feedback
        if (menuSounds != null) menuSounds.PlayChaChing();
        lootCratePopUps.SetActive(true);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(true);
        returnOrSpinAgainPopUpText.text = $"You sold a <u>{currentCar.car_name}</u> for {amount.ToString("N0")} CR.";

        // Reset the cache now that the transaction is done (optional)
        _cachedLootboxSellPrice = -1;
    }

    // Compute sell price for the *currently randomized (lootbox)* car on the turntable:
    // base = half of car price + 1/4 of each active part price,
    // skipping default parts for slots: 0,2,3,4,6 (Exhaust, Front/Rear Wheels, Rear Splitter, Spoiler).
    private int ComputeLootboxSellPrice()
    {
        if (currentCar == null) return 0;

        int total = currentCar.price / 2;

        // Find the live model (preferred) or fall back to prefab to avoid null issues.
        Transform modelRoot = _spawnedModel != null ? _spawnedModel.transform : currentCar.carModel.transform;
        Transform body = modelRoot != null ? modelRoot.Find("BODY") : null;
        if (body == null) return total;

        PartHolder PH(Transform t) => t ? t.GetComponent<PartHolder>() : null;

        // Wheels live at root in your hierarchy (not under BODY)
        PartHolder frontWheels = PH((_spawnedModel != null ? _spawnedModel.transform : currentCar.carModel.transform).Find("FRONT_WHEELS"));
        PartHolder rearWheels = PH((_spawnedModel != null ? _spawnedModel.transform : currentCar.carModel.transform).Find("REAR_WHEELS"));

        // Performance root
        Transform perf = body.Find("PERFORMANCE_PARTS");

        // Map holders by slot index to mirror your save/indexing scheme
        var holders = new List<(int slot, PartHolder holder)>
        {
            (0,  PH(body.Find("EXHAUSTS"))),
            (1,  PH(body.Find("FRONT_SPLITTERS"))),
            (2,  frontWheels),
            (3,  PH(body.Find("REAR_SPLITTERS"))),
            (4,  rearWheels),
            (5,  PH(body.Find("SIDESKIRTS"))),
            (6,  PH(body.Find("SPOILERS"))),
            (7,  PH(body)),                         // SUSPENSIONS lives on BODY
            (8,  PH(perf ? perf.Find("ENGINE") : null)),
            (9,  PH(perf ? perf.Find("TRANSMISSION") : null)),
            (10, PH(perf ? perf.Find("LIVES") : null)),
            (11, PH(body.Find("DECALS"))),
            (12, PH(body.Find("LIVERIES")))
        };

        // Helper to get active CarPart (falls back to first if none flagged active)
        CarPart ActivePart(PartHolder h)
        {
            if (h == null) return null;
            var parts = h.GetPartArray();
            if (parts == null || parts.Length == 0) return null;
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] != null && parts[i].gameObject.activeSelf)
                    return parts[i];
            return parts[0];
        }

        foreach (var (slot, holder) in holders)
        {
            var part = ActivePart(holder);
            if (part == null) continue;

            bool isDefault = slot switch
            {
                0 => !string.IsNullOrEmpty(currentCar.DefaultExhaust) && part.name == currentCar.DefaultExhaust,
                2 => !string.IsNullOrEmpty(currentCar.DefaultFrontWheels) && part.name == currentCar.DefaultFrontWheels,
                3 => !string.IsNullOrEmpty(currentCar.DefaultRearSplitter) && part.name == currentCar.DefaultRearSplitter,
                4 => !string.IsNullOrEmpty(currentCar.DefaultRearWheels) && part.name == currentCar.DefaultRearWheels,
                6 => !string.IsNullOrEmpty(currentCar.DefaultSpoiler) && part.name == currentCar.DefaultSpoiler,
                _ => false
            };

            if (!isDefault)
            {
                total += ((int)part.price) / 4;
            }
        }

        return total;
    }

    public void PrepareToReplaceOwnedCarWithLootboxCar()
    {
        ExitToGarage();

        buttonSet1.SetActive(false);
        buttonSet2.SetActive(false);
        buttonSet3.SetActive(true);

        garageUIscript.inCarReplaceState = true;
    }

    // Replace an existing car that the player owns with the randomized lootbox car.
    // Only called if player already has max copies of a certain car type owned.
    public void ReplaceOwnedCarWithLootboxCar()
    {
        var save = SaveManager.Instance.SaveData;

        if (save == null || currentCar == null || _spawnedModel == null)
        {
            Debug.LogWarning("ReplaceOwnedCarWithLootboxCar: Missing SaveData or no lootbox car displayed.");
            return;
        }

        string type = currentCarType;
        int index = currentCarIndex;
        var key = (CarType: type, CarIndex: index);

        if (!save.Cars.ContainsKey(key))
        {
            Debug.LogWarning($"ReplaceOwnedCarWithLootboxCar: No owned car found at ({type}, {index}) to replace.");
            return;
        }

        // Snapshot from the displayed, randomized model
        SaveData.CarData replacement = BuildCarDataFromDisplayedModel();
        save.Cars[key] = replacement;

        // Update pointers
        save.CurrentCarType = type;
        save.CurrentCarIndex = index;
        save.LastOwnedCarType = type;
        save.LastOwnedCarIndex = index;

        SaveManager.Instance.SaveGame();

        lootCratePopUps.SetActive(true);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(true);

        string awardedName = string.IsNullOrWhiteSpace(currentCar?.car_name) ? currentCar?.name : currentCar.car_name;
        returnOrSpinAgainPopUpText.text = $"Replaced your car with a <u>{awardedName}</u>.";
    }

    // UI flow for re-opening a car lootbox after spin.
    public void OpenLootboxAgain()
    {
        ResetUIElements();

        // Re-enable shop menu
        mainMenuUI.SetActive(true);
        topLevelMainMenuButtons.SetActive(false);
        shopMenu.SetActive(true);

        // Disable the currently displayed popups
        lootCratePopUps.SetActive(false);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(false);

        // Disable the garage ui display
        garageUI.SetActive(false);
    }

    // UI flow for returning to shop after spin.
    public void ExitToShop()
    {
        ResetUIElements();

        // Re-enable shop menu
        mainMenuUI.SetActive(true);
        topLevelMainMenuButtons.SetActive(false);
        shopMenu.SetActive(true);

        // Disable the currently displayed popups
        lootCratePopUps.SetActive(false);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(false);

        // Disable the garage ui display
        garageUI.SetActive(false);

        // Reset to default shop menu
        shopMenuScript.ResetAllUI();
    }

    // UI flow for exiting to garage after spin.
    public void ExitToGarage()
    {
        // Put the garage UI back
        ResetUIElements();

        // Close loot crate UI
        lootCratePopUps.SetActive(false);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(false);

        // Show the garage, hide the shop
        if (garageUI != null) garageUI.SetActive(true);
        if (shopMenu != null) shopMenu.SetActive(false);

        var save = SaveManager.Instance.SaveData;

        // Choose the target car based on LastOwnedCarType/Index (set when a lootbox car is added).
        string type = save.LastOwnedCarType;
        int index = save.LastOwnedCarIndex;

        // If that pair is invalid (e.g., player sold, or old data), fall back gracefully.
        Car target = FindCarAssetByType(type);
        bool pairExists = save.Cars.ContainsKey((type, index));

        if (target == null || !pairExists)
        {
            // Try current selection
            if (!string.IsNullOrEmpty(save.CurrentCarType) &&
                save.Cars.ContainsKey((save.CurrentCarType, save.CurrentCarIndex)))
            {
                type = save.CurrentCarType;
                index = save.CurrentCarIndex;
                target = FindCarAssetByType(type);
            }
            // Else pick the first owned car
            else if (save.Cars.Count > 0)
            {
                var first = save.Cars.Keys.First();
                type = first.CarType;
                index = first.CarIndex;
                target = FindCarAssetByType(type);
            }
        }

        if (target != null)
        {
            // Important: pass the *SaveData key string* for carType so ownership lookup works
            DisplayCar(target, type, index, false);
        }
        else
        {
            Debug.LogWarning("ExitToGarage: No owned car found to display.");
        }

        // Reset to default shop menu
        shopMenuScript.ResetAllUI();
        garageUIscript.ChangeCar(0);
    }

    // Reset all UI elements to their default states.
    private void ResetUIElements()
    {
        // Reset car switching buttons
        leftButton.SetActive(true);
        rightButton.SetActive(true);

        // Reset loot crate UI
        lootCratePopUps.SetActive(false);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(false);
        addButton.SetActive(true);
        replaceButton.SetActive(false);

        // Re-enable UI elements that were disabled during lootcrate spin
        nitroObject.SetActive(true);
        backButton.SetActive(true);
        goRaceButton.SetActive(true);

        // Re-enable the car name at the bottom of the screen
        carName.gameObject.SetActive(true);
    }


    /*------------------------------------------------------------------------------------------------*/
    /*--------------------------------------- HELPER FUNCTIONS ---------------------------------------*/
    /*------------------------------------------------------------------------------------------------*/

    /// <summary>
    /// Tries to locate the active model root and key sub-nodes on the currently displayed car.
    /// Uses the spawned (lootbox) instance if available; otherwise falls back to the Car asset's prefab.
    /// </summary>
    private bool TryGetModelRoots(out Transform modelRoot, out Transform body, out Transform perfRoot)
    {
        // Prefer the spawned lootbox model if it exists, otherwise use the prefab attached to the Car asset.
        modelRoot = (_spawnedModel != null) ? _spawnedModel.transform : currentCar?.carModel?.transform;

        body = null;     // Initialize out parameter
        perfRoot = null; // Initialize out parameter

        if (modelRoot == null) return false; // No model root found → cannot proceed.

        // Look for the "BODY" node (required part of hierarchy).
        body = modelRoot.Find("BODY");
        if (body == null) return false;

        // Attempt to locate optional PERFORMANCE_PARTS under BODY.
        var perf = body.Find("PERFORMANCE_PARTS");
        perfRoot = perf != null ? perf : null;

        return true; // Both root and BODY are valid
    }

    /// <summary>
    /// Returns the index of the currently active (enabled) part in a PartHolder.
    /// Falls back to index 0 if none are active or if the holder is invalid.
    /// </summary>
    private static int ActiveIndex(PartHolder holder)
    {
        if (holder == null) return 0; // Null holder → safe fallback index.

        var parts = holder.GetPartArray(); // Retrieve all parts managed by this holder.
        if (parts == null || parts.Length == 0) return 0; // Nothing in the array → default to 0.

        // Iterate through parts and return index of the first active GameObject.
        for (int i = 0; i < parts.Length; i++)
            if (parts[i] != null && parts[i].gameObject.activeSelf) return i;

        return 0; // None active → fallback to 0.
    }

    /// <summary>
    /// Records the active part index into SaveData.CarData for a given slot,
    /// and marks that index as "owned" by the player.
    /// </summary>
    private static void SaveSlot(SaveData.CarData data, int slot, PartHolder holder)
    {
        if (holder == null) return; // Skip invalid holder.

        int idx = ActiveIndex(holder); // Determine which part is active.
        data.CarParts[slot].CurrentInstalledPart = idx; // Record as the installed part.

        // Ensure Ownership dictionary exists for this slot.
        if (data.CarParts[slot].Ownership == null)
            data.CarParts[slot].Ownership = new Dictionary<int, bool>();

        // Mark this part index as owned.
        data.CarParts[slot].Ownership[idx] = true;
    }

    /// <summary>
    /// Copies all paint and light colors from a Car's materials into CarData.
    /// Light slots are treated specially (only emission is stored).
    /// </summary>
    private static void CopyColorsFromCar(SaveData.CarData data, Car src)
    {
        if (src == null) return;

        // Local helper: copy a single Material's values into a given CarData color slot.
        void PutColor(int idx, Material m, bool isLight)
        {
            if (m == null) return; // Skip if no material applied.

            var cd = data.Colors[idx]; // Target slot in SaveData.

            // Pull values from material if property exists, otherwise fall back.
            Color baseOrBlack = m.HasProperty("_Color") ? m.color : Color.black;
            Color f1 = m.HasProperty("_FresnelColor") ? m.GetColor("_FresnelColor") : baseOrBlack;
            Color f2 = m.HasProperty("_FresnelColor2") ? m.GetColor("_FresnelColor2") : baseOrBlack;
            Color em = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float met = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;

            if (isLight)
            {
                // For lights: ignore base/fresnel and store only emission + metallic.
                cd.BaseColor[0] = 0; cd.BaseColor[1] = 0; cd.BaseColor[2] = 0; cd.BaseColor[3] = 1;
                cd.EmissionColor[0] = em.r; cd.EmissionColor[1] = em.g; cd.EmissionColor[2] = em.b; cd.EmissionColor[3] = em.a;
                cd.FresnelColor[0] = cd.FresnelColor[1] = cd.FresnelColor[2] = cd.FresnelColor[3] = 0;
                cd.FresnelColor2[0] = cd.FresnelColor2[1] = cd.FresnelColor2[2] = cd.FresnelColor2[3] = 0;
                cd.MetallicMap = met;
            }
            else
            {
                // For opaque body/rim materials: capture all main properties.
                cd.BaseColor[0] = baseOrBlack.r; cd.BaseColor[1] = baseOrBlack.g; cd.BaseColor[2] = baseOrBlack.b; cd.BaseColor[3] = baseOrBlack.a;
                cd.FresnelColor[0] = f1.r; cd.FresnelColor[1] = f1.g; cd.FresnelColor[2] = f1.b; cd.FresnelColor[3] = f1.a;
                cd.FresnelColor2[0] = f2.r; cd.FresnelColor2[1] = f2.g; cd.FresnelColor2[2] = f2.b; cd.FresnelColor2[3] = f2.a;
                cd.EmissionColor[0] = em.r; cd.EmissionColor[1] = em.g; cd.EmissionColor[2] = em.b; cd.EmissionColor[3] = em.a;
                cd.MetallicMap = met;
            }
        }

        // Apply helper to all car color slots.
        PutColor((int)Car.ColorType.PRIMARY_COLOR, src.primColor, false);
        PutColor((int)Car.ColorType.SECONDARY_COLOR, src.secondColor, false);
        PutColor((int)Car.ColorType.RIM_COLOR, src.rimColor, false);
        PutColor((int)Car.ColorType.PRIMARY_LIGHT, src.primLight, true);
        PutColor((int)Car.ColorType.SECONDARY_LIGHT, src.secondLight, true);
        PutColor((int)Car.ColorType.TAIL_LIGHT, src.tailLight, true);
    }

    /// <summary>
    /// Creates a CarData snapshot from the currently displayed lootbox model.
    /// Walks hierarchy and saves parts + colors.
    /// </summary>
    private SaveData.CarData BuildCarDataFromDisplayedModel()
    {
        var data = new SaveData.CarData(); // Start with a fresh CarData container.

        // Ensure model hierarchy is available (root + BODY).
        if (!TryGetModelRoots(out var modelRoot, out var body, out var perf))
        {
            Debug.LogWarning("BuildCarDataFromDisplayedModel: Model roots not found.");
            return data;
        }

        // Record all cosmetic parts (slots 0–7).
        SaveSlot(data, 0, body.Find("EXHAUSTS")?.GetComponent<PartHolder>());
        SaveSlot(data, 1, body.Find("FRONT_SPLITTERS")?.GetComponent<PartHolder>());
        SaveSlot(data, 2, modelRoot.Find("FRONT_WHEELS")?.GetComponent<PartHolder>());
        SaveSlot(data, 3, body.Find("REAR_SPLITTERS")?.GetComponent<PartHolder>());
        SaveSlot(data, 4, modelRoot.Find("REAR_WHEELS")?.GetComponent<PartHolder>());
        SaveSlot(data, 5, body.Find("SIDESKIRTS")?.GetComponent<PartHolder>());
        SaveSlot(data, 6, body.Find("SPOILERS")?.GetComponent<PartHolder>());
        SaveSlot(data, 7, body.GetComponent<PartHolder>()); // Suspensions holder is directly on BODY.

        // Record performance parts if PERFORMANCE_PARTS node exists (slots 8–10).
        SaveSlot(data, 8, perf ? perf.Find("ENGINE")?.GetComponent<PartHolder>() : null);
        SaveSlot(data, 9, perf ? perf.Find("TRANSMISSION")?.GetComponent<PartHolder>() : null);
        SaveSlot(data, 10, perf ? perf.Find("LIVES")?.GetComponent<PartHolder>() : null);

        // Record decals and livery (slots 11–12).
        SaveSlot(data, 11, body.Find("DECALS")?.GetComponent<PartHolder>());
        SaveSlot(data, 12, body.Find("LIVERIES")?.GetComponent<PartHolder>());

        // Copy paint/light colors from Car’s current materials.
        CopyColorsFromCar(data, currentCar);

        return data;
    }

    /// <summary>
    /// Caches the computed lootbox sell price if not already cached.
    /// Prevents recalculating repeatedly.
    /// </summary>
    private void EnsureLootboxSellPriceCached()
    {
        if (_cachedLootboxSellPrice < 0) // Only compute once
            _cachedLootboxSellPrice = ComputeLootboxSellPrice();
    }

    /// <summary>
    /// Builds a dictionary mapping car display names to their bucket index within CarCollection.
    /// Skips null or empty names. Duplicate names keep their first index.
    /// </summary>
    public void BuildCarTypeNameIndex()
    {
        typeIndexByName.Clear(); // Reset any existing index.

        if (carCollection == null || carCollection.carTypes == null) return;

        // Iterate all car type buckets.
        for (int i = 0; i < carCollection.carTypes.Count; i++)
        {
            var bucket = carCollection.carTypes[i];
            if (bucket.items == null || bucket.items.Count == 0) continue;

            // Iterate all assets in bucket.
            foreach (var so in bucket.items)
            {
                var car = so as Car;
                if (car == null) continue;

                // Use car_name if set, otherwise fallback to Unity object name.
                string name = !string.IsNullOrWhiteSpace(car.car_name) ? car.car_name : car.name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Only add if not already mapped (first occurrence wins).
                if (!typeIndexByName.ContainsKey(name))
                    typeIndexByName[name] = i;
            }
        }

        typeNameIndexBuilt = true; // Mark index as built so it won’t rebuild unnecessarily.
    }

    /// <summary>
    /// Returns the consistent display name for a Car (prefers car_name over object name).
    /// </summary>
    private static string CarDisplayName(Car c) =>
        string.IsNullOrWhiteSpace(c?.car_name) ? c?.name : c.car_name;

    /// <summary>
    /// Finds a Car asset in CarCollection that matches the given type string (display name).
    /// Used to resolve SaveData entries back to prefab assets.
    /// </summary>
    private Car FindCarAssetByType(string typeString)
    {
        if (string.IsNullOrEmpty(typeString) || carCollection == null) return null;

        // Iterate through all buckets and items to find a matching display name.
        foreach (var t in carCollection.carTypes)
        {
            foreach (var item in t.items)
            {
                var car = item as Car;
                if (car == null) continue;

                string name = CarDisplayName(car);
                if (name == typeString) return car; // Match found
            }
        }
        return null; // Not found
    }


    /*------------------------------------------------------------------------------------------------*/
    /*--------------------------------------- LOOTBOX HELPERS ----------------------------------------*/
    /*------------------------------------------------------------------------------------------------*/

    /// <summary>
    /// Spawns a random car (by type bucket) using a fixed, skewed weight distribution,
    /// then displays it in "lootbox" mode (randomized parts/colors).
    /// Assumptions:
    ///  - carCollection and its carTypes/items are populated.
    ///  - Index 99 exists within the chosen bucket (per your fixed pick).
    ///  - DisplayCar will handle lootbox placement and randomization.
    /// </summary>
    private void SpawnWeightedRandomCar()
    {
        // Heavier weights make early indices more likely. Order matters.
        float[] weights = { 40f, 20f, 10f, 8f, 6f, 5f, 4f, 3f, 2f, 1f, 0.5f, 0.25f, 0.2f, 0.05f };

        float total = 0f;                              // Running sum for normalization.
        float[] cum = new float[weights.Length];       // Cumulative array used for inverse CDF sampling.

        // Build cumulative weights: cum[i] = sum(weights[0..i]).
        for (int w = 0; w < weights.Length; w++)
        {
            total += weights[w];                       // Accumulate total weight.
            cum[w] = total;                            // Store cumulative sum at this index.
        }

        // Sample a bucket index according to weights.
        int typeIdx = WeightedPick(cum, total);

        // Guard the index against collection bounds (robust to short carTypes lists).
        typeIdx = Mathf.Clamp(typeIdx, 0, Mathf.Max(0, carCollection.carTypes.Count - 1));

        // Resolve the chosen bucket from the collection.
        var bucket = carCollection.carTypes[typeIdx];

        // Fixed car index within the bucket, per your current design.
        // NOTE: Ensure that '99' exists in all target buckets, or add bounds checks as needed.
        int carIdx = 99;

        // Resolve the Car ScriptableObject at (typeIdx, carIdx).
        var carAsset = bucket.items[carIdx] as Car;
        if (carAsset != null)
        {
            // Use car_name if set, otherwise fallback to the underlying object name.
            string displayType = string.IsNullOrWhiteSpace(carAsset.car_name) ? carAsset.name : carAsset.car_name;

            // Display in lootbox mode (true), which randomizes and places using local offsets.
            DisplayCar(carAsset, displayType, carIdx, true);
        }
        // else: if null, the bucket did not contain a Car at index 99; consider adding a guard if needed.
    }

    /// <summary>
    /// Builds a sequence of delays (in seconds) for the lootbox randomizer loop,
    /// starting faster and slowing toward the end using a power (slowDownBias) easing.
    /// The length equals 'spinCount'.
    /// </summary>
    private List<float> BuildDelaySchedule()
    {
        var delays = new List<float>(spinCount);       // Pre-size list to avoid reallocations.

        for (int i = 0; i < spinCount; i++)
        {
            // Linear progress in [0,1]. When spinCount == 1, clamp to 1 to avoid divide-by-zero.
            float tLin = (spinCount <= 1) ? 1f : (float)i / (spinCount - 1);

            // Apply a power curve to bias toward the end (larger exponent => more end-weighted).
            float tBias = Mathf.Pow(tLin, slowDownBias);

            // Interpolate between startDelay and endDelay by the biased t.
            float delay = Mathf.Lerp(startDelay, endDelay, tBias);

            delays.Add(delay);                         // Append this step’s delay.
        }

        return delays;                                 // Returned list used by the randomize coroutine.
    }

    /// <summary>
    /// Samples an index from a cumulative weight array (inverse transform sampling).
    /// 'cumulative' must be strictly non-decreasing; 'total' is cumulative[^last].
    /// Returns the first index i where r < cumulative[i].
    /// </summary>
    private int WeightedPick(float[] cumulative, float total)
    {
        // Random r in [0, total). Using UnityEngine.Random for deterministic seeding if configured externally.
        float r = Random.value * total;

        // Find the first cumulative bucket that exceeds r.
        for (int i = 0; i < cumulative.Length; i++)
            if (r < cumulative[i]) return i;

        // Fallback: due to floating-point edge cases, return the last index.
        return cumulative.Length - 1;
    }

    /// <summary>
    /// Enables input listening for "quick tap to skip" behavior during the spin.
    /// Resets internal state to ensure a clean capture of the next tap/gesture.
    /// </summary>
    private void BeginSkipListen()
    {
        _listenForSkip = true;     // Enable Update() path that checks for skip input.
        _pressStart = -1f;         // Reset press/tap timer sentinel.
        _skipFingerId = -1;        // Ensure no stale finger is tracked on mobile.
    }

    /// <summary>
    /// Disables input listening for the skip feature and clears any partial state.
    /// Should be called when the spin completes or a skip is handled.
    /// </summary>
    private void EndSkipListen()
    {
        _listenForSkip = false;    // Disable skip input processing in Update().
        _pressStart = -1f;         // Clear timing capture.
        _skipFingerId = -1;        // Clear finger association.
    }
}
