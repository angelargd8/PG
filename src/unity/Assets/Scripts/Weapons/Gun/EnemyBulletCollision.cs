using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyBulletCollision : MonoBehaviour
{
    private int playerProjectileLayer;

    private PooledBullet pooledBullet;


    private void Awake()
    {
        pooledBullet =
            GetComponent<PooledBullet>();

        playerProjectileLayer =
            LayerMask.NameToLayer(
                "Projectile"
            );
    }


    private void OnTriggerEnter(
        Collider other
    )
    {
        if (other.gameObject.layer !=
            playerProjectileLayer)
        {
            return;
        }


        pooledBullet.Despawn();
    }
}