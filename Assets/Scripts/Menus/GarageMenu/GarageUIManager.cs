using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class GarageUIManager : MonoBehaviour
{
    // System variables.
    [Header("System Variables")]
    public GameObject customizationUI;
    public GameObject garageUI;
    private float lerpDuration = 0.1f;
    public Button buttonTemplate;
    private Button[] instantiatedButtons;
    private int currentInstantiatedButtonIndex;
    private bool inItemDisplayMenu;
    private bool isPlayerInAllRimsMenu;
    private bool didPlayerPurchaseNewPart;
    private bool isPlayerInPaintMenu;
    private int startingIndex;
    private int partIndexBought;
    private int rearRims;
    public GarageCamera garageCamera;
    public ScrollViewControllerBottomLayer scrollController;
    public CreditManager creditManager;
    public TextMeshProUGUI nitrocount;
    public MenuSounds menuSounds;

    [Space(10)]
    [Header("UI Popups for buying items")]
    public GameObject popUps;
    public GameObject buyConfirmationPopUp;
    public TextMeshProUGUI buyConfirmationPopUpText;
    public Button buyButton;
    public GameObject notEnoughCreditsPopUp;
    public TextMeshProUGUI notEnoughCreditsPopUpText;
    [Header("UI Popups for buying paint")]
    public GameObject paintPopUps;
    public GameObject paintBuyConfirmationPopUp;
    public TextMeshProUGUI paintBuyConfirmationPopUpText;
    public Button paintBuyButton;
    public GameObject paintNotEnoughCreditsPopUp;
    public TextMeshProUGUI paintNotEnoughCreditsPopUpText;

    // Car variables.
    [Space(10)]
    [Header("Car Related Stuff")]
    [SerializeField] private CarCollection carCollection;
    [SerializeField] private CarDisplay carDisplay;
    private GameObject currentCar;
    private Car car;

    // IMPORTANT: Car type is now a string (car name), not an int index.
    private string currentCarType;
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;

    // Name <-> index mapping for CarCollection access and ordered traversal.
    private Dictionary<string, int> typeIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private List<string> typeOrder = new List<string>();

    // Optional: name-based capability gate (temporary, prefer per-car flags in data)
    private readonly HashSet<string> noEmissiveSecondaryTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ferret", "viking hd"
    };

    // Customization bucket variables.
    [Space(10)]
    [Header("Top Level Customization Buckets")]
    [SerializeField] private GameObject[] customizationBuckets;
    [System.Serializable]
    public class CustomizationSubBucketRow
    {
        public GameObject[] items;
    }
    public CustomizationSubBucketRow[] customizationSubBuckets;
    private int currentBucketIndex = 0;
    private int bucketSubIndex = 0;
    private int lastSubBucket = 0;

    // Cached component arrays.
    private RawImage[][] rawImages;
    private TextMeshProUGUI[][] textMeshPros;
    private Button[][] bucketButtons;
    private Image[][] bucketImages;

    // Customization parts.
    private CarPart[][] carParts;
    private PartHolder suspensionHolder;
    private int currentPartIndex = 0;

    [Space(10)]
    [Header("Left/Right Buttons")]
    public Button leftButton;
    public Button rightButton;

    // Paint Menu variables.
    private Material primaryColor;
    private Material secondaryColor;
    private Material rimColor;
    private Material primaryLight;
    private Material secondaryLight;
    private Material tailLight;

    [Space(10)]
    [Header("Paint Menu Objects / Buttons")]
    public GameObject colors;
    public GameObject metalSpheres;
    public Button buttonPrimaryColor;
    public Button buttonSecondaryColor;
    public Button buttonSwitchPaintTypeLeft;
    public Button buttonSwitchPaintTypeRight;
    public TextMeshProUGUI paintType;
    public List<GameObject> colorBuckets;
    public Shader matteShader;
    public Shader glossShader;
    public Texture2D[] liveries;

    private Shader activeShader;
    private int currentPaintType;
    private int whichPartToPaint; // 0 = primary_color, 1 = secondary_color, 2 = rim_color, 3 = primary_light, 4 = secondary_light, 5 = tail_light
    private readonly Dictionary<Car.ColorType, SaveData.ColorData> _pendingColors = new Dictionary<Car.ColorType, SaveData.ColorData>();
    private readonly Dictionary<Car.ColorType, (int paintType, int presetIndex)> _pendingPreset = new Dictionary<Car.ColorType, (int paintType, int presetIndex)>();
    private int currentPaintPrice;

    // Vars for metallic paints
    private readonly float nonMetallicVal = 0.304f;
    private readonly float metallicVal = 1f;
    static readonly int ID_Metallic = Shader.PropertyToID("_Metallic");
    static readonly int ID_MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
    static readonly int KW_MetallicMap = Shader.PropertyToID("_METALLICSPECGLOSSMAP"); // keyword string used below

    // Other buttons.
    [Space(10)]
    [Header("Rim Menu Buttons")]
    public Button frontRimButton;
    public Button rearRimButton;
    public Button allRimButton;
    public Button rimColorButton;

    [Space(10)]
    [Header("Lighting Menu Buttons")]
    public Button primaryLightButton;
    public Button secondaryLightButton;
    public Button tailLightButton;

    [Space(10)]
    [Header("FOR TRAILER ONLY")]
    public Button leftCarChange;
    public Button rightCarChange;
    public Button carCustomize;
    public Button[] backButtons;
    private bool inCustomizationMenu;
    public Button primaryColorButton;
    public Button secondaryColorButton;
    public Button rimButton;
    public Button primaryLightColorButton;
    public Button secondaryLightColorButton;
    public Button tailLightColorButton;
    private bool invokedFromUpdate;

    private const string defaultCar = "OWNED_0_0";
    private const string carsOwned = "CARS_OWNED";

    private int counter = 0;

    private void Awake()
    {
        // Build type name <-> index map and ordered traversal list.
        BuildCarTypeNameIndex();

        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Ensure at least one car is always owned.
        if (saveData.Cars.Count == 0)
        {
            SaveData.CarData defaultCarData = new SaveData.CarData();

            // Choose a default type name; if not set previously, use the first type in collection.
            string defaultTypeName = typeOrder.Count > 0 ? typeOrder[0] : "ferret";

            // Add the default car to the Cars dictionary.
            saveData.Cars[(defaultTypeName, 0)] = defaultCarData;

            saveData.CurrentCarType = defaultTypeName;
            saveData.CurrentCarIndex = 0;
            saveData.LastOwnedCarType = defaultTypeName;
            saveData.LastOwnedCarIndex = 0;

            SaveManager.Instance.SaveGame();
        }
        else
        {
            // If CurrentCarType is empty or unknown, normalize to a known one.
            if (string.IsNullOrWhiteSpace(saveData.CurrentCarType) || !typeIndexByName.ContainsKey(saveData.CurrentCarType))
            {
                string fallback = typeOrder.Count > 0 ? typeOrder[0] : "ferret";
                saveData.CurrentCarType = fallback;
                saveData.CurrentCarIndex = 0;
                SaveManager.Instance.SaveGame();
            }
        }

        carParts = new CarPart[13][];
        CacheComponents();
        RevertToLastOwnedCar();
        nitrocount.text = saveData.NitroCount.ToString();
    }

    private void Update()
    {
        // ************************************************* PC KEYBOARD INPUT COMMANDS FOR DEBUGGING ************************************************* //
        if (Input.GetKeyDown(KeyCode.C))
        {
            creditManager.ChangeCredits(1900000);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Cursor.visible = !Cursor.visible; // Toggle the cursor's visibility
        }

        if (isPlayerInPaintMenu) /* 0 = Primary Color, 1 = Secondary Color, 2 = Rim Color, 3 = Primary Light, 4 = Secondary Light, 5 = Tail Lights */
        {
            if (Input.GetKeyUp(KeyCode.B))
            {
                if (whichPartToPaint < 2)
                    backButtons[3].onClick.Invoke();
                else if (whichPartToPaint == 2)
                    backButtons[2].onClick.Invoke();
                else
                    backButtons[4].onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) && whichPartToPaint < 2)
            {
                buttonSwitchPaintTypeLeft.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) && whichPartToPaint < 2)
            {
                buttonSwitchPaintTypeRight.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                invokedFromUpdate = true;
                if (whichPartToPaint == 0)
                {
                    primaryColorButton.onClick.Invoke();
                    SetColor(primaryColorButton);
                }

                else if (whichPartToPaint == 1)
                {
                    secondaryColorButton.onClick.Invoke();
                    SetColor(secondaryColorButton);
                }

                else if (whichPartToPaint == 2)
                {
                    rimButton.onClick.Invoke();
                    SetColor(rimButton);
                }

                else if (whichPartToPaint == 3)
                {
                    primaryLightColorButton.onClick.Invoke();
                    SetColor(primaryLightColorButton);
                }

                else if (whichPartToPaint == 4)
                {
                    secondaryLightColorButton.onClick.Invoke();
                    SetColor(secondaryLightColorButton);
                }

                else
                {
                    tailLightColorButton.onClick.Invoke();
                    SetColor(tailLightColorButton);
                }
                invokedFromUpdate = false;
            }
        }

        if (inCustomizationMenu) return;

        if (!isPlayerInPaintMenu)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                carCustomize.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                leftCarChange.onClick.Invoke();
            }

            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rightCarChange.onClick.Invoke();
            }
        }
    }   // ******************************************************************************************************************************************** //

    private void BuildCarTypeNameIndex()
    {
        typeIndexByName.Clear();
        typeOrder.Clear();

        for (int i = 0; i < carCollection.carTypes.Count; i++)
        {
            var bucket = carCollection.carTypes[i];
            if (bucket.items == null || bucket.items.Count == 0)
                continue;

            var first = bucket.items[0] as Car;
            if (first == null)
                throw new InvalidOperationException($"CarCollection.carTypes[{i}] contains a non-Car ScriptableObject.");

            // Preferred: Car exposes a public string TypeName. Fallback to asset name if missing.
            string typeName = !string.IsNullOrWhiteSpace(first.car_name)
                ? first.car_name
                : first.name; // fallback

            if (string.IsNullOrWhiteSpace(typeName))
                throw new InvalidOperationException($"Car at index {i} has empty type name.");

            if (typeIndexByName.ContainsKey(typeName))
                throw new InvalidOperationException($"Duplicate car type name '{typeName}' detected in CarCollection.");

            typeIndexByName[typeName] = i;
            typeOrder.Add(typeName);
        }
    }

    private int GetCarTypeIndex(string typeName)
    {
        if (!typeIndexByName.TryGetValue(typeName, out var idx))
            throw new KeyNotFoundException($"Unknown car type '{typeName}'. Ensure it exists in CarCollection.");
        return idx;
    }

    private CarCollection.CarType GetCarTypeBucket(string typeName)
    {
        return carCollection.carTypes[GetCarTypeIndex(typeName)];
    }

    private int GetTypeOrderIndex(string typeName)
    {
        int t = typeOrder.IndexOf(typeName);
        if (t < 0) throw new KeyNotFoundException($"Type '{typeName}' not found in type order.");
        return t;
    }

    private string GetNextType(string current)
    {
        int i = GetTypeOrderIndex(current);
        return typeOrder[(i + 1) % typeOrder.Count];
    }

    private string GetPrevType(string current)
    {
        int i = GetTypeOrderIndex(current);
        return typeOrder[(i - 1 + typeOrder.Count) % typeOrder.Count];
    }

    private void CacheComponents()
    {
        rawImages = new RawImage[customizationBuckets.Length][];
        textMeshPros = new TextMeshProUGUI[customizationBuckets.Length][];
        bucketButtons = new Button[customizationBuckets.Length][];
        bucketImages = new Image[customizationBuckets.Length][];

        for (int i = 0; i < customizationBuckets.Length; i++)
        {
            int subArrayLength = customizationSubBuckets[i].items.Length;

            rawImages[i] = new RawImage[subArrayLength];
            textMeshPros[i] = new TextMeshProUGUI[subArrayLength];
            bucketButtons[i] = new Button[subArrayLength];
            bucketImages[i] = new Image[subArrayLength];

            for (int j = 0; j < subArrayLength; j++)
            {
                rawImages[i][j] = customizationSubBuckets[i].items[j].GetComponentInChildren<RawImage>();
                textMeshPros[i][j] = customizationSubBuckets[i].items[j].GetComponentInChildren<TextMeshProUGUI>();
                bucketButtons[i][j] = customizationSubBuckets[i].items[j].GetComponent<Button>();
                bucketImages[i][j] = customizationSubBuckets[i].items[j].GetComponent<Image>();
            }
        }
    }

    /*------------------------------------- GARAGE UI FUNCTIONS -------------------------------------*/
    public void ChangeCar(int change)
    {
        var saveData = SaveManager.Instance.SaveData;

        // Load current selection
        currentCarType = saveData.CurrentCarType;
        currentCarIndex = saveData.CurrentCarIndex;

        // Safety: if type unknown, reset to first available.
        if (string.IsNullOrWhiteSpace(currentCarType) || !typeIndexByName.ContainsKey(currentCarType))
        {
            currentCarType = typeOrder.Count > 0 ? typeOrder[0] : currentCarType;
            currentCarIndex = 0;
        }

        // Apply index change within the same type
        currentCarIndex += change;

        // Owned copies of current type
        numOfThisCarTypeOwned = SaveManager.Instance.SaveData.Cars.Count(kv => kv.Key.CarType == currentCarType);

        // Variations count available in collection
        var typeBucket = GetCarTypeBucket(currentCarType);
        int maxVariations = typeBucket.items.Count;

        if (currentCarIndex < 0)
        {
            // Move to previous type
            currentCarType = GetPrevType(currentCarType);
            typeBucket = GetCarTypeBucket(currentCarType);
            numOfThisCarTypeOwned = SaveManager.Instance.SaveData.Cars.Count(kv => kv.Key.CarType == currentCarType);
            maxVariations = typeBucket.items.Count;

            currentCarIndex = Mathf.Min(Mathf.Max(numOfThisCarTypeOwned - 1, 0), Mathf.Max(maxVariations - 1, 0));
        }
        else if (currentCarIndex > Mathf.Min(numOfThisCarTypeOwned - 1, maxVariations - 1))
        {
            // Move to next type
            currentCarType = GetNextType(currentCarType);
            typeBucket = GetCarTypeBucket(currentCarType);
            numOfThisCarTypeOwned = SaveManager.Instance.SaveData.Cars.Count(kv => kv.Key.CarType == currentCarType);
            maxVariations = typeBucket.items.Count;

            currentCarIndex = 0;
        }

        // Persist current selection
        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        // Grab the car object
        car = (Car)typeBucket.items[currentCarIndex];

        // Save the last owned car index if this car is owned.
        bool isOwned = saveData.Cars.ContainsKey((currentCarType, currentCarIndex));
        if (isOwned)
        {
            saveData.LastOwnedCarType = currentCarType;
            saveData.LastOwnedCarIndex = currentCarIndex;
        }
        SaveManager.Instance.SaveGame();

        if (carDisplay != null)
        {
            // Ensure CarDisplay supports string carType. If not, map to index via GetCarTypeIndex(currentCarType).
            currentCar = carDisplay.DisplayCar(car, currentCarType, currentCarIndex, false);
        }

        Transform carTransform = currentCar.transform.Find("BODY").transform;

        carParts[0] = carTransform.Find("EXHAUSTS").GetComponent<PartHolder>().GetPartArray();
        carParts[1] = carTransform.Find("FRONT_SPLITTERS").GetComponent<PartHolder>().GetPartArray();
        carParts[2] = currentCar.transform.Find("FRONT_WHEELS").GetComponent<PartHolder>().GetPartArray();
        carParts[3] = carTransform.Find("REAR_SPLITTERS").GetComponent<PartHolder>().GetPartArray();
        carParts[4] = currentCar.transform.Find("REAR_WHEELS").GetComponent<PartHolder>().GetPartArray();
        carParts[5] = carTransform.Find("SIDESKIRTS").GetComponent<PartHolder>().GetPartArray();
        carParts[6] = carTransform.Find("SPOILERS").GetComponent<PartHolder>().GetPartArray();

        suspensionHolder = currentCar.transform.Find("BODY").GetComponent<PartHolder>();
        carParts[7] = suspensionHolder.GetPartArray(); // The suspension customization parts are attached to the main body.

        Transform performanceParts = carTransform.Find("PERFORMANCE_PARTS").transform;
        carParts[8] = performanceParts.Find("ENGINE").GetComponent<PartHolder>().GetPartArray();
        carParts[9] = performanceParts.Find("TRANSMISSION").GetComponent<PartHolder>().GetPartArray();
        carParts[10] = performanceParts.Find("LIVES").GetComponent<PartHolder>().GetPartArray();
        carParts[11] = carTransform.Find("DECALS").GetComponent<PartHolder>().GetPartArray();
        carParts[12] = carTransform.Find("LIVERIES").GetComponent<PartHolder>().GetPartArray();

        primaryColor = currentCar.GetComponent<CarPart>().primaryColor;
        secondaryColor = currentCar.GetComponent<CarPart>().secondaryColor;
        rimColor = currentCar.GetComponent<CarPart>().rimColor;
        primaryLight = currentCar.GetComponent<CarPart>().primaryLight;
        secondaryLight = currentCar.GetComponent<CarPart>().secondaryLight;
        tailLight = currentCar.GetComponent<CarPart>().tailLight;

        // Calculate performance part prices
        int carPrice = car.price;

        // ENGINE (carParts[8])
        carParts[8][0].SetPrice(0);
        carParts[8][1].SetPrice(carPrice / 4);
        carParts[8][2].SetPrice(carPrice);

        // TRANSMISSION (carParts[9])
        carParts[9][0].SetPrice(0);
        carParts[9][1].SetPrice(carPrice / 8);
        carParts[9][2].SetPrice(Mathf.RoundToInt(carPrice * 0.6f)); // 60% of car price

        // LIVES (carParts[10])
        carParts[10][0].SetPrice(0);
        carParts[10][1].SetPrice(carPrice / 2);
        carParts[10][2].SetPrice(carPrice * 2);

        // Sort each row by price
        for (int i = 0; i < carParts.Length; i++)
        {
            Array.Sort(carParts[i], (part1, part2) => part1.price.CompareTo(part2.price));
        }

        UpdatePerformanceStats();

        // Add chrome display function over here.
    }

    // Revert the current car index back to the last car the player owns.
    public void RevertToLastOwnedCar()
    {
        var saveData = SaveManager.Instance.SaveData;

        currentCarType = saveData.LastOwnedCarType;
        currentCarIndex = saveData.LastOwnedCarIndex;

        if (string.IsNullOrWhiteSpace(currentCarType) || !typeIndexByName.ContainsKey(currentCarType))
        {
            currentCarType = typeOrder.Count > 0 ? typeOrder[0] : currentCarType;
            currentCarIndex = 0;
        }

        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        SaveManager.Instance.SaveGame();

        int oldCurrentCarIndex = currentCarIndex;
        currentCarIndex = 0;
        ChangeCar(oldCurrentCarIndex);
        currentCarIndex = oldCurrentCarIndex;
    }

    public void SetLastOwnedCar()
    {
        var saveData = SaveManager.Instance.SaveData;

        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        saveData.LastOwnedCarType = currentCarType;
        saveData.LastOwnedCarIndex = currentCarIndex;

        SaveManager.Instance.SaveGame();
    }

    // Calculate the sell price of the car. (Sell price = car price / 2 + value of installed parts / 4).
    public int GetSellPrice()
    {
        var saveData = SaveManager.Instance.SaveData;

        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        int sellPrice = car.price / 2;

        for (int i = 0; i < carParts.Length; i++)
        {
            currentPartIndex = carData.CarParts[i].CurrentInstalledPart;
            if (currentPartIndex == -1) currentPartIndex = GetDefaultPartIndex(i);

            bool isDefaultPart = i switch
            {
                0 => carParts[i][currentPartIndex].name == car.DefaultExhaust,      // Exhaust
                2 => carParts[i][currentPartIndex].name == car.DefaultFrontWheels,  // Front Wheels
                3 => carParts[i][currentPartIndex].name == car.DefaultRearSplitter, // Rear Splitter
                4 => carParts[i][currentPartIndex].name == car.DefaultRearWheels,   // Rear Wheels
                6 => carParts[i][currentPartIndex].name == car.DefaultSpoiler,      // Spoiler
                _ => false
            };

            if (!isDefaultPart)
            {
                sellPrice += (int)carParts[i][currentPartIndex].price / 4;
            }
        }
        return sellPrice;
    }

    // Remove ownership of all parts. Used when selling the car.
    public void ClearOwnedParts(int carIndex)
    {
        var saveData = SaveManager.Instance.SaveData;

        for (int i = 0; i < carParts.Length; i++)
        {
            bool foundDefaultPart = false;
            for (int j = 0; j < carParts[i].Length; j++)
            {
                switch (i)
                {
                    case 0:
                        if (carParts[i][j].name == car.DefaultExhaust) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    case 2:
                        if (carParts[i][j].name == car.DefaultFrontWheels) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    case 3:
                        if (carParts[i][j].name == car.DefaultRearSplitter) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    case 4:
                        if (carParts[i][j].name == car.DefaultRearWheels) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    case 6:
                        if (carParts[i][j].name == car.DefaultSpoiler) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    default:
                        carParts[i][j].gameObject.SetActive(false);
                        break;
                }
            }
            if (!foundDefaultPart)
            {
                carParts[i][0].gameObject.SetActive(true);
            }
        }

        car.SetDefaultColors();
        car.ApplyLivery(0);

        saveData.Cars.Remove((currentCarType, carIndex));
        SaveManager.Instance.SaveGame();
    }

    // Shift indices after removing a car copy of a given type (string).
    public void AdjustCarIndices(string carTypeName, int removedCarIndex)
    {
        var saveData = SaveManager.Instance.SaveData;

        var keysToShift = saveData.Cars.Keys
            .Where(key => key.CarType == carTypeName && key.CarIndex > removedCarIndex)
            .OrderBy(key => key.CarIndex)
            .ToList();

        foreach (var key in keysToShift)
        {
            SaveData.CarData carData = saveData.Cars[key];
            saveData.Cars.Remove(key);
            var newKey = (key.CarType, key.CarIndex - 1);
            saveData.Cars[newKey] = carData;
        }

        SaveManager.Instance.SaveGame();
    }

    // Enter customization menu.
    public void EnterCustomizationMenu(bool entering)
    {
        if (entering)
        {
            customizationUI.SetActive(true);
            garageUI.SetActive(false);
            inCustomizationMenu = true;
        }
        else
        {
            ResetBuckets();
            currentBucketIndex = 0;
            bucketSubIndex = 0;
            customizationUI.SetActive(false);
            garageUI.SetActive(true);
            inCustomizationMenu = false;
        }
    }

    /*------------------------------------- CUSTOMIZATION UI FUNCTIONS -------------------------------------*/
    public void EnterCustomizationBucket(int bucketIndex)
    {
        if (!inItemDisplayMenu)
        {
            lastSubBucket = bucketSubIndex;
            currentBucketIndex = bucketIndex;
            bucketSubIndex = 0;
            for (int i = 0; i < customizationBuckets.Length; i++)
            {
                if (i == currentBucketIndex) customizationBuckets[i].SetActive(true);
                else customizationBuckets[i].SetActive(false);
            }
        }
    }

    public void DisplayItemTypes(int partIndex)
    {
        inItemDisplayMenu = true;
        currentPartIndex = partIndex;

        bucketButtons[currentBucketIndex][bucketSubIndex].interactable = false;

        for (int i = 0; i < bucketButtons[currentBucketIndex].Length; i++)
        {
            AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 0f);
        }

        var saveData = SaveManager.Instance.SaveData;

        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        startingIndex = carData.CarParts[currentPartIndex].CurrentInstalledPart;
        if (startingIndex == -1) startingIndex = GetDefaultPartIndex(currentPartIndex);
        currentInstantiatedButtonIndex = startingIndex;

        if (currentPartIndex < 8)
            garageCamera.SetCameraPosition(currentPartIndex + 1);

        if (isPlayerInAllRimsMenu)
        {
            rearRims = carData.CarParts[4].CurrentInstalledPart;
            if (rearRims == -1) rearRims = GetDefaultPartIndex(4);
            else carParts[4][rearRims].gameObject.SetActive(false);

            carParts[4][startingIndex].gameObject.SetActive(true);
        }

        int numOfParts = carParts[partIndex].Length;
        instantiatedButtons = new Button[numOfParts];

        scrollController.scrollRect.gameObject.SetActive(true);
        Transform spawnTransform = scrollController.scrollRect.content.transform;

        for (int i = 0; i < numOfParts; i++)
        {
            float alphaValue = 10;
            bool interactable = false;
            Button newButton = Instantiate(buttonTemplate, Vector3.zero, Quaternion.identity, spawnTransform);

            bool isPartOwned = carData.CarParts[currentPartIndex].Ownership.TryGetValue(i, out bool owned) && owned;

            int tempIndex = i;
            newButton.onClick.AddListener(() => ConfirmBuyPart(tempIndex, partIndex));

            TextMeshProUGUI[] texts = newButton.GetComponentsInChildren<TextMeshProUGUI>();
            Image image = newButton.GetComponent<Image>();
            texts[0].color = new Color(texts[0].color.r, texts[0].color.g, texts[0].color.b, alphaValue / 255f);
            texts[1].color = new Color(texts[0].color.r, texts[0].color.g, texts[0].color.b, alphaValue / 255f);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alphaValue / 255f);

            texts[0].text = carParts[partIndex][i].name;

            // If performance part, set button text to peformance effect of part. TODO: Move this elsewhere, each button text should be price only.
            //if (partIndex == 8)
            //    texts[1].text = "+" + System.Math.Round((513.57616f * car.defaultAccelMaxValue * carParts[partIndex][i].accelMaxValueUpgrade - 608.44812f) - (513.57616f * car.defaultAccelMaxValue - 608.44812f)).ToString() + " HP";
            //else if (partIndex == 9)
            //    texts[1].text = "0-60: -" + System.Math.Round((Mathf.Max(-12.28856f * car.defaultAccelIncreaseRate + 23.2393f, -5.484f * car.defaultAccelIncreaseRate + 12.068f) - Mathf.Max(-12.28856f * car.defaultAccelIncreaseRate * carParts[partIndex][i].accelIncreaseRateUpgrade + 23.2393f, -5.484f * car.defaultAccelIncreaseRate * carParts[partIndex][i].accelIncreaseRateUpgrade + 12.068f)), 1).ToString() + "s";
            //else if (partIndex == 10)
            //    texts[1].text = "+" + (carParts[partIndex][i].maxLives - car.defaultNumLives) + " lives";
            //else
            {
                if (i == startingIndex)
                    texts[1].text = "INSTALLED";
                else if (isPartOwned)
                    texts[1].text = "OWNED";
                else
                    texts[1].text = carParts[partIndex][i].price.ToString("F0") + " cr";
            }

            newButton.interactable = interactable;
            newButton.gameObject.SetActive(true);
            instantiatedButtons[i] = newButton;

            scrollController.buttons.Add(newButton);
            scrollController.buttonTransforms.Add(newButton.transform);
            scrollController.buttonImages.Add(image);
            scrollController.buttonNames.Add(texts[0]);
            scrollController.buttonPrices.Add(texts[1]);
            scrollController.backButton = backButtons[currentBucketIndex - 1];
        }

        scrollController.Initialize();
        scrollController.SetScrollPosition(startingIndex);
    }

    public void ChangeItem(int change)
    {
        int oldInstantiatedButtonIndex = currentInstantiatedButtonIndex;
        currentInstantiatedButtonIndex = change;

        if (currentInstantiatedButtonIndex < 0 || currentInstantiatedButtonIndex > instantiatedButtons.Length - 1)
        {
            currentInstantiatedButtonIndex = Mathf.Clamp(currentInstantiatedButtonIndex, 0, instantiatedButtons.Length - 1);
            return;
        }

        carParts[currentPartIndex][oldInstantiatedButtonIndex].gameObject.SetActive(false);
        carParts[currentPartIndex][currentInstantiatedButtonIndex].gameObject.SetActive(true);

        if (isPlayerInAllRimsMenu)
        {
            carParts[4][oldInstantiatedButtonIndex].gameObject.SetActive(false);
            carParts[4][currentInstantiatedButtonIndex].gameObject.SetActive(true);
        }

        if (currentPartIndex == 7) suspensionHolder.SetSuspensionHeight(currentInstantiatedButtonIndex);

        if (currentPartIndex == 12)
        {
            if (change == 0)
            {
                primaryColor.SetTexture("_LiveryMap", null);
                primaryColor.DisableKeyword("_AKMU_CARPAINT_LIVERY");
                return;
            }
            primaryColor.SetTexture("_LiveryMap", liveries[change]);
            primaryColor.EnableKeyword("_AKMU_CARPAINT_LIVERY");
        }
    }

    private void ConfirmBuyPart(int partType, int partIndex)
    {
        popUps.SetActive(true);
        if (creditManager.GetCredits() < carParts[partIndex][partType].price)
        {
            notEnoughCreditsPopUpText.text = "You  do  not  have  enough  credits  to   purchase  this  part";
            notEnoughCreditsPopUp.SetActive(true);
            buyConfirmationPopUp.SetActive(false);
        }
        else
        {
            buyConfirmationPopUpText.text = "Buy  this  part  for  " + carParts[partIndex][partType].price.ToString("N0") + "  CR?";
            notEnoughCreditsPopUp.SetActive(false);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => BuyPart(partType, partIndex));
            buyConfirmationPopUp.SetActive(true);
        }
    }

    private void BuyPart(int partType, int partIndex)
    {
        var saveData = SaveManager.Instance.SaveData;

        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        int oldIndex = carData.CarParts[partIndex].CurrentInstalledPart;
        if (oldIndex == -1) oldIndex = GetDefaultPartIndex(partIndex);

        carData.CarParts[partIndex].CurrentInstalledPart = partType;

        if (isPlayerInAllRimsMenu)
        {
            carData.CarParts[4].CurrentInstalledPart = partType; // Rear wheels
            rearRims = partType;
        }

        if (partIndex > 7 && partIndex < 11)
        {
            UpdatePerformanceStats();
        }

        startingIndex = partType;

        bool isPartOwned = carData.CarParts[partIndex].Ownership.TryGetValue(partType, out bool owned) && owned;

        if (!isPartOwned)
        {
            carData.CarParts[partIndex].Ownership[partType] = true;
            creditManager.ChangeCredits((int)(-1 * carParts[partIndex][partType].price));
        }

        SaveManager.Instance.SaveGame();

        Debug.Log(counter++ + " | Old part name: " + carParts[partIndex][oldIndex].name + " index: " + oldIndex);
        scrollController.buttonPrices[oldIndex].text = "OWNED";
        scrollController.buttonPrices[partType].text = "INSTALLED";

        notEnoughCreditsPopUp.SetActive(false);
        buyConfirmationPopUp.SetActive(false);
        popUps.SetActive(false);
    }

    public void EnterPreviousBucket()
    {
        if (inItemDisplayMenu)
        {
            garageCamera.SetCameraPosition(0);
            for (int i = 0; i < instantiatedButtons.Length; i++)
            {
                Destroy(instantiatedButtons[i].gameObject);
            }
            bucketButtons[currentBucketIndex][bucketSubIndex].interactable = true;

            for (int i = 0; i < customizationSubBuckets[currentBucketIndex].items.Length; i++)
            {
                if (i == bucketSubIndex)
                    AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 255f);

                else if (i == bucketSubIndex - 1 || i == bucketSubIndex + 1)
                    AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 100f);

                else
                    AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 10f);
            }

            carParts[currentPartIndex][currentInstantiatedButtonIndex].gameObject.SetActive(false);
            carParts[currentPartIndex][startingIndex].gameObject.SetActive(true);

            if (isPlayerInAllRimsMenu)
            {
                carParts[4][currentInstantiatedButtonIndex].gameObject.SetActive(false);
                carParts[4][rearRims].gameObject.SetActive(true);
            }
            else if (currentPartIndex == 7)
            {
                suspensionHolder.SetSuspensionHeight(startingIndex);
            }
            isPlayerInAllRimsMenu = false;
            didPlayerPurchaseNewPart = false;
            inItemDisplayMenu = false;
        }

        else if (isPlayerInPaintMenu)
        {
            colorBuckets[0].SetActive(true);
            colorBuckets[1].SetActive(false);
            colorBuckets[2].SetActive(false);
            colorBuckets[3].SetActive(false);
            colorBuckets[4].SetActive(false);

            activeShader = matteShader;

            if (whichPartToPaint == 0 || whichPartToPaint == 1)
            {
                buttonPrimaryColor.gameObject.SetActive(true);
                buttonSecondaryColor.gameObject.SetActive(true);
            }

            else if (whichPartToPaint == 2)
            {
                frontRimButton.gameObject.SetActive(true);
                rearRimButton.gameObject.SetActive(true);
                allRimButton.gameObject.SetActive(true);
                rimColorButton.gameObject.SetActive(true);
            }

            else
            {
                primaryLightButton.gameObject.SetActive(true);
                secondaryLightButton.gameObject.SetActive(true);
                tailLightButton.gameObject.SetActive(true);
            }

            leftButton.gameObject.SetActive(true);
            rightButton.gameObject.SetActive(true);

            colors.SetActive(false);
            buttonSwitchPaintTypeLeft.gameObject.SetActive(false);
            buttonSwitchPaintTypeRight.gameObject.SetActive(false);
            paintType.text = "MATTE";
            isPlayerInPaintMenu = false;
        }
        else
        {
            ResetBuckets();
            bucketSubIndex = lastSubBucket;
            currentBucketIndex = 0;
            customizationBuckets[0].SetActive(true);
            for (int i = 1; i < customizationBuckets.Length; i++)
            {
                customizationBuckets[i].SetActive(false);
            }
        }
        metalSpheres.SetActive(false);
    }

    private void ResetBuckets()
    {
        for (int i = 0; i < customizationSubBuckets[currentBucketIndex].items.Length; i++)
        {
            if (i == 0)
            {
                customizationSubBuckets[currentBucketIndex].items[i].transform.localPosition = new Vector3(22f, 121f, 0f);
                customizationSubBuckets[currentBucketIndex].items[i].transform.localScale = new Vector3(1.26874959f, 1.26875019f, 1.26874959f);
                AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 255f);
                bucketButtons[currentBucketIndex][i].interactable = true;
            }
            else if (i == 1)
            {
                customizationSubBuckets[currentBucketIndex].items[i].transform.localPosition = new Vector3(404f, 121f, 0f);
                customizationSubBuckets[currentBucketIndex].items[i].transform.localScale = Vector3.one;
                AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 100);
                bucketButtons[currentBucketIndex][i].interactable = false;
            }
            else
            {
                customizationSubBuckets[currentBucketIndex].items[i].transform.localPosition = customizationSubBuckets[currentBucketIndex].items[i - 1].transform.localPosition + new Vector3(338f, 0f, 0f);
                customizationSubBuckets[currentBucketIndex].items[i].transform.localScale = Vector3.one;
                AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 10);
                bucketButtons[currentBucketIndex][i].interactable = false;
            }
        }
    }

    public void SetIsPlayerInAllRimsMenuToTrue()
    {
        isPlayerInAllRimsMenu = true;
    }

    /*----------------------------------- PAINT CUSTOMIZATION FUNCTIONS ------------------------------------*/
    public void DisplayColorsInUI(int part /* 0 = Primary Color, 1 = Secondary Color, 2 = Rim Color, 3 = Primary Light, 4 = Secondary Light, 5 = Tail Lights */)
    {
        whichPartToPaint = part;
        isPlayerInPaintMenu = true;

        if (whichPartToPaint == 0 || whichPartToPaint == 1) // Primary or Secondary color
        {
            currentPaintType = 1;
            buttonPrimaryColor.gameObject.SetActive(false);
            buttonSecondaryColor.gameObject.SetActive(false);
            buttonSwitchPaintTypeLeft.gameObject.SetActive(true);
            buttonSwitchPaintTypeRight.gameObject.SetActive(true);
        }
        else if (whichPartToPaint == 2) // Rim Color
        {
            currentPaintType = 1;
            frontRimButton.gameObject.SetActive(false);
            rearRimButton.gameObject.SetActive(false);
            allRimButton.gameObject.SetActive(false);
            rimColorButton.gameObject.SetActive(false);
            buttonSwitchPaintTypeLeft.gameObject.SetActive(true);
            buttonSwitchPaintTypeRight.gameObject.SetActive(true);
            paintType.text = "GLOSS";
            garageCamera.SetCameraPosition(9);
        }
        else
        {
            currentPaintType = 0;
            primaryLightButton.gameObject.SetActive(false);
            secondaryLightButton.gameObject.SetActive(false);
            tailLightButton.gameObject.SetActive(false);
            paintType.text = "";
        }

        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);

        ChangePaintType(0);
        colors.SetActive(true);

        RestoreCheckmarkFromSave(whichPartToPaint, currentPaintType);
        activeShader = matteShader;
    }

    // Change the type of paint to put on the car by setting 'currentPaintType'
    /* currentPaintType settings: 0 = Lighting, 1 = Gloss, 2 = Pearlescent, 3 = Emissive, 4 = Metal */
    public void ChangePaintType(int change)
    {
        bool supportsEmissiveSecondary = SupportsEmissiveSecondary(currentCarType, car);

        // Determine which color bucket to display depending on which part we are painting
        switch (whichPartToPaint)
        {
            case 0: // Primary colors can be gloss, pearlescent, or metal NOT emissive
            {
                if (change < 0 && currentPaintType == 4) currentPaintType = 2;
                else if (change > 0 && currentPaintType == 2) currentPaintType = 4;
                else currentPaintType += change;
                currentPaintType = Mathf.Clamp(currentPaintType, 1, 4);
                break;
            }

            case 1: // Secondary colors can be gloss, pearlescent, emissive (depending on the car), or metal
            {
                if (supportsEmissiveSecondary)
                {
                    currentPaintType += change;
                    currentPaintType = Mathf.Clamp(currentPaintType, 1, 4);
                }
                else
                {
                    if (change < 0 && currentPaintType == 4) currentPaintType = 2;
                    else if (change > 0 && currentPaintType == 2) currentPaintType = 4;
                    else currentPaintType += change;
                    currentPaintType = Mathf.Clamp(currentPaintType, 1, 4);
                }
                break;
            }
            
            case 2: // Rim Colors can be gloss, emissive, or metal NOT pearlescent
            {
                // Skip pearlescents
                if (change < 0 && currentPaintType == 3) currentPaintType = 1;
                else if (change > 0 && currentPaintType == 1) currentPaintType = 3;

                else currentPaintType += change;
                currentPaintType = Mathf.Clamp(currentPaintType, 1, 4);
                break;
            }
        }

        switch (currentPaintType)
        {
            case 0: // LIGHTING
                colorBuckets[0].SetActive(true);
                colorBuckets[1].SetActive(false);
                colorBuckets[2].SetActive(false);
                colorBuckets[3].SetActive(false);
                colorBuckets[4].SetActive(false);
                metalSpheres.SetActive(false);
                paintType.text = "";
                activeShader = matteShader;
                break;

            case 1: // GLOSS
                colorBuckets[0].SetActive(false);
                colorBuckets[1].SetActive(true);
                colorBuckets[2].SetActive(false);
                colorBuckets[3].SetActive(false);
                colorBuckets[4].SetActive(false);
                metalSpheres.SetActive(false);
                paintType.text = "GLOSS";
                activeShader = glossShader;
                break;

            case 2: // PEARLESCENT
                colorBuckets[0].SetActive(false);
                colorBuckets[1].SetActive(false);
                colorBuckets[2].SetActive(true);
                colorBuckets[3].SetActive(false);
                colorBuckets[4].SetActive(false);
                metalSpheres.SetActive(false);
                paintType.text = "PEARLESCENT";
                activeShader = glossShader;
                break;

            case 3: // EMISSIVE
                colorBuckets[0].SetActive(false);
                colorBuckets[1].SetActive(false);
                colorBuckets[2].SetActive(false);
                colorBuckets[3].SetActive(true);
                colorBuckets[4].SetActive(false);
                metalSpheres.SetActive(false);
                paintType.text = "EMISSIVE";
                activeShader = matteShader;
                break;

            case 4: // METAL
                colorBuckets[0].SetActive(false);
                colorBuckets[1].SetActive(false);
                colorBuckets[2].SetActive(false);
                colorBuckets[3].SetActive(false);
                colorBuckets[4].SetActive(true);
                metalSpheres.SetActive(true);
                paintType.text = "METALS";
                activeShader = glossShader;
                break;
        }

        RestoreCheckmarkFromSave(whichPartToPaint, currentPaintType);
    }

    private bool SupportsEmissiveSecondary(string typeName, Car carAsset)
    {
        // Preferred: return carAsset.SupportsEmissiveSecondary; // if your Car scriptable exposes this
        // Temporary: name-based exclusion
        return !noEmissiveSecondaryTypes.Contains(typeName);
    }

    // Called when user clicks on a paint button. PREVIEWS the colour only.
    public void SetColor(int paintPrice)
    {
        if (invokedFromUpdate) return;

        Button clickedButton = EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
        if (clickedButton == null)
        {
            Debug.LogWarning("Current selected object is not a button.");
            return;
        }

        // ---- NEW: choose colors depending on paint type ----
        Color topColor, middleColor, bottomColor;
        Color buttonColor = new Color(0, 0, 0);

        bool isMetal = (currentPaintType == 4);
        if (isMetal)
        {
            // Metallic buttons have no images/materials; select by button name
            GetMetallicPreset(clickedButton.name, out topColor, out middleColor, out bottomColor);
            buttonColor = topColor; // for emissive branches we keep black; otherwise use base
        }
        else
        {
            // Existing logic for non-metal buttons (may use Image.material or Image.color)
            Material buttonMaterial = clickedButton.GetComponent<Image>().material;

            if (buttonMaterial && buttonMaterial.HasProperty("_TopColor") &&
                buttonMaterial.HasProperty("_MiddleColor") &&
                buttonMaterial.HasProperty("_BottomColor"))
            {
                topColor = buttonMaterial.GetColor("_TopColor");
                middleColor = buttonMaterial.GetColor("_MiddleColor");
                bottomColor = buttonMaterial.GetColor("_BottomColor");
            }
            else
            {
                buttonColor = topColor = middleColor = bottomColor = clickedButton.GetComponent<Image>().color;
            }
        }

        Car.ColorType colorType = (Car.ColorType)whichPartToPaint;

        // Apply to materials (preview) and cache the pending selection
        var pending = new SaveData.ColorData();

        // ---- Apply preview per part ----
        switch (colorType)
        {
            case Car.ColorType.PRIMARY_COLOR:
                if (isMetal)
                {
                    primaryColor.color = topColor;
                    primaryColor.SetColor("_FresnelColor", middleColor);
                    primaryColor.SetColor("_FresnelColor2", bottomColor);
                    primaryColor.SetColor("_EmissionColor", Color.black);
                    primaryColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(primaryColor, metallicVal);          // << set metallic
                }
                else
                {
                    primaryColor.color = topColor;
                    primaryColor.SetColor("_FresnelColor", middleColor);
                    primaryColor.SetColor("_FresnelColor2", bottomColor);
                    primaryColor.SetColor("_EmissionColor", Color.black);
                    primaryColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(primaryColor, nonMetallicVal);       // << ensure non-metal
                }

                pending.BaseColor = ToArray(topColor);
                pending.FresnelColor = ToArray(middleColor);
                pending.FresnelColor2 = ToArray(bottomColor);
                pending.EmissionColor = ToArray(Color.black);
                pending.MetallicMap = isMetal ? metallicVal : nonMetallicVal;  // << cache for save
                break;

            case Car.ColorType.SECONDARY_COLOR:
                paintPrice /= 4;

                if (currentPaintType == 3) // Emissive
                {
                    secondaryColor.color = Color.black;
                    secondaryColor.SetColor("_FresnelColor", Color.black);
                    secondaryColor.SetColor("_FresnelColor2", Color.black);
                    secondaryColor.SetColor("_EmissionColor", buttonColor);
                    secondaryColor.EnableKeyword("_EMISSION");
                    SetMaterialMetallic(secondaryColor, nonMetallicVal);    // emissive is non-metal

                    pending.BaseColor = ToArray(Color.black);
                    pending.FresnelColor = ToArray(Color.black);
                    pending.FresnelColor2 = ToArray(Color.black);
                    pending.EmissionColor = ToArray(buttonColor);
                    pending.MetallicMap = nonMetallicVal;
                }
                else if (isMetal) // Metallic non-emissive secondary
                {
                    secondaryColor.color = topColor;
                    secondaryColor.SetColor("_FresnelColor", middleColor);
                    secondaryColor.SetColor("_FresnelColor2", bottomColor);
                    secondaryColor.SetColor("_EmissionColor", Color.black);
                    secondaryColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(secondaryColor, metallicVal);

                    pending.BaseColor = ToArray(topColor);
                    pending.FresnelColor = ToArray(middleColor);
                    pending.FresnelColor2 = ToArray(bottomColor);
                    pending.EmissionColor = ToArray(Color.black);
                    pending.MetallicMap = metallicVal;
                }
                else // Non-emissive, non-metal (gloss/pearl)
                {
                    secondaryColor.color = topColor;
                    secondaryColor.SetColor("_FresnelColor", middleColor);
                    secondaryColor.SetColor("_FresnelColor2", bottomColor);
                    secondaryColor.SetColor("_EmissionColor", Color.black);
                    secondaryColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(secondaryColor, nonMetallicVal);

                    pending.BaseColor = ToArray(topColor);
                    pending.FresnelColor = ToArray(middleColor);
                    pending.FresnelColor2 = ToArray(bottomColor);
                    pending.EmissionColor = ToArray(Color.black);
                    pending.MetallicMap = nonMetallicVal;
                }
                break;

            case Car.ColorType.RIM_COLOR:
                paintPrice /= 10;

                if (currentPaintType == 3) // Emissive rims
                {
                    rimColor.color = Color.black;
                    rimColor.SetColor("_EmissionColor", buttonColor);
                    rimColor.EnableKeyword("_EMISSION");
                    SetMaterialMetallic(rimColor, nonMetallicVal);

                    pending.BaseColor = ToArray(Color.black);
                    pending.EmissionColor = ToArray(buttonColor);
                    pending.MetallicMap = nonMetallicVal;
                }
                else if (isMetal) // Metallic rims
                {
                    rimColor.color = topColor;
                    rimColor.SetColor("_EmissionColor", Color.black);
                    rimColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(rimColor, metallicVal);

                    pending.BaseColor = ToArray(topColor);
                    pending.EmissionColor = ToArray(Color.black);
                    pending.MetallicMap = metallicVal;
                }
                else // Non-metal rims
                {
                    rimColor.color = buttonColor;
                    rimColor.SetColor("_EmissionColor", Color.black);
                    rimColor.DisableKeyword("_EMISSION");
                    SetMaterialMetallic(rimColor, nonMetallicVal);

                    pending.BaseColor = ToArray(buttonColor);
                    pending.EmissionColor = ToArray(Color.black);
                    pending.MetallicMap = nonMetallicVal;
                }
                break;

            case Car.ColorType.PRIMARY_LIGHT:
                paintPrice /= 10;
                primaryLight.SetColor("_EmissionColor", buttonColor);
                // Lights are not metallic
                SetMaterialMetallic(primaryLight, nonMetallicVal);

                pending.EmissionColor = ToArray(buttonColor);
                pending.MetallicMap = nonMetallicVal;
                break;

            case Car.ColorType.SECONDARY_LIGHT:
                paintPrice /= 10;
                secondaryLight.SetColor("_EmissionColor", buttonColor);
                SetMaterialMetallic(secondaryLight, nonMetallicVal);

                pending.EmissionColor = ToArray(buttonColor);
                pending.MetallicMap = nonMetallicVal;
                break;

            case Car.ColorType.TAIL_LIGHT:
                paintPrice /= 10;
                tailLight.SetColor("_EmissionColor", buttonColor);
                SetMaterialMetallic(tailLight, nonMetallicVal);

                pending.EmissionColor = ToArray(buttonColor);
                pending.MetallicMap = nonMetallicVal;
                break;

            default:
                Debug.LogError("Invalid part to paint");
                return;
        }

        // Determine which palette button this was.
        int presetIndex = GetPresetIndexInBucket(clickedButton);

        // Cache pending selection
        _pendingColors[(Car.ColorType)whichPartToPaint] = pending;
        _pendingPreset[(Car.ColorType)whichPartToPaint] = (currentPaintType, presetIndex);

        paintNotEnoughCreditsPopUp.SetActive(false);
        paintBuyConfirmationPopUp.SetActive(false);
        paintPopUps.SetActive(true);
        string typeOfPaint = (currentPaintType == 0) ? "lights" : "paint";
        paintBuyConfirmationPopUpText.text = "Buy  " + typeOfPaint + "  for  " + paintPrice.ToString("N0") + "  CR?";
        paintBuyConfirmationPopUp.SetActive(true);

        currentPaintPrice = paintPrice;
    }

    // Persists the color change and adjust player credits OR displays not enough credits message.
    public void BuyColor()
    {
        if (creditManager.GetCredits() < currentPaintPrice)
        {
            paintBuyConfirmationPopUp.SetActive(false);
            string typeOfPaint = (currentPaintType == 0) ? "these  lights" : "this  paint";
            paintNotEnoughCreditsPopUpText.text = "\nYou  do  not  have  enough  credits  to  purchase  " + typeOfPaint + ".  Required:\n" + currentPaintPrice.ToString("N0") + "  CR";
            paintNotEnoughCreditsPopUp.SetActive(true);
        }
        else
        {
            // Verify there is a pending selection for the current part
            Car.ColorType colorType = (Car.ColorType)whichPartToPaint;
            if (!_pendingColors.TryGetValue(colorType, out var pending))
            {
                Debug.LogWarning("No pending color to buy for current part.");
                return;
            }

            SaveData saveData = SaveManager.Instance.SaveData;
            if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
            {
                Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
                return;
            }

            if (currentPaintType == 0) menuSounds.PlayAirWrenchSound();
            else menuSounds.PlaySprayCan();
            creditManager.ChangeCredits(-currentPaintPrice);

            // Persist to save data
            SaveData.ColorData colorData = carData.Colors[whichPartToPaint];

            // Copy fields that were set in preview
            if (pending.BaseColor != null) colorData.BaseColor = pending.BaseColor;
            if (pending.FresnelColor != null) colorData.FresnelColor = pending.FresnelColor;
            if (pending.FresnelColor2 != null) colorData.FresnelColor2 = pending.FresnelColor2;
            if (pending.EmissionColor != null) colorData.EmissionColor = pending.EmissionColor;

            // Save metallic color
            colorData.MetallicMap = pending.MetallicMap;

            if (_pendingPreset.TryGetValue(colorType, out var sel))
            {
                colorData.SelectedPaintType = sel.paintType;
                colorData.SelectedPresetIndex = sel.presetIndex;

                // Immediately reflect in UI for the current bucket
                ApplyCheckmarkForBucket(sel.paintType, sel.presetIndex);
            }

            SaveManager.Instance.SaveGame();

            // Optionally clear the pending entry for this part after purchase
            _pendingColors.Remove(colorType);
            _pendingPreset.Remove(colorType);

            paintPopUps.SetActive(false);
            paintBuyConfirmationPopUp.SetActive(false);
        }
    }

    // Revert car color back to currently saved color
    public void RevertColor()
    {
        if (!SaveManager.Instance.SaveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
            return;
        }

        Car.ColorType colorType = (Car.ColorType)whichPartToPaint;
        if (whichPartToPaint < 0 || whichPartToPaint >= carData.Colors.Length)
        {
            Debug.LogError("Invalid color index for this car.");
            return;
        }

        SaveData.ColorData saved = carData.Colors[whichPartToPaint];

        Color baseColor = FromArray(saved.BaseColor);
        Color fresnelColor = FromArray(saved.FresnelColor);
        Color fresnelColor2 = FromArray(saved.FresnelColor2);
        Color emissionColor = FromArray(saved.EmissionColor);

        switch (colorType)
        {
            case Car.ColorType.PRIMARY_COLOR:
                primaryColor.color = baseColor;
                primaryColor.SetColor("_FresnelColor", fresnelColor);
                primaryColor.SetColor("_FresnelColor2", fresnelColor2);
                SetMaterialMetallic(primaryColor, saved.MetallicMap);
                break;

            case Car.ColorType.SECONDARY_COLOR:
                secondaryColor.color = baseColor;
                secondaryColor.SetColor("_FresnelColor", fresnelColor);
                secondaryColor.SetColor("_FresnelColor2", fresnelColor2);
                secondaryColor.SetColor("_EmissionColor", emissionColor);
                if (emissionColor != Color.black)
                    secondaryColor.EnableKeyword("_EMISSION");
                else
                    secondaryColor.DisableKeyword("_EMISSION");
                SetMaterialMetallic(secondaryColor, saved.MetallicMap);
                break;

            case Car.ColorType.RIM_COLOR:
                rimColor.color = baseColor;
                rimColor.SetColor("_EmissionColor", emissionColor);
                if (emissionColor != Color.black)
                    rimColor.EnableKeyword("_EMISSION");
                else
                    rimColor.DisableKeyword("_EMISSION");
                SetMaterialMetallic(rimColor, saved.MetallicMap);
                break;

            case Car.ColorType.PRIMARY_LIGHT:
                primaryLight.SetColor("_EmissionColor", emissionColor);
                SetMaterialMetallic(primaryLight, saved.MetallicMap);
                break;

            case Car.ColorType.SECONDARY_LIGHT:
                secondaryLight.SetColor("_EmissionColor", emissionColor);
                SetMaterialMetallic(secondaryLight, saved.MetallicMap);
                break;

            case Car.ColorType.TAIL_LIGHT:
                tailLight.SetColor("_EmissionColor", emissionColor);
                SetMaterialMetallic(tailLight, saved.MetallicMap);
                break;

            default:
                Debug.LogError("Invalid part to revert");
                break;
        }

        // Remove pending preview for this part, if any
        _pendingColors.Remove(colorType);
    }

    // Helper function for retrieving metallic colors from button name
    private static void GetMetallicPreset(string buttonName, out Color baseColor, out Color fresnel1, out Color fresnel2)
    {
        switch (buttonName)
        {
            case "Gold":
                // Rich yellow-gold
                baseColor = new Color(1.00f, 0.75f, 0f, 1f);
                fresnel1 = new Color(1.00f, 0.75f, 0f, 1f);
                fresnel2 = new Color(1.00f, 0.75f, 0f, 1f);
                break;

            case "Bronze":
                // Warmer, punchier bronze
                baseColor = new Color(0.98f, 0.61f, 0.33f, 1f);
                fresnel1 = new Color(1.000f, 0.820f, 0.640f, 1f);
                fresnel2 = new Color(0.353f, 0.176f, 0.047f, 1f); 
                break;

            case "Silver":
            default:
                baseColor = Color.white;
                fresnel1 = new Color(0.698f, 0.698f, 0.698f, 1f);
                fresnel2 = Color.white;
                break;
        }
    }


    // Set the metallic property of a material
    private void SetMaterialMetallic(Material m, float value, bool useTexture = false)
    {
        if (m == null) return;

        // If you want to force the slider to drive the look, ensure no metallic map is bound
        if (!useTexture)
        {
            m.SetTexture(ID_MetallicGlossMap, null);
            m.DisableKeyword("_METALLICSPECGLOSSMAP");
        }
        else
        {
            // If you later introduce a metallic texture, assign it and enable the keyword:
            // m.SetTexture(ID_MetallicGlossMap, yourTexture);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        m.SetFloat(ID_Metallic, value);
    }

    private static float[] ToArray(Color c) => new[] { c.r, c.g, c.b, c.a };
    private static Color FromArray(float[] arr)
    {
        if (arr == null || arr.Length < 4) return Color.black;
        return new Color(arr[0], arr[1], arr[2], arr[3]);
    }

    // Get the palette "slot index" within the active bucket.
    // If your bucket contains non-button children, prefer a dedicated component (see Note below).
    private static int GetPresetIndexInBucket(Button b) => b.transform.GetSiblingIndex();

    // Turn on a single child named "Checkmark" under the selected slot; turn off others
    private void ApplyCheckmarkForBucket(int paintType, int presetIndex)
    {
        if (paintType < 0 || paintType >= colorBuckets.Count) return;
        var bucket = colorBuckets[paintType].transform;

        for (int i = 0; i < bucket.childCount; i++)
        {
            var mark = bucket.GetChild(i).Find("Checkmark");
            if (mark != null) mark.gameObject.SetActive(i == presetIndex);
        }
    }

    // Clear all checkmarks in a bucket
    private void ClearCheckmarksForBucket(int paintType)
    {
        if (paintType < 0 || paintType >= colorBuckets.Count) return;
        var bucket = colorBuckets[paintType].transform;

        for (int i = 0; i < bucket.childCount; i++)
        {
            var mark = bucket.GetChild(i).Find("Checkmark");
            if (mark != null) mark.gameObject.SetActive(false);
        }
    }

    // Read saved selection for (PART, PAINT TYPE) and reflect it in UI
    private void RestoreCheckmarkFromSave(int partIndex /*whichPartToPaint*/, int paintType)
    {
        if (!SaveManager.Instance.SaveData.Cars.TryGetValue((currentCarType, currentCarIndex), out var carData))
        {
            ClearCheckmarksForBucket(paintType);
            return;
        }

        var saved = carData.Colors[partIndex];
        if (saved.SelectedPaintType == paintType && saved.SelectedPresetIndex >= 0)
            ApplyCheckmarkForBucket(paintType, saved.SelectedPresetIndex);
        else
            ClearCheckmarksForBucket(paintType);
    }

    // Overloaded DEBUG version of SetColor() that uses a button as input. Only called when pressing Return on a paint button while using a PC keyboard.
    public void SetColor(Button clickedButton)
    {
        if (clickedButton == null)
        {
            Debug.LogWarning("Clicked button is null.");
            return;
        }

        Material buttonMaterial = clickedButton.GetComponent<Image>().material;
        Color topColor, middleColor, bottomColor;
        Color buttonColor = new Color(0, 0, 0);

        if (buttonMaterial && buttonMaterial.HasProperty("_TopColor") && buttonMaterial.HasProperty("_MiddleColor") && buttonMaterial.HasProperty("_BottomColor"))
        {
            topColor = buttonMaterial.GetColor("_TopColor");
            middleColor = buttonMaterial.GetColor("_MiddleColor");
            bottomColor = buttonMaterial.GetColor("_BottomColor");
        }
        else
        {
            buttonColor = topColor = middleColor = bottomColor = clickedButton.GetComponent<Image>().color;
        }

        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
            return;
        }

        SaveData.ColorData colorData = carData.Colors[whichPartToPaint];
        colorData.BaseColor = new float[] { topColor.r, topColor.g, topColor.b, topColor.a };
        if (currentPaintType == 1 || currentPaintType == 2)
        {
            colorData.FresnelColor = new float[] { middleColor.r, middleColor.g, middleColor.b, middleColor.a };
            colorData.FresnelColor2 = new float[] { bottomColor.r, bottomColor.g, bottomColor.b, bottomColor.a };
        }
        else
        {
            colorData.FresnelColor = null;
            colorData.FresnelColor2 = null;
        }

        Car.ColorType colorType = (Car.ColorType)whichPartToPaint;

        switch (colorType)
        {
            case Car.ColorType.PRIMARY_COLOR:
                primaryColor.color = topColor;
                primaryColor.SetColor("_FresnelColor", middleColor);
                primaryColor.SetColor("_FresnelColor2", bottomColor);
                break;
            case Car.ColorType.SECONDARY_COLOR:
                secondaryColor.color = topColor;
                secondaryColor.SetColor("_FresnelColor", middleColor);
                secondaryColor.SetColor("_FresnelColor2", bottomColor);
                if (currentPaintType == 1 || currentPaintType == 2)
                {
                    secondaryColor.SetColor("_FresnelColor", middleColor);
                    secondaryColor.SetColor("_FresnelColor2", bottomColor);
                    secondaryColor.SetColor("_EmissionColor", Color.black);
                    secondaryColor.DisableKeyword("_EMISSION");
                }
                else if (currentPaintType == 3)
                {
                    secondaryColor.color = Color.black;
                    secondaryColor.SetColor("_EmissionColor", buttonColor);
                    secondaryColor.EnableKeyword("_EMISSION");
                }
                break;
            case Car.ColorType.RIM_COLOR:
                rimColor.color = buttonColor;
                if (currentPaintType == 3)
                {
                    rimColor.color = Color.black;
                    rimColor.SetColor("_EmissionColor", buttonColor);
                    rimColor.EnableKeyword("_EMISSION");
                }
                else
                {
                    rimColor.SetColor("_EmissionColor", Color.black);
                    rimColor.DisableKeyword("_EMISSION");
                }
                break;
            case Car.ColorType.PRIMARY_LIGHT:
                primaryLight.SetColor("_EmissionColor", buttonColor);
                break;
            case Car.ColorType.SECONDARY_LIGHT:
                secondaryLight.SetColor("_EmissionColor", buttonColor);
                break;
            case Car.ColorType.TAIL_LIGHT:
                tailLight.SetColor("_EmissionColor", buttonColor);
                break;
            default:
                Debug.LogError("Invalid part to paint");
                return;
        }

        SaveManager.Instance.SaveGame();
    }

    /*----------------------------------------- OTHER FUNCTIONS --------------------------------------------*/
    public void UpdatePerformanceStats()
    {
        var saveData = SaveManager.Instance.SaveData;

        int engineIndex = 0;
        int transmissionIndex = 0;
        int livesIndex = 0;

        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            // Do nothing.
        }
        else
        {
            Debug.Log(engineIndex + " " + carData.CarParts[8].CurrentInstalledPart);
            engineIndex = engineIndex == -1 ? 0 : carData.CarParts[8].CurrentInstalledPart;            // ENGINE
            transmissionIndex = transmissionIndex == -1 ? 0 : carData.CarParts[9].CurrentInstalledPart; // TRANSMISSION
            livesIndex = livesIndex == -1 ? 0 : carData.CarParts[10].CurrentInstalledPart;              // LIVES
        }

        if (engineIndex != -1) car.accelMaxValue = car.defaultAccelMaxValue * carParts[8][engineIndex].accelMaxValueUpgrade;
        if (transmissionIndex != -1) car.accelIncreaseRate = car.defaultAccelIncreaseRate * carParts[9][transmissionIndex].accelIncreaseRateUpgrade;
        if (livesIndex != -1) car.numlives = carParts[10][livesIndex].maxLives;

        carDisplay.UpdateStats(car.accelMaxValue, car.accelIncreaseRate, car.numlives);
    }

    public int GetDefaultPartIndex(int inputPartIndex)
    {
        switch (inputPartIndex)
        {
            // Set the starting index to the car's default exhausts.
            case 0:
                for (int i = 0; i < carParts[inputPartIndex].Length; i++)
                {
                    if (carParts[inputPartIndex][i].name == car.DefaultExhaust) return i;
                }

                break;
            // Set the starting index to the default front wheels.
            case 2:
                for (int i = 0; i < carParts[inputPartIndex].Length; i++)
                {
                    if (carParts[inputPartIndex][i].name == car.DefaultFrontWheels) return i;
                }
                break;
            // Set the starting index to the default rear splitter.
            case 3:
                for (int i = 0; i < carParts[inputPartIndex].Length; i++)
                {
                    if (carParts[inputPartIndex][i].name == car.DefaultRearSplitter) return i;
                }
                break;
            // Set the starting index to the default rear wheels.
            case 4:
                for (int i = 0; i < carParts[inputPartIndex].Length; i++)
                {
                    if (carParts[inputPartIndex][i].name == car.DefaultRearWheels) return i;
                }
                break;
            // Set the starting index to the default spoiler.
            case 6:
                for (int i = 0; i < carParts[inputPartIndex].Length; i++)
                {
                    if (carParts[inputPartIndex][i].name == car.DefaultSpoiler) return i;
                }
                break;
            default:
                return 0;
        }
        return 0;
    }

    // Utility function to adjust alpha values while keeping the original RGB values.
    void AdjustAlpha(RawImage rawImage, TextMeshProUGUI textMesh, Image image, float alpha)
    {
        if (rawImage)
        {
            Color newColor = rawImage.color;
            if (alpha != 255f)
                newColor.a = alpha / 4 / 255f;
            else
                newColor.a = alpha / 255f;
            rawImage.color = newColor;
        }
        if (textMesh)
        {
            Color newColor = textMesh.color;
            newColor.a = alpha / 255f;
            textMesh.color = newColor;
        }
        if (image)
        {
            Color newColor = image.color;
            newColor.a = alpha / 255f;
            image.color = newColor;
        }
    }
}
