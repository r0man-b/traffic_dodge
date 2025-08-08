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

    [Space(10)]
    [Header("UI Popups for buying items")]
    public GameObject popUps;
    public GameObject buyConfirmationPopUp;
    public TextMeshProUGUI buyConfirmationPopUpText;
    public Button buyButton;
    public GameObject notEnoughCreditsPopUp;
    public TextMeshProUGUI notEnoughCreditsPopUpText;

    // Car variables.
    [Space(10)]
    [Header("Car Related Stuff")]
    [SerializeField] private CarCollection carCollection;
    [SerializeField] private CarDisplay carDisplay;
    private GameObject currentCar;
    private Car car;
    private int currentCarType;
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;

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

    private void Awake()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Ensure at least one car is always owned.
        if (saveData.Cars.Count == 0)
        {
            // Set the 0th ferret (Type = 0) as being owned.
            SaveData.CarData defaultCarData = new SaveData.CarData();

            // Add the default ferret to the Cars dictionary.
            saveData.Cars[(0, 0)] = defaultCarData;

            saveData.CurrentCarType = 0;
            saveData.CurrentCarIndex = 0;
            saveData.LastOwnedCarType = 0;
            saveData.LastOwnedCarIndex = 0;

            // Save changes.
            SaveManager.Instance.SaveGame();
        }

        carParts = new CarPart[13][];
        CacheComponents();
        RevertToLastOwnedCar();
        nitrocount.text = saveData.NitroCount.ToString();
    }

    private void Update()
    {
        // ************************************************* PC KEYBOARD INPUT COMMANDS FOR DEBUGGING ************************************************* //
        if (Input.GetKeyDown(KeyCode.C))                                                                                                                //
        {                                                                                                                                               //
            creditManager.ChangeCredits(1900000);                                                                                                       //
        }                                                                                                                                               //
        if (Input.GetKeyDown(KeyCode.H))                                                                                                                //
        {                                                                                                                                               //
            Cursor.visible = !Cursor.visible; // Toggle the cursor's visibility                                                                         //
        }                                                                                                                                               //
                                                                                                                                                        //
        if (isPlayerInPaintMenu) /* 0 = Primary Color, 1 = Secondary Color, 2 = Rim Color, 3 = Primary Light, 4 = Secondary Light, 5 = Tail Lights */   //
        {                                                                                                                                               //
            if (Input.GetKeyUp(KeyCode.B))                                                                                                              //
            {                                                                                                                                           //
                if (whichPartToPaint < 2)                                                                                                               //
                    backButtons[3].onClick.Invoke();                                                                                                    //
                else if (whichPartToPaint == 2)                                                                                                         //
                    backButtons[2].onClick.Invoke();                                                                                                    //
                else                                                                                                                                    //
                    backButtons[4].onClick.Invoke();                                                                                                    //
            }                                                                                                                                           //
                                                                                                                                                        //
            if (Input.GetKeyDown(KeyCode.LeftArrow) && whichPartToPaint < 2)                                                                            //
            {                                                                                                                                           //
                buttonSwitchPaintTypeLeft.onClick.Invoke();                                                                                             //
            }                                                                                                                                           //
                                                                                                                                                        //
            if (Input.GetKeyDown(KeyCode.RightArrow) && whichPartToPaint < 2)                                                                           //
            {                                                                                                                                           //
                buttonSwitchPaintTypeRight.onClick.Invoke();                                                                                            //
            }                                                                                                                                           //
                                                                                                                                                        //
            if (Input.GetKeyDown(KeyCode.Return))                                                                                                       //
            {                                                                                                                                           //
                invokedFromUpdate = true;                                                                                                               //
                if (whichPartToPaint == 0)                                                                                                              //
                {                                                                                                                                       //
                    primaryColorButton.onClick.Invoke();                                                                                                //
                    SetColor(primaryColorButton);                                                                                                       //
                }                                                                                                                                       //
                                                                                                                                                        //
                else if (whichPartToPaint == 1)                                                                                                         //
                {                                                                                                                                       //
                    secondaryColorButton.onClick.Invoke();                                                                                              //
                    SetColor(secondaryColorButton);                                                                                                     //
                }                                                                                                                                       //
                                                                                                                                                        //
                else if (whichPartToPaint == 2)                                                                                                         //
                {                                                                                                                                       //
                    rimButton.onClick.Invoke();                                                                                                         //
                    SetColor(rimButton);                                                                                                                //
                }                                                                                                                                       //
                                                                                                                                                        //
                else if (whichPartToPaint == 3)                                                                                                         //
                {                                                                                                                                       //
                    primaryLightColorButton.onClick.Invoke();                                                                                           //
                    SetColor(primaryLightColorButton);                                                                                                  //
                }                                                                                                                                       //
                                                                                                                                                        //
                else if (whichPartToPaint == 4)                                                                                                         //
                {                                                                                                                                       //
                    secondaryLightColorButton.onClick.Invoke();                                                                                         //
                    SetColor(secondaryLightColorButton);                                                                                                //
                }                                                                                                                                       //
                                                                                                                                                        //
                else                                                                                                                                    //
                {                                                                                                                                       //
                    tailLightColorButton.onClick.Invoke();                                                                                              //
                    SetColor(tailLightColorButton);                                                                                                     //
                }                                                                                                                                       //
                invokedFromUpdate = false;                                                                                                              //
            }                                                                                                                                           //
        }                                                                                                                                               //
                                                                                                                                                        //
        if (inCustomizationMenu) return;                                                                                                                //
                                                                                                                                                        //
        if (!isPlayerInPaintMenu)                                                                                                                       //
        {                                                                                                                                               //
            if (Input.GetKeyDown(KeyCode.Return))                                                                                                       //
            {                                                                                                                                           //
                carCustomize.onClick.Invoke();                                                                                                          //
            }                                                                                                                                           //
                                                                                                                                                        //
            if (Input.GetKeyDown(KeyCode.LeftArrow))                                                                                                    //
            {                                                                                                                                           //
                leftCarChange.onClick.Invoke();                                                                                                         //
            }                                                                                                                                           //
                                                                                                                                                        //
            else if (Input.GetKeyDown(KeyCode.RightArrow))                                                                                              //
            {                                                                                                                                           //
                rightCarChange.onClick.Invoke();                                                                                                        //
            }                                                                                                                                           //
        }                                                                                                                                               //
    }   // ******************************************************************************************************************************************** //


    private void CacheComponents()
    {
        // Initialize customizationSubBuckets and the first dimension of the cached arrays
        //customizationSubBuckets = new GameObject[customizationBuckets.Length][];
        rawImages = new RawImage[customizationBuckets.Length][];
        textMeshPros = new TextMeshProUGUI[customizationBuckets.Length][];
        bucketButtons = new Button[customizationBuckets.Length][];
        bucketImages = new Image[customizationBuckets.Length][];

        for (int i = 0; i < customizationBuckets.Length; i++)
        {
            Transform bucketTransform = customizationBuckets[i].transform;
            int childCount = bucketTransform.childCount - 1;  // Subtracting one to exclude the last child (back button)

            // Store child game objects of the current customizationBucket, excluding the back button
            /*customizationSubBuckets[i] = new GameObject[childCount];
            for (int c = 0; c < childCount; c++)
            {
                customizationSubBuckets[i][c] = bucketTransform.GetChild(c).gameObject;
                Debug.Log("Customization bucket column " + i + ", row " + c + " name: " + customizationSubBuckets[i][c].name);
            }*/

            int subArrayLength = customizationSubBuckets[i].items.Length;

            // Initialize the second dimension of the cached arrays based on the subArrayLength
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
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        currentCarIndex += change;

        // Get the current car type (e.g. ferret, viking HD) and get the total number of this type of car owned.
        currentCarType = saveData.CurrentCarType;
        numOfThisCarTypeOwned  = SaveManager.Instance.SaveData.Cars.Count(car => car.Key.CarType == currentCarType);

        // Boundary checking.
        if (currentCarIndex < 0)
        {
            // Switch to the previous car type.
            currentCarType -= 1;
            if (currentCarType < 0) currentCarType = carCollection.carTypes.Count - 1;
            numOfThisCarTypeOwned = SaveManager.Instance.SaveData.Cars.Count(car => car.Key.CarType == currentCarType);

            // Set the index to the last owned car copy of this car type. E.g. if the player owns 5 ferrets, set the currentCarIndex to 4.
            currentCarIndex = Mathf.Min(Mathf.Max(numOfThisCarTypeOwned - 1, 0), carCollection.carTypes[currentCarType].items.Count - 1);
        }
        // Boundary checking. So as to not display the full 100 potentially buyable copies of the same car,
        // check to make sure we don't go over the total number of copies of this car that the player owns.
        else if (currentCarIndex > numOfThisCarTypeOwned - 1 || currentCarIndex > carCollection.carTypes[currentCarType].items.Count - 1)
        {
            currentCarType += 1;
            if (currentCarType > carCollection.carTypes.Count - 1) currentCarType = 0;
            currentCarIndex = 0;
        }


        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        // Grab the car object from the car collection field. DO I NEED THIS???
        car = (Car)carCollection.carTypes[currentCarType].items[currentCarIndex];

        // Save the last owned car index if this car is owned.
        bool isOwned = saveData.Cars.ContainsKey((currentCarType, currentCarIndex));
        if (isOwned)
        {
            saveData.LastOwnedCarType = currentCarType;
            saveData.LastOwnedCarIndex = currentCarIndex;
        }
        SaveManager.Instance.SaveGame();

        if (carDisplay != null) currentCar = carDisplay.DisplayCar((Car)carCollection.carTypes[currentCarType].items[currentCarIndex], currentCarType, currentCarIndex);

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

        // Loop through each row (array) in the 2D array
        for (int i = 0; i < carParts.Length; i++)
        {
            // Sort each row (1D array) by price.
            Array.Sort(carParts[i], (part1, part2) => part1.price.CompareTo(part2.price));
        }

        UpdatePerformanceStats();
        
        // Add chrome display function over here.
    }

    // Revert the current car index back to the last car the player owns.
    public void RevertToLastOwnedCar()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Retrieve the last owned car type and index.
        currentCarType = saveData.LastOwnedCarType;
        currentCarIndex = saveData.LastOwnedCarIndex;

        // Update the currently displayed car.
        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        // Save changes to disk.
        SaveManager.Instance.SaveGame();

        // Temporarily set currentCarIndex to 0, then change the car.
        int oldCurrentCarIndex = currentCarIndex;
        currentCarIndex = 0;
        ChangeCar(oldCurrentCarIndex);

        // Restore the original currentCarIndex.
        currentCarIndex = oldCurrentCarIndex;
    }

    public void SetLastOwnedCar()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Update the last owned car type and index.
        saveData.CurrentCarType = currentCarType;
        saveData.CurrentCarIndex = currentCarIndex;

        saveData.LastOwnedCarType = currentCarType;
        saveData.LastOwnedCarIndex = currentCarIndex;

        // Save the changes to disk.
        SaveManager.Instance.SaveGame();
    }


    // Calculate the sell price of the car. (Sell price = car price / 2 + value of installed parts / 4).
    public int GetSellPrice()
    {
        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Attempt to get the car's save data.
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            // Throw an exception if the car data doesn't exist.
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        int sellPrice = car.price / 2;

        // Iterate across all car part types from 0 (Exhausts) to 12 (Liveries).
        for (int i = 0; i < carParts.Length; i++)
        {
            // Access the currently installed part index for the specified part 'i' (0-12).
            currentPartIndex = carData.CarParts[i].CurrentInstalledPart;
            if (currentPartIndex == -1) currentPartIndex = GetDefaultPartIndex(i);

            // Don't add to the sell price if the currently installed part is a default part.
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
        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Iterate across all car part types from 0 (Exhausts) to 12 (Liveries).
        for (int i = 0; i < carParts.Length; i++)
        {
            bool foundDefaultPart = false;
            for (int j = 0; j < carParts[i].Length; j++)
            {
                switch (i)
                {
                    // Set the car's default exhausts as active.
                    case 0:
                        if (carParts[i][j].name == car.DefaultExhaust) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    // Set the car's default front wheels as active.
                    case 2:
                        if (carParts[i][j].name == car.DefaultFrontWheels) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    // Set the car's default rear splitter as active.
                    case 3:
                        if (carParts[i][j].name == car.DefaultRearSplitter) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    // Set the car's default rear wheels as active.
                    case 4:
                        if (carParts[i][j].name == car.DefaultRearWheels) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    // Set the car's default spoiler as active.
                    case 6:
                        if (carParts[i][j].name == car.DefaultSpoiler) { carParts[i][j].gameObject.SetActive(true); foundDefaultPart = true; }
                        else carParts[i][j].gameObject.SetActive(false);
                        break;
                    default:
                        carParts[i][j].gameObject.SetActive(false);
                        break;
                }
            }
            if (!foundDefaultPart) // If no default part exists, set the 0th car part (Stock/None) as active.
            {
                carParts[i][0].gameObject.SetActive(true);
            }
        }

        // Paint the car's default colors.
        car.SetDefaultColors();

        // Remove any livery currently painted on the car.
        car.ApplyLivery(0);

        // Remove the car entry from the dictionary.
        saveData.Cars.Remove((currentCarType, carIndex));

        // Save the updated data.
        SaveManager.Instance.SaveGame();
    }

    // This function is used instead of ClearOwnedParts() if the player sells a car copy somewhere
    // in the middle of their total number of cars of this type. It works by shifting all the keys
    // of the cars dictionary following this car's index by -1.
    public void AdjustCarIndices(int carType, int removedCarIndex)
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Create a list of keys that need to be shifted.
        var keysToShift = saveData.Cars.Keys
            .Where(key => key.CarType == carType && key.CarIndex > removedCarIndex)
            .OrderBy(key => key.CarIndex)
            .ToList();

        // Shift each car's index down by 1.
        foreach (var key in keysToShift)
        {
            // Extract the CarData and remove the old key.
            SaveData.CarData carData = saveData.Cars[key];
            saveData.Cars.Remove(key);

            // Insert the car data with the new adjusted key.
            var newKey = (key.CarType, key.CarIndex - 1);
            saveData.Cars[newKey] = carData;
        }

        // Save the updated data.
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

        // Set all previous buttons to not be interactable.
        bucketButtons[currentBucketIndex][bucketSubIndex].interactable = false;

        // Change the alpha values of the previous buttons to zero.
        for (int i = 0; i < bucketButtons[currentBucketIndex].Length; i++)
        {
            AdjustAlpha(rawImages[currentBucketIndex][i], textMeshPros[currentBucketIndex][i], bucketImages[currentBucketIndex][i], 0f);
        }

        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Ensure the car exists in the dictionary.
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        // Get the index of the current installed part so we can highlight the correct button.
        startingIndex = carData.CarParts[currentPartIndex].CurrentInstalledPart;
        if (startingIndex == -1) startingIndex = GetDefaultPartIndex(currentPartIndex);
        currentInstantiatedButtonIndex = startingIndex;

        // Point the camera at the part.
        if (currentPartIndex < 8)
            garageCamera.SetCameraPosition(currentPartIndex + 1);

        // Handle 'All Rims' menu logic.
        if (isPlayerInAllRimsMenu)
        {
            // Retrieve the currently installed rear rims part index.
            rearRims = carData.CarParts[4].CurrentInstalledPart;

            // Deactivate the previously installed rear rims part.
            if (rearRims == -1) rearRims = GetDefaultPartIndex(4); // Handle entering the "All Rims" menu for the first time after buying a car.
            else carParts[4][rearRims].gameObject.SetActive(false);

            // Match the rear rims with the front rims.
            carParts[4][startingIndex].gameObject.SetActive(true);
        }

        // Create button array with same length as the number of parts in this bucket.
        int numOfParts = carParts[partIndex].Length;
        instantiatedButtons = new Button[numOfParts];

        // Activate the scroll view for the buttons & set it to the parent transform.
        scrollController.scrollRect.gameObject.SetActive(true);
        Transform spawnTransform = scrollController.scrollRect.content.transform;

        for (int i = 0; i < numOfParts; i++)
        {
            float alphaValue = 10;
            bool interactable = false;
            Button newButton = Instantiate(buttonTemplate, Vector3.zero, Quaternion.identity, spawnTransform /*buttonTemplate.transform.parent*/); //////////////// NEW ///////////////////////////

            // Find if the current part is owned.
            bool isPartOwned = carData.CarParts[currentPartIndex].Ownership.TryGetValue(i, out bool owned) && owned;

            // Make the button clickable.
            int tempIndex = i;
            newButton.onClick.AddListener(() => ConfirmBuyPart(tempIndex, partIndex));

            // Adjust the alpha channels of the button's text & image.
            TextMeshProUGUI[] texts = newButton.GetComponentsInChildren<TextMeshProUGUI>();
            Image image = newButton.GetComponent<Image>();
            texts[0].color = new Color(texts[0].color.r, texts[0].color.g, texts[0].color.b, alphaValue / 255f);
            texts[1].color = new Color(texts[0].color.r, texts[0].color.g, texts[0].color.b, alphaValue / 255f);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alphaValue / 255f);

            // Correctly set the part's button's name & price.
            texts[0].text = carParts[partIndex][i].name;
            if (partIndex == 8)
                texts[1].text = "+" + System.Math.Round((513.57616f * car.defaultAccelMaxValue * carParts[partIndex][i].accelMaxValueUpgrade - 608.44812f) - (513.57616f * car.defaultAccelMaxValue - 608.44812f)).ToString() + " HP";

            else if (partIndex == 9)
                texts[1].text = "0-60: -" + System.Math.Round((Mathf.Max(-12.28856f * car.defaultAccelIncreaseRate + 23.2393f, -5.484f * car.defaultAccelIncreaseRate + 12.068f) - Mathf.Max(-12.28856f * car.defaultAccelIncreaseRate * carParts[partIndex][i].accelIncreaseRateUpgrade + 23.2393f, -5.484f * car.defaultAccelIncreaseRate * carParts[partIndex][i].accelIncreaseRateUpgrade + 12.068f)), 1).ToString() + "s";

            else if (partIndex == 10)
                texts[1].text = "+" + (carParts[partIndex][i].maxLives - car.defaultNumLives) + " lives";

            else
            {
                if (i == startingIndex)
                    texts[1].text = "INSTALLED";
                else if (isPartOwned)
                    texts[1].text = "OWNED";
                else
                    texts[1].text = carParts[partIndex][i].price.ToString() + " cr";    // Display price.
            }

            // Activate the button.
            newButton.interactable = interactable;
            newButton.gameObject.SetActive(true);
            instantiatedButtons[i] = newButton;

            // Add the button & components to the scroll view's cached component arrays.
            scrollController.buttons.Add(newButton);
            scrollController.buttonTransforms.Add(newButton.transform);
            scrollController.buttonImages.Add(image);
            scrollController.buttonNames.Add(texts[0]);
            scrollController.buttonPrices.Add(texts[1]);
            scrollController.backButton = backButtons[currentBucketIndex - 1];

        }

        // Initialize the scroll view, and set its scroll position to the button with the currently installed part.
        scrollController.Initialize();
        scrollController.SetScrollPosition(startingIndex);
    }

    public void ChangeItem(int change)
    {
        int oldInstantiatedButtonIndex = currentInstantiatedButtonIndex;
        currentInstantiatedButtonIndex = change;

        // Boundary checking.
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

        // Change the car suspension height.
        if (currentPartIndex == 7) suspensionHolder.SetSuspensionHeight(currentInstantiatedButtonIndex);

        // Change the car livery.
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
            buyConfirmationPopUpText.text = "Buy  this  part  for  " + carParts[partIndex][partType].price + "?";
            notEnoughCreditsPopUp.SetActive(false);
            buyButton.onClick.AddListener(() => BuyPart(partType, partIndex));
            buyConfirmationPopUp.SetActive(true);
        }
    }

    private void BuyPart(int partType, int partIndex)
    {
        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;

        // Ensure the car exists in the dictionary.
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            throw new KeyNotFoundException($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
        }

        // Get the old part index.
        int oldIndex = carData.CarParts[partIndex].CurrentInstalledPart;
        if (oldIndex == -1) oldIndex = GetDefaultPartIndex(partIndex);

        // Update the currently installed part.
        carData.CarParts[partIndex].CurrentInstalledPart = partType;

        // Save both front & rear wheel indices if the player is in the 'All Rims' bucket.
        if (isPlayerInAllRimsMenu)
        {
            carData.CarParts[4].CurrentInstalledPart = partType; // Rear wheels
            rearRims = partType;
        }

        // If the part is a performance part, update the current car's performance stats.
        if (partIndex > 7 && partIndex < 11)
        {
            UpdatePerformanceStats();
        }

        // Update the startingIndex to the newly installed part.
        startingIndex = partType;

        // Check if the part has already been purchased
        bool isPartOwned = carData.CarParts[partIndex].Ownership.TryGetValue(partType, out bool owned) && owned;

        // Update the credit count and mark part as owned if the part hasn't been purchased before.
        if (!isPartOwned)
        {
            carData.CarParts[partIndex].Ownership[partType] = true;
            creditManager.ChangeCredits((int)(-1 * carParts[partIndex][partType].price));
        }

        // Save the updated data.
        SaveManager.Instance.SaveGame();

        // Update the button texts.
        scrollController.buttonPrices[oldIndex].text = "OWNED";
        scrollController.buttonPrices[partType].text = "INSTALLED";

        // Deactivate the UI popups
        popUps.SetActive(false);
    }

    public void EnterPreviousBucket()
    {
        if (inItemDisplayMenu)
        {
            //if (currentPartIndex < 8)
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
        
        // Disable the buttons that we used to get into the paint menu, and enable the switch paint type buttons.
        if (whichPartToPaint == 0 || whichPartToPaint == 1)
        {
            buttonPrimaryColor.gameObject.SetActive(false);
            buttonSecondaryColor.gameObject.SetActive(false);
            buttonSwitchPaintTypeLeft.gameObject.SetActive(true);
            buttonSwitchPaintTypeRight.gameObject.SetActive(true);
        }
        else if (whichPartToPaint == 2)
        {
            frontRimButton.gameObject.SetActive(false);
            rearRimButton.gameObject.SetActive(false);
            allRimButton.gameObject.SetActive(false);
            rimColorButton.gameObject.SetActive(false);
            buttonSwitchPaintTypeLeft.gameObject.SetActive(true);
            buttonSwitchPaintTypeRight.gameObject.SetActive(true);
            paintType.text = "MATTE";
            garageCamera.SetCameraPosition(9);
        }
        else
        {
            primaryLightButton.gameObject.SetActive(false);
            secondaryLightButton.gameObject.SetActive(false);
            tailLightButton.gameObject.SetActive(false);
            paintType.text = "";
        }

        // Disable the buttons that allowed us to change customization buckets.
        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);

        // Enable the colors menu.
        colors.SetActive(true);

        // Set the default paint type to matte.
        currentPaintType = 0;
        activeShader = matteShader;
    }

    public void ChangePaintType(int change)
    {
        // Rims can only either be matte or emissive.
        if (whichPartToPaint == 2)
        {
            if (change < 0) currentPaintType = 0;
            else currentPaintType = 3;
        }
        else
            currentPaintType += change;

        // Check boundaries.
        if (whichPartToPaint == 0) // Primary color doesn't include emissive colors.
        {
            if (currentPaintType < 0 || currentPaintType > 2)
            {
                currentPaintType = Mathf.Clamp(currentPaintType, 0, 2);
                return;
            }
        }
        else if (whichPartToPaint == 1) // Secondary color includes emissive colors.
        {
            if (currentCarType == 0 || currentCarType == 1) // Ferret & Viking HD don't have emissive colors enabled. NEEDS TO BE CHANGED FOR METAL COLORS IN THE FUTURE.
            {
                if (currentPaintType < 0 || currentPaintType > 2)
                {
                    currentPaintType = Mathf.Clamp(currentPaintType, 0, 2);
                    return;
                }
            }
            else if (currentPaintType < 0 || currentPaintType > 3)
            {
                if (currentPaintType < 0 || currentPaintType > 3)
                {
                    currentPaintType = Mathf.Clamp(currentPaintType, 0, 3);
                    return;
                }
            }
        }
        else if (whichPartToPaint == 2) // Rim color includes emissive colors.
        {
            if (currentPaintType < 0 || currentPaintType > 3)
            {
                currentPaintType = Mathf.Clamp(currentPaintType, 0, 3);
                return;
            }
        }

        if (currentPaintType == 0) // MATTE.
        {
            colorBuckets[0].SetActive(true);
            colorBuckets[1].SetActive(false);
            colorBuckets[2].SetActive(false);
            colorBuckets[3].SetActive(false);
            paintType.text = "MATTE";
            activeShader = matteShader;
        }

        else if (currentPaintType == 1) // GLOSS.
        {
            colorBuckets[0].SetActive(false);
            colorBuckets[1].SetActive(true);
            colorBuckets[2].SetActive(false);
            colorBuckets[3].SetActive(false);
            paintType.text = "GLOSS";
            activeShader = glossShader;
        }

        else if (currentPaintType == 2) // PEARLESCENT.
        {
            colorBuckets[0].SetActive(false);
            colorBuckets[1].SetActive(false);
            colorBuckets[2].SetActive(true);
            colorBuckets[3].SetActive(false);
            paintType.text = "PEARLESCENT";
            activeShader = glossShader;
        }
        else if (currentPaintType == 3) // EMISSIVE.
        {
            colorBuckets[0].SetActive(false);
            colorBuckets[1].SetActive(false);
            colorBuckets[2].SetActive(false);
            colorBuckets[3].SetActive(true);
            paintType.text = "EMISSIVE";
            activeShader = matteShader;
        }
    }

    // Called when user clicks on a paint button. Sets the colour of the part to paint to the button's colour.
    public void SetColor()
    {
        if (invokedFromUpdate) return;
        Button clickedButton = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        if (clickedButton == null)
        {
            Debug.LogWarning("Current selected object is not a button.");
            return;
        }

        // Get the material of the paint button we clicked.
        Material buttonMaterial = clickedButton.GetComponent<Image>().material;
        Color topColor, middleColor, bottomColor;
        Color buttonColor = new Color(0,0,0);

        // Retrieve the three colors from the button if it is a pearlescent paint button.
        if (buttonMaterial && buttonMaterial.HasProperty("_TopColor") && buttonMaterial.HasProperty("_MiddleColor") && buttonMaterial.HasProperty("_BottomColor"))
        {
            
            topColor = buttonMaterial.GetColor("_TopColor");
            middleColor = buttonMaterial.GetColor("_MiddleColor");
            bottomColor = buttonMaterial.GetColor("_BottomColor");
        }
        else // Retrieve the main color of the button if it is any other paint type.
        {
            buttonColor = topColor = middleColor = bottomColor = clickedButton.GetComponent<Image>().color;
        }

        // Access the SaveData instance and get the current car's data.
        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
            return;
        }

        // Get ColorType enum.
        Car.ColorType colorType = (Car.ColorType)whichPartToPaint;

        // Update the color data based on the current part to paint.
        SaveData.ColorData colorData = carData.Colors[whichPartToPaint];

        // Update the material of the car to reflect the new color.
        switch (colorType)
        {
            case Car.ColorType.PRIMARY_COLOR:
                primaryColor.color = topColor;
                primaryColor.SetColor("_FresnelColor", middleColor); // Using the middle color for FresnelColor.
                primaryColor.SetColor("_FresnelColor2", bottomColor); // Using the bottom color for FresnelColor2.
                colorData.BaseColor = new float[] { topColor.r, topColor.g, topColor.b, topColor.a };
                colorData.FresnelColor = new float[] { middleColor.r, middleColor.g, middleColor.b, middleColor.a };
                colorData.FresnelColor2 = new float[] { bottomColor.r, bottomColor.g, bottomColor.b, bottomColor.a };
                break;
            case Car.ColorType.SECONDARY_COLOR:
                if (currentPaintType == 1 || currentPaintType == 2) // Non-emissive color.
                {
                    secondaryColor.color = topColor;
                    secondaryColor.SetColor("_FresnelColor", middleColor);
                    secondaryColor.SetColor("_FresnelColor2", bottomColor);
                    secondaryColor.SetColor("_EmissionColor", Color.black);
                    secondaryColor.DisableKeyword("_EMISSION");
                    colorData.BaseColor = new float[] { topColor.r, topColor.g, topColor.b, topColor.a };
                    colorData.FresnelColor = new float[] { middleColor.r, middleColor.g, middleColor.b, middleColor.a };
                    colorData.FresnelColor2 = new float[] { bottomColor.r, bottomColor.g, bottomColor.b, bottomColor.a };
                    colorData.EmissionColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                }
                else // Emissive color.
                {
                    secondaryColor.color = Color.black; // Set base color to black.
                    secondaryColor.SetColor("_FresnelColor", Color.black);
                    secondaryColor.SetColor("_FresnelColor2", Color.black);
                    secondaryColor.SetColor("_EmissionColor", buttonColor);
                    secondaryColor.EnableKeyword("_EMISSION");
                    colorData.BaseColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                    colorData.FresnelColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                    colorData.FresnelColor2 = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                    colorData.EmissionColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                }
                break;
            case Car.ColorType.RIM_COLOR:
                if (currentPaintType == 3) // Emissive color.
                {
                    rimColor.color = Color.black; // Set base color to black.
                    /*  UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
                    secondaryColor.SetColor("_FresnelColor", Color.black);
                    secondaryColor.SetColor("_FresnelColor2", Color.black);*/
                    rimColor.SetColor("_EmissionColor", buttonColor);
                    rimColor.EnableKeyword("_EMISSION");
                    colorData.BaseColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                    /*   UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
                    colorData.FresnelColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                    colorData.FresnelColor2 = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };*/
                    colorData.EmissionColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                }
                else
                {
                    rimColor.color = buttonColor;
                    /*  UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
                    secondaryColor.SetColor("_FresnelColor", buttonColor);
                    secondaryColor.SetColor("_FresnelColor2", buttonColor);*/
                    rimColor.SetColor("_EmissionColor", Color.black);
                    rimColor.DisableKeyword("_EMISSION");
                    colorData.BaseColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                    /*   UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
                    colorData.FresnelColor = new float[] { middleColor.r, middleColor.g, middleColor.b, middleColor.a };
                    colorData.FresnelColor2 = new float[] { bottomColor.r, bottomColor.g, bottomColor.b, bottomColor.a };*/
                    colorData.EmissionColor = new float[] { Color.black.r, Color.black.g, Color.black.b, Color.black.a };
                }
                break;
            case Car.ColorType.PRIMARY_LIGHT:
                primaryLight.SetColor("_EmissionColor", buttonColor);
                colorData.EmissionColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                break;
            case Car.ColorType.SECONDARY_LIGHT:
                secondaryLight.SetColor("_EmissionColor", buttonColor);
                colorData.EmissionColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                break;
            case Car.ColorType.TAIL_LIGHT:
                tailLight.SetColor("_EmissionColor", buttonColor);
                colorData.EmissionColor = new float[] { buttonColor.r, buttonColor.g, buttonColor.b, buttonColor.a };
                break;
            default:
                Debug.LogError("Invalid part to paint");
                return;
        }

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }

    // Overloaded DEBUG version of SetColor() that uses a button as input. Only called when pressing Return on a paint button while using a PC keyboard.
    public void SetColor(Button clickedButton)
    {
        if (clickedButton == null)
        {
            Debug.LogWarning("Clicked button is null.");
            return;
        }

        // Get the material of the paint button we clicked.
        Material buttonMaterial = clickedButton.GetComponent<Image>().material;
        Color topColor, middleColor, bottomColor;
        Color buttonColor = new Color(0, 0, 0);

        // Retrieve the three colors from the button if it is a pearlescent paint button.
        if (buttonMaterial && buttonMaterial.HasProperty("_TopColor") && buttonMaterial.HasProperty("_MiddleColor") && buttonMaterial.HasProperty("_BottomColor"))
        {

            topColor = buttonMaterial.GetColor("_TopColor");
            middleColor = buttonMaterial.GetColor("_MiddleColor");
            bottomColor = buttonMaterial.GetColor("_BottomColor");
        }
        else // Retrieve the main color of the button if it is any other paint type.
        {
            buttonColor = topColor = middleColor = bottomColor = clickedButton.GetComponent<Image>().color;
        }

        // Access the SaveData instance and get the current car's data.
        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
            return;
        }

        // Update the color data based on the current part to paint.
        SaveData.ColorData colorData = carData.Colors[whichPartToPaint];
        colorData.BaseColor = new float[] { topColor.r, topColor.g, topColor.b, topColor.a };
        if (currentPaintType == 1 || currentPaintType == 2) // For Fresnel colors (e.g., pearlescent paint).
        {
            colorData.FresnelColor = new float[] { middleColor.r, middleColor.g, middleColor.b, middleColor.a };
            colorData.FresnelColor2 = new float[] { bottomColor.r, bottomColor.g, bottomColor.b, bottomColor.a };
        }
        else // For non-Fresnel colors.
        {
            colorData.FresnelColor = null;
            colorData.FresnelColor2 = null;
        }

        // Get ColorType enum.
        Car.ColorType colorType = (Car.ColorType)whichPartToPaint;

        // Update the material of the car to reflect the new color.
        switch (colorType)
        {
            case Car.ColorType.PRIMARY_COLOR:
                primaryColor.color = topColor;
                primaryColor.SetColor("_FresnelColor", middleColor); // Using the middle color for FresnelColor.
                primaryColor.SetColor("_FresnelColor2", bottomColor); // Using the bottom color for FresnelColor2.
                break;
            case Car.ColorType.SECONDARY_COLOR:
                secondaryColor.color = topColor;
                secondaryColor.SetColor("_FresnelColor", middleColor);
                secondaryColor.SetColor("_FresnelColor2", bottomColor);
                if (currentPaintType == 1 || currentPaintType == 2) // Non-emissive color.
                {
                    secondaryColor.SetColor("_FresnelColor", middleColor);
                    secondaryColor.SetColor("_FresnelColor2", bottomColor);
                    secondaryColor.SetColor("_EmissionColor", Color.black);
                    secondaryColor.DisableKeyword("_EMISSION");
                }
                else if (currentPaintType == 3) // Emissive color.
                {
                    secondaryColor.color = Color.black; // Set base color to black.
                    secondaryColor.SetColor("_EmissionColor", buttonColor);
                    secondaryColor.EnableKeyword("_EMISSION");
                }
                break;
            case Car.ColorType.RIM_COLOR:
                rimColor.color = buttonColor;
                if (currentPaintType == 3) // Emissive color.
                {
                    rimColor.color = Color.black; // Set base color to black.
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

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }

    /*----------------------------------------- OTHER FUNCTIONS --------------------------------------------*/
    public void UpdatePerformanceStats()
    {
        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;

        int engineIndex = 0;
        int transmissionIndex = 0;
        int livesIndex = 0;

        // Ensure the car exists in the dictionary.
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            // Do nothing.
        }
        else
        {
            // Retrieve the indices of the installed engine, transmission, and lives parts.
            Debug.Log(engineIndex + " " + carData.CarParts[8].CurrentInstalledPart);
            engineIndex = engineIndex == -1 ? 0: carData.CarParts[8].CurrentInstalledPart;       // ENGINE
            transmissionIndex = transmissionIndex == -1 ? 0: carData.CarParts[9].CurrentInstalledPart; // TRANSMISSION
            livesIndex = livesIndex == -1 ? 0: carData.CarParts[10].CurrentInstalledPart;       // LIVES
        }

        // Update car stats based on the currently installed parts.
        if (engineIndex != -1) car.accelMaxValue = car.defaultAccelMaxValue * carParts[8][engineIndex].accelMaxValueUpgrade;
        if (transmissionIndex != -1) car.accelIncreaseRate = car.defaultAccelIncreaseRate * carParts[9][transmissionIndex].accelIncreaseRateUpgrade;
        if (livesIndex != -1) car.numlives = carParts[10][livesIndex].maxLives;

        // Update the display.
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
