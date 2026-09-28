using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class BulletPool : MonoBehaviour, IExperiencePreloadable, IExperienceRuntime
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
    private readonly HashSet<PooledBullet> leased = new();
    private readonly List<PooledBullet> returnBuffer = new();
    private bool isDisabling;

    public int ActiveBulletCount => leased.Count;
    public int InactiveBulletCount => pool?.CountInactive ?? 0;

    [ContextMenu("Log Bullet Pool State")]
    private void LogPoolState() => Debug.Log(
        $"[BulletPool] {name}: enabled={isActiveAndEnabled}, active={ActiveBulletCount}, " +
        $"available={InactiveBulletCount}, retainedLimit={maxSize} (not an ammunition limit).", this);

    public void BeginExperience() => EnsureInitialized();
    public void EndExperience() => ReturnAllToPool();

    private void Awake()
    {
        EnsureInitialized();
    }

    public System.Collections.IEnumerator Preload()
    {
        EnsureInitialized();
        yield break;
    }

    private void EnsureInitialized()
    {
        if (pool != null) return;
        if (bulletPrefab == null)
        {
            Debug.LogError(
                "[BulletPool] No se asign� el prefab de la bala.",
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
    public PooledBullet Spawn(
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime,
        Transform shooterRoot = null)
    {
        if (!isActiveAndEnabled || isDisabling) return null;
        if (pool == null)
        {
            Debug.LogWarning(
                "[BulletPool] El pool todavía no está inicializado.",
                this);

            return null;
        }

        PooledBullet bullet = pool.Get();
        leased.Add(bullet);

        bullet.Launch(
            this,
            position,
            rotation,
            speed,
            lifetime,
            shooterRoot
        );
        
        return bullet;
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

        if (leased.Remove(bullet)) pool.Release(bullet);
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
         * No se activa aqu� porque primero debemos colocar
         * la bala en Bullet Point. Launch() la activar�.
         */
    }

    private void OnReturnedToPool(PooledBullet bullet)
    {
        bullet.PrepareForPool();

        // OnDisable can run during a parent's SetActive(false). Unity forbids reparenting then.
        if (!isDisabling && bullet.transform.parent != poolRoot)
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

    private void ReturnAllToPool()
    {
        returnBuffer.Clear();
        returnBuffer.AddRange(leased);
        // Cancellation is not a missed shot: do not publish Despawned during scene teardown.
        foreach (PooledBullet bullet in returnBuffer)
            if (bullet != null) Release(bullet);
        leased.Clear();
        returnBuffer.Clear();
    }

    private void OnDisable()
    {
        // Normal transitions already called EndExperience while the hierarchy was active.
        // Also handle direct deactivation safely, without attaching to a parent being disabled.
        isDisabling = true;
        try { ReturnAllToPool(); }
        finally { isDisabling = false; }
    }

    private void OnDestroy() => pool?.Clear();
}
