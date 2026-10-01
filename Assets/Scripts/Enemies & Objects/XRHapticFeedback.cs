using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRHapticFeedback : MonoBehaviour
{
    private float _nextConnectionCheck;
    private int _missingHands = -1;

    private void Update()
    {
        if (Instance != this || Time.unscaledTime < _nextConnectionCheck) return;
        _nextConnectionCheck = Time.unscaledTime + 1f;
        int missing = (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid ? 0 : 1) |
                      (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).isValid ? 0 : 2);
        if (missing == _missingHands) return;
        _missingHands = missing;
        if (missing != 0)
        {
            string hands = missing == 3 ? "izquierdo y derecho" : missing == 1 ? "izquierdo" : "derecho";
            Debug.LogWarning($"[XR] Control {hands} no disponible. La app continuara; se omitira la vibracion de las manos desconectadas hasta que vuelvan a conectarse.", this);
        }
    }

    public static XRHapticFeedback Instance
    {
        get;
        private set;
    }



    [Header("Default Haptics")]

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultAmplitude = 0.5f;

    [Min(0.01f)]
    [SerializeField]
    private float defaultDuration = 0.08f;


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
