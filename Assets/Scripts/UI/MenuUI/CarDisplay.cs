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

    [Header("Prefabs (fill in inspector)")]
    [SerializeField] private GameObject[] carPrefabs; // index aligned with car type index
    private readonly Dictionary<string, int> typeIndexByName = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

    [Header("Custom Objects")]
    public CreditManager creditManager;
    public GarageUIManager garageUIManager;

    [Header("Lock UI image/background")]
    public GameObject lockUiElement;
    public GameObject lockImage;

    [Header("Bottom button sets")]
    public GameObject buttonSet1;
    public GameObject buttonSet2;
    public GameObject leftButton;
    public GameObject rightButton;

    [Header("Buy/sell confirmation popups")]
    public GameObject popUps;
    public GameObject buyConfirmationPopUp;
    public TextMeshProUGUI buyConfirmationPopUpText;
    public GameObject notEnoughCreditsPopUp;
    public TextMeshProUGUI notEnoughCreditsPopUpText;
    public GameObject sellConfirmationPopUp;
    public TextMeshProUGUI sellConfirmationPopUpText;
    public GameObject cannotSellPopUp;
    public TextMeshProUGUI cannotSellPopUpText;

    [Header("Loot crate popups")]
    public GameObject lootCratePopUps;
    public GameObject addOrSellPopUp;
    public TextMeshProUGUI addOrSellPopUpText;
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

    private Car currentCar;
    private GameObject _spawnedModel;
    private string currentCarType;   // switched to string
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;
    private int sellPrice;
    private const string carsOwned = "CARS_OWNED";
    public bool typeNameIndexBuilt = false;

    // -------------------- Loot-box style randomizer --------------------
    private readonly int spinCount = 100;
    private readonly float startDelay = 0.2f;   // fast at start
    private readonly float endDelay = 0.8228f;   // slow at end
    private readonly float slowDownBias = 2f;
    public CarCollection carCollection;
    Coroutine _spinCo;
    [Header("Turntable Spin")]
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


    // Instantiate and display car on turntable in garage.
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


    // Randomize car for lootboxes.
    public void RandomizeCar()
    {
        // Set all buttons and widges to be inactive
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
        _spinCo = StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
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

    // Couroutine that spins the turntable.
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

    public void AddLootboxCarToGarage()
    {
        var saveData = SaveManager.Instance.SaveData;

        // Determine the next index for this car type and create the save record.
        int nextIndex = saveData.Cars.Count(c => c.Key.CarType == currentCarType);
        var newCarKey = (CarType: currentCarType, CarIndex: nextIndex);

        if (!saveData.Cars.TryGetValue(newCarKey, out SaveData.CarData carData))
        {
            carData = new SaveData.CarData();
            saveData.Cars[newCarKey] = carData;
        }

        // ---- Read currently active parts from the displayed (spawned) model ----
        // Fallback to prefab if needed (shouldn't usually be necessary).
        Transform body = _spawnedModel != null
            ? _spawnedModel.transform.Find("BODY")
            : currentCar.carModel.transform.Find("BODY");

        if (body == null)
        {
            Debug.LogWarning("AddLootboxCarToGarage: BODY transform not found.");
            return;
        }

        // Minimal local helpers
        int ActiveIndex(PartHolder h)
        {
            if (h == null) return 0;
            var parts = h.GetPartArray();
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].gameObject.activeSelf) return i;
            return 0;
        }

        void SaveSlot(int slot, PartHolder holder)
        {
            if (holder == null) return;

            int idx = ActiveIndex(holder);
            carData.CarParts[slot].CurrentInstalledPart = idx;

            // Ensure the dictionary exists
            var own = carData.CarParts[slot].Ownership;
            if (own == null)
                carData.CarParts[slot].Ownership = own = new Dictionary<int, bool>();

            // Mark this part as owned (Add or overwrite)
            own[idx] = true;
            // If you prefer the explicit form:
            // if (!own.ContainsKey(idx)) own.Add(idx, true); else own[idx] = true;
        }

        // Cosmetic & aero
        SaveSlot(0, body.Find("EXHAUSTS")?.GetComponent<PartHolder>());
        SaveSlot(1, body.Find("FRONT_SPLITTERS")?.GetComponent<PartHolder>());
        SaveSlot(2, _spawnedModel.transform.Find("FRONT_WHEELS")?.GetComponent<PartHolder>());
        SaveSlot(3, body.Find("REAR_SPLITTERS")?.GetComponent<PartHolder>());
        SaveSlot(4, _spawnedModel.transform.Find("REAR_WHEELS")?.GetComponent<PartHolder>());
        SaveSlot(5, body.Find("SIDESKIRTS")?.GetComponent<PartHolder>());
        SaveSlot(6, body.Find("SPOILERS")?.GetComponent<PartHolder>());
        SaveSlot(7, body.GetComponent<PartHolder>()); // SUSPENSIONS lives on BODY

        // Performance parts
        var perf = body.Find("PERFORMANCE_PARTS");
        SaveSlot(8, perf?.Find("ENGINE")?.GetComponent<PartHolder>());
        SaveSlot(9, perf?.Find("TRANSMISSION")?.GetComponent<PartHolder>());
        SaveSlot(10, perf?.Find("LIVES")?.GetComponent<PartHolder>());

        // Decals & livery
        SaveSlot(11, body.Find("DECALS")?.GetComponent<PartHolder>());
        SaveSlot(12, body.Find("LIVERIES")?.GetComponent<PartHolder>());

        // ---- Copy paint state from the randomized materials on the Car asset ----
        // colorType indices: 0=PRIMARY,1=SECONDARY,2=RIM,3=PRIMARY_LIGHT,4=SECONDARY_LIGHT,5=TAIL_LIGHT

        void PutColor(int idx, Material m, bool isLight)
        {
            if (m == null) return;
            var cd = carData.Colors[idx];

            // Read common channels safely
            Color BaseOrBlack = m.HasProperty("_Color") ? m.color : Color.black;
            Color F1 = m.HasProperty("_FresnelColor") ? m.GetColor("_FresnelColor") : BaseOrBlack;
            Color F2 = m.HasProperty("_FresnelColor2") ? m.GetColor("_FresnelColor2") : BaseOrBlack;
            Color Em = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float met = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;

            if (isLight)
            {
                // Lights store their color in EmissionColor; Base is black.
                cd.BaseColor[0] = 0; cd.BaseColor[1] = 0; cd.BaseColor[2] = 0; cd.BaseColor[3] = 1;
                cd.EmissionColor[0] = Em.r; cd.EmissionColor[1] = Em.g; cd.EmissionColor[2] = Em.b; cd.EmissionColor[3] = Em.a;
                cd.FresnelColor[0] = cd.FresnelColor[1] = cd.FresnelColor[2] = cd.FresnelColor[3] = 0;
                cd.FresnelColor2[0] = cd.FresnelColor2[1] = cd.FresnelColor2[2] = cd.FresnelColor2[3] = 0;
                cd.MetallicMap = met;
            }
            else
            {
                // Body/rims store color in Base; Em used only if you later set emissive rims/secondary.
                cd.BaseColor[0] = BaseOrBlack.r; cd.BaseColor[1] = BaseOrBlack.g; cd.BaseColor[2] = BaseOrBlack.b; cd.BaseColor[3] = BaseOrBlack.a;
                cd.FresnelColor[0] = F1.r; cd.FresnelColor[1] = F1.g; cd.FresnelColor[2] = F1.b; cd.FresnelColor[3] = F1.a;
                cd.FresnelColor2[0] = F2.r; cd.FresnelColor2[1] = F2.g; cd.FresnelColor2[2] = F2.b; cd.FresnelColor2[3] = F2.a;
                cd.EmissionColor[0] = Em.r; cd.EmissionColor[1] = Em.g; cd.EmissionColor[2] = Em.b; cd.EmissionColor[3] = Em.a;
                cd.MetallicMap = met;
            }
        }

        PutColor((int)Car.ColorType.PRIMARY_COLOR, currentCar.primColor, false);
        PutColor((int)Car.ColorType.SECONDARY_COLOR, currentCar.secondColor, false);
        PutColor((int)Car.ColorType.RIM_COLOR, currentCar.rimColor, false);
        PutColor((int)Car.ColorType.PRIMARY_LIGHT, currentCar.primLight, true);
        PutColor((int)Car.ColorType.SECONDARY_LIGHT, currentCar.secondLight, true);
        PutColor((int)Car.ColorType.TAIL_LIGHT, currentCar.tailLight, true);

        // Persist and show confirmation UI
        // NEW: mark the newly added car as the "last owned"
        saveData.LastOwnedCarType = currentCarType;          // NEW
        saveData.LastOwnedCarIndex = nextIndex;              // NEW
                                                             // (Optional) also make it the current selection:
        saveData.CurrentCarType = currentCarType;            // NEW (optional)
        saveData.CurrentCarIndex = nextIndex;                // NEW (optional)

        SaveManager.Instance.SaveGame();

        lootCratePopUps.SetActive(true);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(true);
        returnOrSpinAgainPopUpText.text = $"You have added a <u>{currentCar.car_name}</u> to your garage.";
    }

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


    /// <summary>
    /// Compute sell price for the *currently randomized (lootbox)* car on the turntable:
    /// base = half of car price + 1/4 of each active part price,
    /// skipping default parts for slots: 0,2,3,4,6 (Exhaust, Front/Rear Wheels, Rear Splitter, Spoiler).
    /// </summary>
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

    public void PickOwnedCarToReplaceWithLootboxCar()
    {

    }    
    
    public void ReplaceOwnedCarWithLootboxCar()
    {

    }

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

    private void ResetUIElements()
    {
        leftButton.SetActive(true);
        rightButton.SetActive(true);

        lootCratePopUps.SetActive(false);
        addOrSellPopUp.SetActive(false);
        returnOrSpinAgainPopUp.SetActive(false);

        nitroObject.SetActive(true);
        backButton.SetActive(true);
        goRaceButton.SetActive(true);

        carName.gameObject.SetActive(true);
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

    // Update performance stats text
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
    /*--------------------------------------- HELPER FUNCTIONS ---------------------------------------*/
    /*------------------------------------------------------------------------------------------------*/
    public void BuildCarTypeNameIndex()
    {
        typeIndexByName.Clear();
        if (carCollection == null || carCollection.carTypes == null) return;

        for (int i = 0; i < carCollection.carTypes.Count; i++)
        {
            var bucket = carCollection.carTypes[i];
            if (bucket.items == null || bucket.items.Count == 0) continue;

            foreach (var so in bucket.items)
            {
                var car = so as Car;
                if (car == null) continue;

                string name = !string.IsNullOrWhiteSpace(car.car_name) ? car.car_name : car.name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Map every variant name to the SAME bucket index.
                if (!typeIndexByName.ContainsKey(name))
                    typeIndexByName[name] = i;
            }
        }

        typeNameIndexBuilt = true;
    }

    // Resolve display name consistently
    private static string CarDisplayName(Car c) =>
        string.IsNullOrWhiteSpace(c?.car_name) ? c?.name : c.car_name;

    // Search your CarCollection for a Car asset by its type string saved in SaveData
    private Car FindCarAssetByType(string typeString)
    {
        if (string.IsNullOrEmpty(typeString) || carCollection == null) return null;

        foreach (var t in carCollection.carTypes)
        {
            foreach (var item in t.items)
            {
                var car = item as Car;
                if (car == null) continue;

                // NOTE: SaveData uses a string key; your code was storing the display name for currentCarType
                // so we compare with the same logic the code used when setting currentCarType.
                string name = CarDisplayName(car);
                if (name == typeString) return car;
            }
        }
        return null;
    }

    // Call this wherever you need to spawn a weighted random car once.
    private void SpawnWeightedRandomCar()
    {
        float[] weights = { 40f, 20f, 10f, 8f, 6f, 5f, 4f, 3f, 2f, 1f, 0.5f, 0.25f, 0.2f, 0.05f };
        float total = 0f;
        float[] cum = new float[weights.Length];
        for (int w = 0; w < weights.Length; w++) { total += weights[w]; cum[w] = total; }

        int typeIdx = WeightedPick(cum, total);
        typeIdx = Mathf.Clamp(typeIdx, 0, Mathf.Max(0, carCollection.carTypes.Count - 1));

        var bucket = carCollection.carTypes[typeIdx];
        int carIdx = 99; // your fixed pick

        var carAsset = bucket.items[carIdx] as Car;
        if (carAsset != null)
        {
            DisplayCar(
                carAsset,
                string.IsNullOrWhiteSpace(carAsset.car_name) ? carAsset.name : carAsset.car_name,
                carIdx,
                true
            );
        }
    }

    // Build the exact delay schedule used by the randomizer so spin timing matches 1:1.
    List<float> BuildDelaySchedule()
    {
        var delays = new List<float>(spinCount);
        for (int i = 0; i < spinCount; i++)
        {
            float tLin = (spinCount <= 1) ? 1f : (float)i / (spinCount - 1);
            float tBias = Mathf.Pow(tLin, slowDownBias);
            float delay = Mathf.Lerp(startDelay, endDelay, tBias);
            delays.Add(delay);
        }
        return delays;
    }

    // Map a random number onto the cumulative weights.
    int WeightedPick(float[] cumulative, float total)
    {
        float r = Random.value * total; // [0,total)
        for (int i = 0; i < cumulative.Length; i++)
            if (r < cumulative[i]) return i;
        return cumulative.Length - 1;
    }

    private void BeginSkipListen()
    {
        _listenForSkip = true;
        _pressStart = -1f;
        _skipFingerId = -1;
    }

    private void EndSkipListen()
    {
        _listenForSkip = false;
        _pressStart = -1f;
        _skipFingerId = -1;
    }

}
