using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class GuitarDamage : MonoBehaviour
{

    [Header("Damage")]

    [Min(1)]
    [SerializeField]
    private int damage = 1;


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


    private float nextHapticTime;


    private void OnTriggerEnter(
        Collider other
    )
    {
        HandleHit(
            other
        );
    }


    private void OnCollisionEnter(
        Collision collision
    )
    {
        HandleHit(
            collision.collider
        );
    }


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


        enemy.TakeDamage(
            damage
        );



        PlayHaptic();
    }



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