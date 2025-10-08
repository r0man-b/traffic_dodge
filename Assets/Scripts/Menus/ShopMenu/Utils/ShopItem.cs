using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public enum ItemType
    {
        Nitro,
        Credits,
        LootCrate
    }

    [Header("Item Characteristics")]
    public ItemType typeOfItem;     // Shows as dropdown in inspector
    public int quantityOfItem;
    public float priceOfItem;

    [Header("Loot Crate Characteristics ONLY")]
    public string itemDescription = "";
    [TextArea(1, 21)]
    public string leftDescription = "";
    [TextArea(1, 21)]
    public string rightDescription = "";
    public Texture2D crateTexture;
    [Header("CAR Loot Crate Characteristics ONLY")]
    public int carTier;

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
                string creditPrice = "  BUY: " + ((int)priceOfItem).ToString("N0");
                buttonText.text = creditPrice;
                buttonTextOutline.text = creditPrice;
                break;

            case ItemType.LootCrate:
                // Quantity text turns into item description for loot crates
                quantityText.text = itemDescription;

                // Button formatting
                string lootCratePrice = "     OPEN: " + ((int)priceOfItem).ToString("N0");
                buttonText.text = lootCratePrice;
                buttonTextOutline.text = lootCratePrice;
                break;
        }
    }
}