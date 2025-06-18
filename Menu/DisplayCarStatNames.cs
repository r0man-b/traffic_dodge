using UnityEngine;
using TMPro;

public class DisplayCarStatNames : MonoBehaviour
{
    private TMP_Text statText;

    private void Awake()
    {
        // Get the TMP_Text component attached to this GameObject
        statText = GetComponent<TMP_Text>();

        // Load unit preference from SaveData
        bool isImperial = SaveManager.Instance.SaveData.ImperialUnits;

        // Set the correct text
        statText.text = isImperial ?
            "PRICE:\nTOP SPEED:\nHP:\n0-60:\nLIVES:\nENGINE:" :
            "PRICE:\nTOP SPEED:\nHP:\n0-100:\nLIVES:\nENGINE:";
    }
}
