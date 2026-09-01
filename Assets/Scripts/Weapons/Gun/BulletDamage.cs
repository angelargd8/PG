using UnityEngine;

[DisallowMultipleComponent]
public sealed class BulletDamage : MonoBehaviour
{
    [SerializeField]
    private int damage = 1;


    private PooledBullet pooledBullet;


    private void Awake()
    {
        pooledBullet =
            GetComponent<PooledBullet>();
    }


    private void OnTriggerEnter(
        Collider other
    )
    {
        EnemyController enemy =
            other.GetComponentInParent<EnemyController>();


        if (enemy == null)
        {
            return;
        }


        enemy.TakeDamage(
            damage
        );


        pooledBullet.Despawn();
    }
}