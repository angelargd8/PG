using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

// TMP clones the template into the open list and disables the original template.
[AddComponentMenu("UI/XR Dropdown Scroll Rect")]
public sealed class XRDropdownScrollRect : ScrollRect
{
    [SerializeField, Min(0)] private float thumbstickScrollSpeed = 350f;
    [SerializeField, Range(0, 0.95f)] private float thumbstickDeadzone = 0.2f;
    private bool pointerDragging;

    private float ReadVerticalInput()
    {
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 left);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand)
            .TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 right);
        return Mathf.Abs(left.y) >= Mathf.Abs(right.y) ? left.y : right.y;
    }

    private void Update() => ScrollWithThumbstick(ReadVerticalInput(), Time.unscaledDeltaTime);

    internal void ScrollWithThumbstick(float axis, float deltaTime)
    {
        if (!IsActive() || !vertical || content == null || pointerDragging ||
            Mathf.Abs(axis) <= thumbstickDeadzone) return;
        RectTransform view = viewport != null ? viewport : (RectTransform)transform;
        float range = content.rect.height - view.rect.height;
        if (range <= 0) return;
        float strength = Mathf.InverseLerp(thumbstickDeadzone, 1, Mathf.Abs(axis));
        StopMovement();
        verticalNormalizedPosition = Mathf.Clamp01(verticalNormalizedPosition +
            Mathf.Sign(axis) * strength * thumbstickScrollSpeed * deltaTime / range);
    }

    public override void OnScroll(PointerEventData eventData)
    {
        // XRI may also send a scroll event for the same stick. Consume it once.
        if (pointerDragging || Mathf.Abs(ReadVerticalInput()) > thumbstickDeadzone) return;
        base.OnScroll(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsActive()) return;
        pointerDragging = true;
        // Include movement between the press and crossing the drag threshold.
        Vector2 currentPosition = eventData.position;
        eventData.position = eventData.pressPosition;
        base.OnBeginDrag(eventData);
        eventData.position = currentPosition;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        if (eventData.button == PointerEventData.InputButton.Left) pointerDragging = false;
    }

    protected override void OnDisable()
    {
        pointerDragging = false;
        base.OnDisable();
    }
}
