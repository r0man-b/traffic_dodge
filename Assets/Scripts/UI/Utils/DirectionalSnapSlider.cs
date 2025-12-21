using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class DirectionalSnapSlider : MonoBehaviour,
    IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private Slider slider;

    private float dragDirection;
    private int startWhole;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragDirection = 0f;
        startWhole = Mathf.RoundToInt(slider.value);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = slider.value - startWhole;

        if (Mathf.Abs(delta) > 0.0001f)
            dragDirection = Mathf.Sign(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(dragDirection) < 0.1f)
        {
            slider.value = startWhole;
            return;
        }

        int nextWhole = startWhole + (int)dragDirection;
        int afterNextWhole = nextWhole + (int)dragDirection;

        float current = slider.value;

        // Case 1: next whole NOT crossed → always snap forward
        if ((dragDirection > 0 && current < nextWhole) ||
            (dragDirection < 0 && current > nextWhole))
        {
            slider.value = Mathf.Clamp(nextWhole, slider.minValue, slider.maxValue);
            return;
        }

        // Case 2: next whole crossed → evaluate 25% rule
        float segmentStart = nextWhole;
        float segmentEnd = afterNextWhole;

        float progressed = Mathf.Abs(current - segmentStart);
        float segmentLength = Mathf.Abs(segmentEnd - segmentStart);

        float percent = progressed / segmentLength;

        if (percent <= 0.25f)
        {
            // Snap back to last crossed whole
            slider.value = Mathf.Clamp(nextWhole, slider.minValue, slider.maxValue);
        }
        else
        {
            // Continue forward
            slider.value = Mathf.Clamp(afterNextWhole, slider.minValue, slider.maxValue);
        }

        slider.onValueChanged.Invoke(slider.value);
    }
}
