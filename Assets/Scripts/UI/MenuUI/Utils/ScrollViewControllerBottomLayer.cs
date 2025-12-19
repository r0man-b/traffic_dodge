using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollViewControllerBottomLayer : MonoBehaviour
{
    public int scaleFactor = 300;
    private bool activated;
    private float tapMinXThreshold = 0f;
    private float originalThreshold = 10f;
    private bool firstTouch = true;
    public GarageUIManager garageUI;
    public MenuSounds menuSounds;

    [Header("Scrollables")]
    public GameObject scrollbar;
    public DisableableScrollRect scrollRect;

    [Space(10)]
    [Header("Cached Buttons & Button Components")]
    public List<Button> buttons = new List<Button>();
    public List<Transform> buttonTransforms = new List<Transform>();
    public List<Image> buttonImages = new List<Image>();
    public List<TextMeshProUGUI> buttonNames = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> buttonPrices = new List<TextMeshProUGUI>();
    public Button[] directionButtons;
    public Button backButton;

    // Scroll variables.
    private Scrollbar scrollbarComponent;
    private float scrollPosition = 0;
    private float sideScrollSpeed = 0.0008f; // Speed at which to scroll when side tap is held

    // Button variables.
    private float[] buttonPositions;
    private int buttonIndexToChangeTo;
    private bool currentlyChangingButtons;
    private int highlightedButtonIndex = 0;
    private int oldHighlightedButtonIndex = 0;
    private bool gotHighlightedButtonDimensions;
    private Vector3[] highlightedButtonCorners;

    // Input variables.
    private Vector2 tapStartPosition;
    private Vector2 tapEndPosition;
    private bool isTapHeld = false; // To track if the tap is being held on the side.
    private float sideTapTime = 0f; // Duration of the side tap.

    // Other variables.
    private float distance;
    private float time;
    private readonly float snapVelocityThreshold = 10000;
    private Vector3[] scrollRectCorners;
    private int tickCounter = 0;
    private bool endValuesSet;
    //private AudioSource clickSource;
    private bool clickplayed;

    public void Initialize()
    {
        // Store initial button positions in the buttonPositions array.
        buttonPositions = new float[buttonTransforms.Count];
        distance = 1f / (buttonPositions.Length - 1f);
        for (int i = 0; i < buttonPositions.Length; i++)
        {
            buttonPositions[i] = distance * i;
        }

        // Cache the scrollbar component for later use.
        scrollbarComponent = scrollbar.GetComponent<Scrollbar>();

        // Get the corners of the ScrollRect in world space.
        scrollRectCorners = new Vector3[4];
        scrollRect.GetComponent<RectTransform>().GetWorldCorners(scrollRectCorners);

        // Initialize the highlighted button corners array for later use.
        highlightedButtonCorners = new Vector3[4];

        // Update scrolling speed based on number of buttons.
        sideScrollSpeed = 0.03f / buttons.Count;

        activated = true;
    }

    void Update()
    {
        if (!activated) return;

        if (!gotHighlightedButtonDimensions)
        {
            buttonTransforms[highlightedButtonIndex].GetComponent<RectTransform>().GetWorldCorners(highlightedButtonCorners);
            gotHighlightedButtonDimensions = true;
        }

        if (Input.GetKeyDown(KeyCode.Return)) // KeyCode.Return is the Enter key
        {
            buttons[highlightedButtonIndex].onClick.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // KeyCode.Return is the Enter key
        {
            ChangeButton(true);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeButton(false);
        }
        if (Input.GetKeyUp(KeyCode.B))
        {
            backButton.onClick.Invoke(); // Invoke the onClick event of the back button
            return;
        }

        // If user clicked on left/right arrows, manually scroll through the buttons.
        if (currentlyChangingButtons)
        {
            scrollbarComponent.value = Mathf.Lerp(scrollbarComponent.value, buttonPositions[buttonIndexToChangeTo], 0.03f * Time.deltaTime * scaleFactor);
            time += Time.deltaTime;

            //if (Mathf.Approximately(scrollbarComponent.value, buttonPositions[buttonIndexToChangeTo]))
            if (time > 0.2f)
            {
                time = 0;
                currentlyChangingButtons = false;
            }
        }

        // Check for the beginning of a tap.
        if (
                // Check for touch inputs first.
                (
                    (
                    Input.touchCount > 0 &&
                    Input.touches[0].phase == TouchPhase.Began &&
                    Input.touches[0].position.y <= scrollRectCorners[1].y // Make sure we are touching the scrollable viewport.
                    )
                    && gotHighlightedButtonDimensions &&
                    // Ignore the touch if we are touching the highlighted button.
                    (
                    Input.touches[0].position.x <= highlightedButtonCorners[0].x ||
                    Input.touches[0].position.x >= highlightedButtonCorners[3].x
                    )
                )
                // Otherwise, check for mouse inputs.
                ||
                (
                    (
                    Input.GetMouseButtonDown(0) &&
                    Input.mousePosition.y <= scrollRectCorners[1].y
                    )
                    && gotHighlightedButtonDimensions &&
                    (
                    Input.mousePosition.x <= highlightedButtonCorners[0].x ||
                    Input.mousePosition.x >= highlightedButtonCorners[3].x
                    )
                )
            )
        {
            clickplayed = false;
            firstTouch = false;
            Vector2 startPosition = Input.GetMouseButtonDown(0) ? Input.mousePosition : new Vector2(Input.touches[0].position.x, Input.touches[0].position.y);
            bool isLeftSide = startPosition.x < (scrollRectCorners[2].x - scrollRectCorners[0].x) / 2;
            tapStartPosition = startPosition;
            isTapHeld = true;
            sideTapTime = 0f; // Reset tap duration.
        }

        // Check if the tap is being held.
        if (isTapHeld && (Input.GetMouseButton(0) || Input.touchCount > 0))
        {
            
            Vector2 currentTapPosition = Input.GetMouseButton(0) ? Input.mousePosition : new Vector2(Input.touches[0].position.x, Input.touches[0].position.y);
            bool isLeftSide = tapStartPosition.x < (scrollRectCorners[2].x - scrollRectCorners[0].x) / 2;

            if (Mathf.Abs(tapStartPosition.x - currentTapPosition.x) > tapMinXThreshold)
            {
                isTapHeld = false;
                scrollRect.allowDrag = true;
                tapMinXThreshold = originalThreshold;
                scrollbarComponent.value = buttonPositions[highlightedButtonIndex];
                return; // Exit early if it's more of a drag than a tap.
            }

            sideTapTime += Time.deltaTime;
 
            if (sideTapTime > 0.15f) // Arbitrary time after which to start scrolling.
            {
                ScrollThroughButtons(isLeftSide);
                scrollRect.allowDrag = false;
                tapMinXThreshold = 5000f;
                if (isLeftSide)
                    ExecuteEvents.Execute(directionButtons[0].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                else
                    ExecuteEvents.Execute(directionButtons[1].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                //currentlyChangingButtons = true;
            }
        }

        // If user is swiping or has let go of swipe, compare current scroll position to see if highlighted button needs to change.
        if (Input.touchCount > 0 || Input.GetMouseButton(0) || scrollRect.velocity.sqrMagnitude > snapVelocityThreshold)
        {
            scrollPosition = scrollbarComponent.value;
            highlightedButtonIndex = Mathf.RoundToInt(scrollPosition / distance);
            highlightedButtonIndex = Mathf.Clamp(highlightedButtonIndex, 0, buttons.Count - 1);

            // Once the index of the highlighted button has changed, highlight the new middle button.
            if (highlightedButtonIndex != oldHighlightedButtonIndex)
            {
                menuSounds.PlayButtonSwitch();
                clickplayed = true;
                HighlightMiddleButton();
                oldHighlightedButtonIndex = highlightedButtonIndex;
                garageUI.ChangeItem(highlightedButtonIndex);
            }
        }
        else if (!firstTouch)//if (!currentlyChangingButtons)// Once the swipe velocity becomes lower than the threshold, snap the scroll to nearest button.
        {
            scrollbarComponent.value = Mathf.Lerp(scrollbarComponent.value, buttonPositions[highlightedButtonIndex], 0.01f * Time.deltaTime * scaleFactor);
        }

        // Change buttons to the left or right if user briefly tapped.
        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended) && !isTapHeld)
        {
            Vector2 endPosition = Input.GetMouseButtonUp(0) ? new Vector2(Input.mousePosition.x, Input.mousePosition.y) : new Vector2(Input.touches[0].position.x, Input.touches[0].position.y);
            tapEndPosition = endPosition;
            isTapHeld = false;
            scrollRect.allowDrag = true;
            tapMinXThreshold = originalThreshold;

            if (Mathf.Abs(tapStartPosition.x - tapEndPosition.x) < tapMinXThreshold) // Ignore tap end if the user scrolled.
            {
                float scrollRectWidth = scrollRectCorners[2].x - scrollRectCorners[0].x;
                if (tapStartPosition.x < scrollRectCorners[0].x + scrollRectWidth / 2)
                {
                    // Programatically 'click' the left button.
                    ExecuteEvents.Execute(directionButtons[0].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

                    // Begin the button change if we are within bounds.
                    if (highlightedButtonIndex - 1 >= 0)
                    {
                        buttonIndexToChangeTo = highlightedButtonIndex - 1;
                        highlightedButtonIndex = buttonIndexToChangeTo;
                        time = 0;
                        scrollPosition = (buttonPositions[buttonIndexToChangeTo]);
                        currentlyChangingButtons = true;
                        if (!clickplayed)
                            menuSounds.PlayButtonSwitch();
                        HighlightMiddleButton();
                        garageUI.ChangeItem(highlightedButtonIndex);
                    }
                    else return;
                }
                else
                {
                    // Programatically 'click' the right button.
                    ExecuteEvents.Execute(directionButtons[1].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

                    // Begin the button change if we are within bounds.
                    if (highlightedButtonIndex + 1 < buttons.Count)
                    {
                        buttonIndexToChangeTo = highlightedButtonIndex + 1;
                        highlightedButtonIndex = buttonIndexToChangeTo;
                        time = 0;
                        scrollPosition = (buttonPositions[buttonIndexToChangeTo]);
                        currentlyChangingButtons = true;
                        if (!clickplayed)
                            menuSounds.PlayButtonSwitch();
                        HighlightMiddleButton();
                        garageUI.ChangeItem(highlightedButtonIndex);
                    }
                    else return;
                }
            }
        }


        // Increase highlighted button scale until it is greater than 1.26.
        if (!(buttonTransforms[highlightedButtonIndex].localScale.x > 1.26f))
        {
            endValuesSet = false;
            LerpButtonScale();
            tickCounter++;
            if (tickCounter == 4)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                tickCounter = 0;
            }
        }
        else if (!endValuesSet)
        {
            if (!gotHighlightedButtonDimensions)
            {
                buttonTransforms[highlightedButtonIndex].GetComponent<RectTransform>().GetWorldCorners(highlightedButtonCorners);
                gotHighlightedButtonDimensions = true;
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                if (i == highlightedButtonIndex) buttons[i].interactable = true;
                else buttons[i].interactable = false;
            }
            if (tickCounter != 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                tickCounter = 0;
            }
            endValuesSet = true;
        }
    }

    public void SetScrollPosition(int position)
    {
        highlightedButtonIndex = position;
        oldHighlightedButtonIndex = highlightedButtonIndex;
        scrollRect.horizontalNormalizedPosition = buttonPositions[highlightedButtonIndex];
        
        scrollRect.velocity = new Vector2(0, 0);

        for (int i = 0; i < buttonTransforms.Count; i++)
        {
            if (i == highlightedButtonIndex)
            {
                AdjustAlpha(buttonImages[i], buttonNames[i], buttonPrices[i], 255f);
                buttonTransforms[i].localScale = new Vector2(1.26875f, 1.26875f);
                buttons[i].interactable = true;
            }
            else if (i == highlightedButtonIndex + 1 || i == highlightedButtonIndex - 1)
            {
                AdjustAlpha(buttonImages[i], buttonNames[i], buttonPrices[i], 100f);
                buttonTransforms[i].localScale = new Vector2(1f, 1f);
                buttons[i].interactable = false;
            }
            else
            {
                AdjustAlpha(buttonImages[i], buttonNames[i], buttonPrices[i], 25f);
                buttonTransforms[i].localScale = new Vector2(1f, 1f);
                buttons[i].interactable = false;
            }

        }
        activated = true;
        scrollbarComponent.value = buttonPositions[highlightedButtonIndex];
        //Debug.Log(scrollbarComponent.value);
    }

    public void ResetScroll()
    {
        buttons.Clear();
        buttonTransforms.Clear();
        buttonImages.Clear();
        buttonNames.Clear();
        buttonPrices.Clear();
        activated = false;
        firstTouch = true;
        gotHighlightedButtonDimensions = false;
        scrollRect.gameObject.SetActive(false);
    }

    // Set alpha & interactability of middle button & adjacent buttons.
    private void HighlightMiddleButton()
    {

        int i = highlightedButtonIndex;
        AdjustAlpha(buttonImages[i], buttonNames[i], buttonPrices[i], 255f);

        if (i - 1 >= 0)
        {
            AdjustAlpha(buttonImages[i - 1], buttonNames[i - 1], buttonPrices[i - 1], 100f);
            if (i - 2 >= 0)
            {
                AdjustAlpha(buttonImages[i - 2], buttonNames[i - 2], buttonPrices[i - 2], 25f);
            }
        }

        if (i + 1 < buttonTransforms.Count)
        {
            AdjustAlpha(buttonImages[i + 1], buttonNames[i + 1], buttonPrices[i + 1], 100f);
            if (i + 2 < buttonTransforms.Count)
            {
                AdjustAlpha(buttonImages[i + 2], buttonNames[i + 2], buttonPrices[i + 2], 25f);
            }
        }
    }

    // Lerp scale of middle button & adjacent buttons.
    private void LerpButtonScale()
    {
        for (int i = 0; i < buttonTransforms.Count; i++)
        {
            if (i != highlightedButtonIndex) buttonTransforms[i].localScale = Vector2.Lerp(buttonTransforms[i].localScale, new Vector2(1f, 1f), 0.04f * Time.deltaTime * scaleFactor);
            else buttonTransforms[i].localScale = Vector2.Lerp(buttonTransforms[i].localScale, new Vector2(1.26875f, 1.26875f), 0.04f * Time.deltaTime * scaleFactor);
        }
    }

    private void ScrollThroughButtons(bool isLeftSide)
    {
        if (isLeftSide && highlightedButtonIndex > 0)
        {
            buttonIndexToChangeTo = highlightedButtonIndex - 1;
            highlightedButtonIndex = buttonIndexToChangeTo;
        }
        else if (!isLeftSide && highlightedButtonIndex < buttons.Count - 1)
        {
            buttonIndexToChangeTo = highlightedButtonIndex + 1;
            highlightedButtonIndex = buttonIndexToChangeTo;
        }

        if (isLeftSide && scrollPosition > 0)
        {
            scrollPosition -= sideScrollSpeed * Time.deltaTime * scaleFactor;
        }
        else if (!isLeftSide && scrollPosition <= 1)
        {
            scrollPosition += sideScrollSpeed * Time.deltaTime * scaleFactor;
        }

        scrollbarComponent.value = scrollPosition;
    }

    void AdjustAlpha(Image image, TextMeshProUGUI name, TextMeshProUGUI price, float alpha)
    {
        if (image)
        {
            Color newColor = image.color;
            newColor.a = 2 * alpha / 255f;
            image.color = newColor;
        }
        if (name)
        {
            Color newColor = name.color;
            if (alpha != 255f)
                newColor.a = alpha / 4 / 255f;
            else
                newColor.a = alpha / 255f;
            name.color = newColor;
        }
        if (price)
        {
            Color newColor = price.color;
            if (alpha != 255f)
                newColor.a = alpha / 4 / 255f;
            else
                newColor.a = alpha / 255f;
            price.color = newColor;
        }
    }

    private void ChangeButton(bool left)
    {
        isTapHeld = false;
        scrollRect.allowDrag = true;
        tapMinXThreshold = originalThreshold;

        //if (Mathf.Abs(tapStartPosition.x - tapEndPosition.x) < tapMinXThreshold) // Ignore tap end if the user scrolled.
        {
            float scrollRectWidth = scrollRectCorners[2].x - scrollRectCorners[0].x;
            if (left)
            {
                // Programatically 'click' the left button.
                ExecuteEvents.Execute(directionButtons[0].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

                // Begin the button change if we are within bounds.
                if (highlightedButtonIndex - 1 >= 0)
                {
                    buttonIndexToChangeTo = highlightedButtonIndex - 1;
                    highlightedButtonIndex = buttonIndexToChangeTo;
                    time = 0;
                    scrollPosition = (buttonPositions[buttonIndexToChangeTo]);
                    currentlyChangingButtons = true;
                    if (!clickplayed)
                        menuSounds.PlayButtonSwitch();
                    HighlightMiddleButton();
                    garageUI.ChangeItem(highlightedButtonIndex);
                }
                else return;
            }
            else
            {
                // Programatically 'click' the right button.
                ExecuteEvents.Execute(directionButtons[1].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

                // Begin the button change if we are within bounds.
                if (highlightedButtonIndex + 1 < buttons.Count)
                {
                    buttonIndexToChangeTo = highlightedButtonIndex + 1;
                    highlightedButtonIndex = buttonIndexToChangeTo;
                    time = 0;
                    scrollPosition = (buttonPositions[buttonIndexToChangeTo]);
                    currentlyChangingButtons = true;
                    if (!clickplayed)
                        menuSounds.PlayButtonSwitch();
                    HighlightMiddleButton();
                    garageUI.ChangeItem(highlightedButtonIndex);
                }
                else return;
            }
        }
    }
}

