using UnityEngine;
using UnityEngine.UI;

// World-space canvas follows the XR camera and renders in both eyes. No screen-space overlay camera.
public sealed class ExperienceTransitionView : MonoBehaviour
{
    private GameObject overlay;
    private Image image;
    private Material material;
    private Camera targetCamera;

    public void SetFade(Color color, float opacity, Shader shader)
    {
        if (opacity <= 0) { Clear(); return; }
        if (shader == null) { Debug.LogError("Camera Fade necesita Overlay Shader.", this); return; }
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        if (overlay == null)
        {
            overlay = new GameObject("Experience Camera Fade", typeof(RectTransform), typeof(Canvas));
            overlay.transform.SetParent(targetCamera.transform, false);
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = short.MaxValue;
            canvas.worldCamera = targetCamera;
            image = overlay.AddComponent<Image>();
            image.raycastTarget = false;
        }
        if (material == null || material.shader != shader)
        {
            if (material != null) Destroy(material);
            material = new Material(shader);
            image.material = material;
        }
        // Oversized quad covers asymmetric stereo frusta as well as the desktop view.
        float distance = targetCamera.nearClipPlane + 0.05f;
        overlay.transform.localPosition = new Vector3(0, 0, distance);
        ((RectTransform)overlay.transform).sizeDelta = Vector2.one * (distance * 20);
        color.a *= Mathf.Clamp01(opacity);
        image.color = color;
        overlay.SetActive(true);
    }

    public void Clear() { if (overlay != null) overlay.SetActive(false); }
    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
        if (material != null) Destroy(material);
    }
}
