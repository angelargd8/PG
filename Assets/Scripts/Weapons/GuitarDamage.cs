using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class GuitarDamage : MonoBehaviour
{
    // =========================
    // DAMAGE
    // =========================

    [Header("Damage")]

    [Min(1)]
    [SerializeField]
    private int damage = 1;


    // =========================
    // HAPTICS
    // =========================

    [Header("Haptic Feedback")]

    [SerializeField]
    private XRNode hapticHand =
        XRNode.RightHand;


    [Range(0f, 1f)]
    [SerializeField]
    private float hapticAmplitude = 0.6f;


    [Min(0.01f)]
    [SerializeField]
    private float hapticDuration = 0.08f;


    [Tooltip(
        "Evita varias vibraciones seguidas " +
        "por múltiples colliders."
    )]
    [Min(0f)]
    [SerializeField]
    private float hapticCooldown = 0.1f;


    // =========================
    // RUNTIME
    // =========================

    private float nextHapticTime;


    // =========================
    // TRIGGER
    // =========================

    private void OnTriggerEnter(
        Collider other
    )
    {
        HandleHit(
            other
        );
    }


    // =========================
    // COLLISION
    // =========================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        HandleHit(
            collision.collider
        );
    }


    // =========================
    // HIT
    // =========================

    private void HandleHit(
        Collider other
    )
    {
        if (other == null)
        {
            return;
        }


        EnemyController enemy =
            other.GetComponentInParent<
                EnemyController>();


        if (enemy == null)
        {
            return;
        }


        // =========================
        // DAMAGE
        // =========================

        enemy.TakeDamage(
            damage
        );


        // =========================
        // HAPTIC
        // =========================

        PlayHaptic();
    }


    // =========================
    // HAPTIC
    // =========================

    private void PlayHaptic()
    {
        if (
            Time.time <
            nextHapticTime
        )
        {
            return;
        }


        nextHapticTime =
            Time.time +
            hapticCooldown;


        XRHapticFeedback haptics =
            XRHapticFeedback.Instance;


        if (haptics == null)
        {
            Debug.LogWarning(
                "[GuitarDamage] No se encontró " +
                "XRHapticFeedback.",
                this
            );

            return;
        }


        haptics.Pulse(
            hapticHand,
            hapticAmplitude,
            hapticDuration
        );
    }
}