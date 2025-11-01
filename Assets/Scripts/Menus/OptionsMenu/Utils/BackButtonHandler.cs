using UnityEngine;
using System.Collections;
using Settings; // Required to access GraphicsSettingsMenu

public class BackButtonHandler : MonoBehaviour
{
    public OptionsMenuAnimator optionsAnimator;           // Reference to the OptionsMenuAnimator
    public GraphicsSettingsMenu graphicsMenu;             // Reference to the GraphicsSettingsMenu
    public GameObject mainMenu;                           // Main menu to activate
    public GameObject optionsMenu;                        // Options menu to deactivate

    public void OnBackButtonPressed()
    {
        StartCoroutine(HandleBackButton());
    }

    private IEnumerator HandleBackButton()
    {
        // Check if the graphics menu is currently open
        if (optionsAnimator != null && optionsAnimator.IsGraphicsMenuOpen())
        {
            bool confirmed = false;
            bool popupClosed = false;

            graphicsMenu.DisplayPopup((bool userConfirmed) =>
            {
                confirmed = userConfirmed;
                popupClosed = true;
            });

            yield return new WaitUntil(() => popupClosed);
        }

        // Reset the options menu layout before leaving
        if (optionsAnimator != null)
        {
            optionsAnimator.ResetMenu();
        }

        // Proceed with closing options and opening main menu
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
