using UnityEngine;

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
    // TRIGGER
    // =========================

    private void OnTriggerEnter(
        Collider other
    )
    {
        TryDamage(
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
        TryDamage(
            collision.collider
        );
    }


    // =========================
    // DAMAGE
    // =========================

    private void TryDamage(
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
    }
}