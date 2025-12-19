using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MultiTargetButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Graphics affected ONLY while pressed")]
    public Graphic[] pressedTargets;

    [Tooltip("Pressed color override")]
    public Color pressedColor = Color.gray;

    Color[] originalColors;

    void Awake()
    {
        originalColors = new Color[pressedTargets.Length];
        for (int i = 0; i < pressedTargets.Length; i++)
            originalColors[i] = pressedTargets[i].color;
    }

    void SetPressed(bool pressed)
    {
        for (int i = 0; i < pressedTargets.Length; i++)
            pressedTargets[i].color = pressed
                ? pressedColor
                : originalColors[i];
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }
}
