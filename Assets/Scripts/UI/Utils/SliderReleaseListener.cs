using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Slider))]
public class SliderReleaseListener : MonoBehaviour, IEndDragHandler
{
    public Action<float> OnReleased;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnReleased?.Invoke(slider.value);
    }
}
