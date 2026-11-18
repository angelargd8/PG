using UnityEngine;

// Keep the canvas in its additive scene so unloading that scene also removes the UI.
[RequireComponent(typeof(Canvas))]
public sealed class VRLoadingCanvasBinder : MonoBehaviour
{
    [Min(0.1f)] [SerializeField] private float distanceFromCamera = 1.5f;
    [Min(0.0001f)] [SerializeField] private float canvasScale = 0.001f;
    private Canvas canvas;
    private Camera targetCamera;

    private void Awake() => canvas = GetComponent<Canvas>();

    private void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        canvas.worldCamera = targetCamera;
        transform.SetPositionAndRotation(
            targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera,
            targetCamera.transform.rotation);
        transform.localScale = Vector3.one * canvasScale;
    }
}
