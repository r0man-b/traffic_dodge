using UnityEngine;
using TMPro;

public class NitroManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nitroText;

    private void OnEnable()
    {
        if (nitroText == null)
        {
            Debug.LogError("NitroManager: nitroText (TMP_Text) is not assigned.");
            return;
        }

        int currentNitro = SaveManager.Instance.SaveData.NitroCount;
        if (currentNitro < 0)
        {
            currentNitro = 0;
            SaveManager.Instance.SaveData.NitroCount = currentNitro;
            SaveManager.Instance.SaveGame();
        }

        UpdateUI(currentNitro);
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0) return true;

        int current = SaveManager.Instance.SaveData.NitroCount;
        if (current < cost) return false;

        int target = current - cost;
        Apply(target);
        return true;
    }

    public void ChangeNitro(int delta)
    {
        int current = SaveManager.Instance.SaveData.NitroCount;
        int target = current + delta;
        if (target < 0) target = 0;

        Apply(target);
    }

    public int GetNitro()
    {
        return SaveManager.Instance.SaveData.NitroCount;
    }

    private void Apply(int value)
    {
        SaveManager.Instance.SaveData.NitroCount = value;
        SaveManager.Instance.SaveGame();
        UpdateUI(value);
    }

    private void UpdateUI(int value)
    {
        if (nitroText != null)
        {
            nitroText.text = value.ToString();
        }
    }
}
