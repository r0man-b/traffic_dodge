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

    [Header("Custom Objects")]
    public CreditManager creditManager;
    public GarageUIManager garageUIManager;

    [Header("Lock UI image/background")]
    public GameObject lockUiElement;
    public GameObject lockImage;

    [Header("Bottom button sets")]
    public GameObject buttonSet1;
    public GameObject buttonSet2;

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

    [Header("Sound")]
    public MenuSounds menuSounds;

    private Car currentCar;
    private GameObject _spawnedModel;
    private string currentCarType;   // switched to string
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;
    private int sellPrice;
    private const string carsOwned = "CARS_OWNED";

    // -------------------- Loot-box style randomizer --------------------
    private readonly int spinCount = 100;
    private readonly float startDelay = 0.05f;   // fast at start
    private readonly float endDelay = 0.7f;   // slow at end
    private readonly float slowDownBias = 6f;
    public CarCollection carCollection;
    public GameObject carContainer;
    Coroutine _spinCo;
    [Header("Turntable Spin")]
    [SerializeField] private Transform turntable;     // Defaults to carHolder if left null
    [SerializeField] private float spinMaxSpeed = 720f;  // deg/sec at start of spin
    [SerializeField] private float spinMinSpeed = 60f;  // deg/sec near the end

    // Randomize car for lootboxes.
    public void RandomizeCar()
    {
        lockImage.SetActive(false);
        if (_spinCo != null) StopCoroutine(_spinCo);
        _spinCo = StartCoroutine(SpinRoutine());
    }

    //  Coroutine that spawns randomized cars until it stops.
    IEnumerator SpinRoutine()
    {
        // Build the exact per-iteration delays so the spin matches timing perfectly
        List<float> delays = BuildDelaySchedule();
        float totalDuration = 0f;
        for (int i = 0; i < delays.Count; i++) totalDuration += delays[i];

        // Start the decelerating spin in parallel
        StartCoroutine(SpinTurntable(totalDuration));

        for (int i = 0; i < spinCount; i++)
        {
            // Weighted pick (unchanged)
            float[] weights = { 40f, 20f, 10f, 8f, 6f, 5f, 4f, 3f, 2f, 1f, 0.5f, 0.25f, 0.2f, 0.05f };
            float total = 0f;
            float[] cum = new float[weights.Length];
            for (int w = 0; w < weights.Length; w++) { total += weights[w]; cum[w] = total; }

            int typeIdx = WeightedPick(cum, total);
            typeIdx = Mathf.Clamp(typeIdx, 0, Mathf.Max(0, carCollection.carTypes.Count - 1));

            var bucket = carCollection.carTypes[typeIdx];
            int variantCount = Mathf.Max(1, bucket.items.Count);
            int variantIdx = Random.Range(0, variantCount);

            var carAsset = bucket.items[variantIdx] as Car;
            if (carAsset != null)
            {
                DisplayCar(
                    carAsset,
                    string.IsNullOrWhiteSpace(carAsset.car_name) ? carAsset.name : carAsset.car_name,
                    variantIdx,
                    true
                );
            }

            // Wait using the precomputed, eased delay (so spin matches exactly)
            yield return new WaitForSecondsRealtime(delays[i]);
        }

        _spinCo = null; // finished
    }


    // Couroutine that spins the turntable.
    IEnumerator SpinTurntable(float totalDuration)
    {
        var target = turntable != null ? turntable : carHolder;
        if (target == null || totalDuration <= 0f) yield break;

        // Cache starting local rotation and spin accumulator
        Vector3 startEuler = target.localEulerAngles;
        float elapsed = 0f;
        float spinAccum = 0f; // track how much we’ve rotated (in degrees)

        while (elapsed < totalDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            // Normalized progress through the spin [0..1]
            float u = Mathf.Clamp01(elapsed / totalDuration);
            float uBias = Mathf.Pow(u, slowDownBias);                 // same easing as picks
            float omega = Mathf.Lerp(spinMaxSpeed, spinMinSpeed, uBias); // deg/sec

            float dTheta = omega * dt;
            spinAccum += dTheta;

            // Apply rotation around local Y
            target.Rotate(0f, dTheta, 0f, Space.Self);
            yield return null;
        }

        // Snap back to the exact starting rotation so the model ends where it began.
        target.localEulerAngles = startEuler;
    }

    // Map a random number onto the cumulative weights.
    int WeightedPick(float[] cumulative, float total)
    {
        float r = Random.value * total; // [0,total)
        for (int i = 0; i < cumulative.Length; i++)
            if (r < cumulative[i]) return i;
        return cumulative.Length - 1;
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

        carName.text = currentCar.car_name + (currentCarIndex > 0 ? " (" + currentCarIndex + ")" : "");
        carPrice.text = currentCar.price.ToString("N0") + " cr";
        carPowerplant.text = currentCar.powerplant;

        if (_spawnedModel != null)
        {
            Destroy(_spawnedModel);
            _spawnedModel = null;
        }

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

        Debug.Log(Time.time + " NUM OF " + currentCar.name + "s OWNED: " + numOfThisCarTypeOwned);

        // Ensure Car.InitializeCar signature supports string carType
        if (lootboxCar) _car.RandomizeCar(currentCarType, currentCarIndex, isOwned);
        else _car.InitializeCar(currentCarType, currentCarIndex, isOwned);

        _spawnedModel = Instantiate(currentCar.carModel, currentCar.turntablePositon, carHolder.rotation, carHolder);
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
}
