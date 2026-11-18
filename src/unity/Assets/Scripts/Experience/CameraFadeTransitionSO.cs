using UnityEngine;

[CreateAssetMenu(fileName = "CameraFade", menuName = "Scriptable Objects/Experience/Transitions/Camera Fade")]
public sealed class CameraFadeTransitionSO : ExperienceTransitionSO
{
    [Min(0)] [SerializeField] private float fadeOutSeconds = 0.25f;
    [Min(0)] [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private Color color = Color.black;
    [SerializeField] private Shader overlayShader;
    public override double LeadTime => fadeOutSeconds;
    public override double RevealTime => fadeInSeconds;
    public override void Present(ExperienceTransitionView view, double timeFromBoundary, bool waitingForScene)
    {
        float opacity = waitingForScene ? 1 : timeFromBoundary < 0
            ? (fadeOutSeconds <= 0 ? 0 : Mathf.Clamp01(1 + (float)timeFromBoundary / fadeOutSeconds))
            : (fadeInSeconds <= 0 ? 0 : Mathf.Clamp01(1 - (float)timeFromBoundary / fadeInSeconds));
        view.SetFade(color, Mathf.SmoothStep(0, 1, opacity), overlayShader);
    }
}
