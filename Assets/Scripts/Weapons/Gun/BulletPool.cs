using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class BulletPool : MonoBehaviour
{
    [Header("Bullet Prefab")]
    [SerializeField]
    private PooledBullet bulletPrefab;

    [SerializeField]
    private Transform poolRoot;

    [Header("Pool Configuration")]
    [Min(0)]
    [SerializeField]
    private int prewarmCount = 12;

    [Min(1)]
    [SerializeField]
    private int defaultCapacity = 16;

    [Min(1)]
    [SerializeField]
    private int maxSize = 64;

    private ObjectPool<PooledBullet> pool;

    private void Awake()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError(
                "[BulletPool] No se asignó el prefab de la bala.",
                this);

            enabled = false;
            return;
        }

        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        maxSize = Mathf.Max(maxSize, defaultCapacity);

        pool = new ObjectPool<PooledBullet>(
            CreateBullet,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        Prewarm();
    }

    /// <summary>
    /// Obtiene una bala del pool y la dispara.
    /// </summary>
    public void Spawn(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime)
    {
        if (pool == null)
        {
            Debug.LogWarning(
                "[BulletPool] El pool todavía no está inicializado.",
                this);

            return;
        }

        PooledBullet bullet = pool.Get();

        bullet.Launch(
            this,
            position,
            rotation,
            speed,
            lifetime
        );
    }

    /// <summary>
    /// Devuelve una bala utilizada al pool.
    /// </summary>
    internal void Release(PooledBullet bullet)
    {
        if (pool == null || bullet == null)
        {
            return;
        }

        pool.Release(bullet);
    }

    private PooledBullet CreateBullet()
    {
        PooledBullet bullet = Instantiate(
            bulletPrefab,
            poolRoot
        );

        bullet.gameObject.SetActive(false);

        return bullet;
    }

    private void OnTakeFromPool(PooledBullet bullet)
    {
        /*
         * No se activa aquí porque primero debemos colocar
         * la bala en Bullet Point. Launch() la activará.
         */
    }

    private void OnReturnedToPool(PooledBullet bullet)
    {
        bullet.PrepareForPool();

        bullet.transform.SetParent(poolRoot, false);
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(PooledBullet bullet)
    {
        if (bullet != null)
        {
            Destroy(bullet.gameObject);
        }
    }

    private void Prewarm()
    {
        int amount = Mathf.Clamp(
            prewarmCount,
            0,
            maxSize
        );

        List<PooledBullet> prewarmedBullets =
            new List<PooledBullet>(amount);

        for (int i = 0; i < amount; i++)
        {
            prewarmedBullets.Add(pool.Get());
        }

        foreach (PooledBullet bullet in prewarmedBullets)
        {
            pool.Release(bullet);
        }
    }
}