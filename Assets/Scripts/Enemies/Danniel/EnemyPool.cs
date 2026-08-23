using UnityEngine;
using UnityEngine.Pool;
using Unity.Profiling;

public class EnemyPool : MonoBehaviour
{
    private static readonly ProfilerMarker GetMarker =
        new ProfilerMarker("EnemyPool.Get");

    private static readonly ProfilerMarker ReleaseMarker =
        new ProfilerMarker("EnemyPool.Release");

    private static readonly ProfilerMarker CreateMarker =
        new ProfilerMarker("EnemyPool.Instantiate");


    [Header("Pool")]
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int defaultCapacity = 12;
    [SerializeField] private int maxSize = 30;

    [Tooltip("Cantidad de enemigos que se crearan al iniciar")]
    [SerializeField] private int prewarmCount = 12;


    private ObjectPool<GameObject> pool;


    private void Awake()
    {
        pool = new ObjectPool<GameObject>(
            CreateEnemy,
            OnGetEnemy,
            OnReleaseEnemy,
            OnDestroyEnemy,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        PrewarmPool();
    }


    // =========================
    // PREWARM
    // =========================

    private void PrewarmPool()
    {
        int amount =
            Mathf.Clamp(
                prewarmCount,
                0,
                maxSize
            );

        GameObject[] enemies =
            new GameObject[amount];

        // Crear todos los enemigos
        for (int i = 0; i < amount; i++)
        {
            enemies[i] = pool.Get();
        }

        // Devolverlos al pool
        for (int i = 0; i < amount; i++)
        {
            pool.Release(enemies[i]);
        }
    }


    // =========================
    // CREATE
    // =========================

    private GameObject CreateEnemy()
    {
        using (CreateMarker.Auto())
        {
            GameObject enemy =
                Instantiate(
                    enemyPrefab,
                    transform
                );

            EnemyDanniel enemyScript =
                enemy.GetComponent<EnemyDanniel>();

            if (enemyScript != null)
            {
                enemyScript.SetPool(this);
            }

            enemy.SetActive(false);

            return enemy;
        }
    }


    // =========================
    // GET
    // =========================

    private void OnGetEnemy(GameObject enemy)
    {
        /*
         * Primero se configura:
         * - Parent
         * - Position
         * - Rotation
         *
         * y despues hacemos SetActive(true) 
         */
    }


    public GameObject GetEnemy(
        Transform parent,
        Vector3 position,
        Quaternion rotation
    )
    {
        using (GetMarker.Auto())
        {
            GameObject enemy =
                pool.Get();

            Transform enemyTransform =
                enemy.transform;

            enemyTransform.SetParent(
                parent,
                false
            );

            enemyTransform.SetPositionAndRotation(
                position,
                rotation
            );

            // Activar solamente cuando
            // ya esta completamente configurado
            enemy.SetActive(true);

            return enemy;
        }
    }


    // =========================
    // RELEASE
    // =========================

    private void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);

        enemy.transform.SetParent(
            transform,
            false
        );
    }


    public void ReleaseEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        using (ReleaseMarker.Auto())
        {
            pool.Release(enemy);
        }
    }


    // =========================
    // DESTROY
    // =========================

    private void OnDestroyEnemy(GameObject enemy)
    {
        Destroy(enemy);
    }
}