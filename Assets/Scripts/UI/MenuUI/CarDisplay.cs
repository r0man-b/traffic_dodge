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
    private string currentCarType;   // switched to string
    private int currentCarIndex;
    private int numOfThisCarTypeOwned;
    private int sellPrice;
    private const string carsOwned = "CARS_OWNED";

    // -------------------- Loot-box style randomizer --------------------
    private int spinCount = 100;
    private float startDelay = 0.05f;   // fast at start
    private float endDelay = 0.7f;   // slow at end
    private float slowDownBias = 6f;
    public CarCollection carCollection;
    Coroutine _spinCo;

    // Public entrypoint
    public void RandomizeCar()
    {
        if (_spinCo != null) StopCoroutine(_spinCo);
        _spinCo = StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
    {
        // Rarity weights by top-level index in carCollection (sum = 100)
        // 0 Ferret 40, 1 Viking HD 20, 2 Cambrioleur 10, 3 Scythe 8, 4 Leischer 1000 6,
        // 5 Raion 5, 6 Katana 4, 7 Leischer TTRR 3, 8 Veloci 2, 9 Rapid Poision 1,
        // 10 Accenditore 0.5, 11 Cmiesta 0.25, 12 Intervento 0.2, 13 Valen 0.05
        float[] weights = { 40f, 20f, 10f, 8f, 6f, 5f, 4f, 3f, 2f, 1f, 0.5f, 0.25f, 0.2f, 0.05f };

        // Precompute cumulative distribution
        float total = 0f;
        float[] cum = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            total += weights[i];
            cum[i] = total;
        }

        for (int i = 0; i < spinCount; i++)
        {
            // Weighted pick of top-level type index (0..13)
            int typeIdx = WeightedPick(cum, total);

            // Guard against mismatched data
            typeIdx = Mathf.Clamp(typeIdx, 0, Mathf.Max(0, carCollection.carTypes.Count - 1));

            var bucket = carCollection.carTypes[typeIdx];
            int variantCount = Mathf.Max(1, bucket.items.Count);

            // Choose a random variant within that type (0..count-1)
            int variantIdx = Random.Range(0, variantCount);

            // Get the Car asset and its display name
            var carAsset = bucket.items[variantIdx] as Car;
            if (carAsset != null)
            {
                // Show it using the existing pipeline (updates UI, replaces model, etc.)
                DisplayCar(carAsset, string.IsNullOrWhiteSpace(carAsset.car_name) ? carAsset.name : carAsset.car_name, variantIdx, true);
            }

            // Ease-out timing: quick at start, slows toward the end
            float tLin = (spinCount <= 1) ? 1f : (float)i / (spinCount - 1);
            float tBias = Mathf.Pow(tLin, slowDownBias);        // stays near 0 for most of the run
            float delay = Mathf.Lerp(startDelay, endDelay, tBias);

            yield return new WaitForSecondsRealtime(delay);
        }

        _spinCo = null; // finished
    }

    // Map a random number onto the cumulative weights
    int WeightedPick(float[] cumulative, float total)
    {
        float r = Random.value * total; // [0,total)
        for (int i = 0; i < cumulative.Length; i++)
            if (r < cumulative[i]) return i;
        return cumulative.Length - 1;
    }

    // NOTE: Update caller sites to pass car type as string.
    // If your Car.InitializeCar previously accepted an int type, update it to accept string.
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

        if (carHolder.childCount > 0)
            Destroy(carHolder.GetChild(0).gameObject);

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

        GameObject carModel = Instantiate(currentCar.carModel, currentCar.turntablePositon, carHolder.rotation, carHolder);
        return carModel;
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
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;

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
}
