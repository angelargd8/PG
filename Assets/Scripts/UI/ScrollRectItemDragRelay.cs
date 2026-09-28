using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// EventTrigger on an option intercepts drag events, even with only sound callbacks.
[DisallowMultipleComponent]
public sealed class ScrollRectItemDragRelay : MonoBehaviour,
    IInitializePotentialDragHandler, IDragHandler, IScrollHandler
{
    private ScrollRect scrollRect;
    private void OnEnable() => scrollRect = GetComponentInParent<ScrollRect>();
    // TMP instantiates each active item without a parent, then assigns Content.
    private void OnTransformParentChanged() => scrollRect = GetComponentInParent<ScrollRect>();

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (scrollRect == null || !scrollRect.isActiveAndEnabled ||
            eventData.button != PointerEventData.InputButton.Left) return;
        // The input module owns the drag and cancels the Toggle click on movement.
        eventData.pointerDrag = scrollRect.gameObject;
        scrollRect.OnInitializePotentialDrag(eventData);
    }

    // Makes the relay discoverable before initialization redirects the gesture.
    public void OnDrag(PointerEventData eventData) { }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null && scrollRect.isActiveAndEnabled)
            scrollRect.OnScroll(eventData);
    }
}
