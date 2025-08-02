using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoRaceMenu : MonoBehaviour
{
    private List<Transform> environmentButtons = new List<Transform>();
    private int currentSelectedIndex = -1;

    void Start()
    {
        // Use this GameObject (EnvironmentPanel) as the container
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            environmentButtons.Add(child);

            int index = i;

            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnEnvironmentSelected(index));
            }

            // Ensure all borders are initially hidden
            Transform border = child.Find("Border");
            if (border != null)
            {
                border.gameObject.SetActive(false);
            }
        }

        // Preselect saved environment if valid
        int savedIndex = SaveManager.Instance.SaveData.CurrentEnvironment;
        if (savedIndex >= 0 && savedIndex < environmentButtons.Count)
        {
            OnEnvironmentSelected(savedIndex);
        }
    }

    void OnEnvironmentSelected(int index)
    {
        if (index == currentSelectedIndex)
            return;

        // Disable previous border
        if (currentSelectedIndex >= 0 && currentSelectedIndex < environmentButtons.Count)
        {
            Transform previous = environmentButtons[currentSelectedIndex].Find("Border");
            if (previous != null)
                previous.gameObject.SetActive(false);
        }

        // Enable selected border
        Transform selected = environmentButtons[index].Find("Border");
        if (selected != null)
            selected.gameObject.SetActive(true);

        // Save selection
        currentSelectedIndex = index;
        SaveManager.Instance.SaveData.CurrentEnvironment = index;
    }
}
