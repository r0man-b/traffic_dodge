using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public enum ItemType
    {
        Nitro,
        Credits
    }

    [Header("Item Characteristics")]
    public ItemType typeOfItem;     // Shows as dropdown in inspector
    public int quantityOfItem;
    public float priceOfItem;

    [Header("UI References")]
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI buttonTextOutline;

    private void Awake()
    {
        switch (typeOfItem)
        {
            case ItemType.Nitro:
                // Quantity formatting
                quantityText.text = quantityOfItem + " NITRO CANS:";

                // Button formatting
                string nitroPrice = "BUY: $" + priceOfItem.ToString("F2");
                buttonText.text = nitroPrice;
                buttonTextOutline.text = nitroPrice;
                break;

            case ItemType.Credits:
                // Quantity formatting with commas
                quantityText.text = quantityOfItem.ToString("N0") + " CR:";

                // Button formatting
                string creditPrice = "BUY: " + ((int)priceOfItem).ToString("N0");
                buttonText.text = creditPrice;
                buttonTextOutline.text = creditPrice;
                break;
        }
    }
}