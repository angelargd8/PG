using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        EnemyDanniel enemy =
            other.GetComponentInParent<EnemyDanniel>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }
}