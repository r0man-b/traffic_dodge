using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPart : MonoBehaviour
{
    public bool useCarPartData = true;
    public float customPrice;
    public float suspensionHeight;
    public float accelMaxValueUpgrade;
    public float accelIncreaseRateUpgrade;
    public int maxLives;
    public Material primaryColor;
    public Material secondaryColor;
    public Material rimColor;
    public Material primaryLight;
    public Material secondaryLight;
    public Material tailLight;

    public CarPartData carPartData;
    public float price => useCarPartData ? carPartData.price : customPrice;

    public void SetPrice(float newPrice)
    {
        customPrice = newPrice;
    }
}
