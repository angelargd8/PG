using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRHapticFeedback : MonoBehaviour
{
    // =========================
    // INSTANCE
    // =========================

    public static XRHapticFeedback Instance
    {
        get;
        private set;
    }


    // =========================
    // DEFAULT CONFIGURATION
    // =========================

    [Header("Default Haptics")]

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultAmplitude = 0.5f;

    [Min(0.01f)]
    [SerializeField]
    private float defaultDuration = 0.08f;


    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Debug.LogWarning(
                "[XRHapticFeedback] Ya existe una instancia.",
                this
            );

            return;
        }


        Instance = this;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =========================
    // PUBLIC
    // =========================

    public void Pulse(
        XRNode hand
    )
    {
        Pulse(
            hand,
            defaultAmplitude,
            defaultDuration
        );
    }


    public void Pulse(
        XRNode hand,
        float amplitude,
        float duration
    )
    {
        InputDevice device =
            InputDevices.GetDeviceAtXRNode(
                hand
            );


        if (!device.isValid)
        {
            return;
        }


        if (
            !device.TryGetHapticCapabilities(
                out HapticCapabilities capabilities
            )
        )
        {
            return;
        }


        if (!capabilities.supportsImpulse)
        {
            return;
        }


        device.SendHapticImpulse(
            0,
            Mathf.Clamp01(amplitude),
            Mathf.Max(0.01f, duration)
        );
    }


    // =========================
    // SHORTCUTS
    // =========================

    public void PulseRight(
        float amplitude,
        float duration
    )
    {
        Pulse(
            XRNode.RightHand,
            amplitude,
            duration
        );
    }


    public void PulseLeft(
        float amplitude,
        float duration
    )
    {
        Pulse(
            XRNode.LeftHand,
            amplitude,
            duration
        );
    }
}