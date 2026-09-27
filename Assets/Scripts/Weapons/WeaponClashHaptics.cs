using UnityEngine;

// Sample after the weapon followers have applied the current controller poses.
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class WeaponClashHaptics : MonoBehaviour
{
    [Header("Weapon Pair")]
    [Tooltip("Colliders del arma izquierda; excluye proyectiles y otros objetos del pool.")]
    [SerializeField] private Collider[] _leftColliders = new Collider[0];
    [SerializeField] private Collider[] _rightColliders = new Collider[0];

    [Header("Clash Feedback")]
    [Range(0f, 1f)] [SerializeField] private float _amplitude = 0.45f;
    [Min(0.01f)] [SerializeField] private float _duration = 0.07f;
    [Tooltip("Tiempo minimo entre choques. Mantener las armas juntas no repite la vibracion.")]
    [Min(0f)] [SerializeField] private float _cooldown = 0.12f;

    private bool _touching;
    private float _nextPulseTime;

    private void OnEnable()
    {
        _touching = false;
        _nextPulseTime = 0f;
    }

    private void LateUpdate()
    {
        bool touching = WeaponsOverlap();
        bool entered = touching && !_touching;
        _touching = touching;

        // Track contact during pause, but never queue feedback for resuming.
        if (!entered || Time.timeScale <= 0f || AudioListener.pause ||
            Time.unscaledTime < _nextPulseTime) return;

        XRHapticFeedback haptics = XRHapticFeedback.Instance;
        if (haptics == null) return;

        _nextPulseTime = Time.unscaledTime + Mathf.Max(0f, _cooldown);
        float amplitude = Mathf.Clamp01(_amplitude);
        float duration = Mathf.Max(0.01f, _duration);
        haptics.PulseLeft(amplitude, duration);
        haptics.PulseRight(amplitude, duration);
    }

    private bool WeaponsOverlap()
    {
        if (_leftColliders == null || _rightColliders == null) return false;

        foreach (Collider left in _leftColliders)
        {
            if (!CanDetect(left)) continue;
            foreach (Collider right in _rightColliders)
            {
                if (left == right || !CanDetect(right)) continue;

                // Explicit poses work with LateUpdate tracking, solid colliders,
                // triggers and kinematic bodies without changing their physics.
                if (Physics.ComputePenetration(
                    left, left.transform.position, left.transform.rotation,
                    right, right.transform.position, right.transform.rotation,
                    out _, out _)) return true;
            }
        }

        return false;
    }

    private static bool CanDetect(Collider collider)
    {
        return collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
    }
}
