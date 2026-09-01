using UnityEngine;
using UnityEngine.Pool;
using Unity.Profiling;
using System.Collections;

public class EnemyPool :
    MonoBehaviour,
    IExperiencePreloadable
{
    // =========================
    // PROFILER
    // =========================

    private static readonly ProfilerMarker GetMarker =
        new ProfilerMarker("EnemyPool.Get");

    private static readonly ProfilerMarker ReleaseMarker =
        new ProfilerMarker("EnemyPool.Release");

    private static readonly ProfilerMarker CreateMarker =
        new ProfilerMarker("EnemyPool.Instantiate");


    // =========================
    // POOL
    // =========================

    [Header("Pool")]

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private int defaultCapacity = 12;

    [SerializeField]
    private int maxSize = 30;

    [Tooltip(
        "Cantidad de enemigos que se crearán al iniciar."
    )]
    [SerializeField]
    private int prewarmCount = 12;


    // =========================
    // COMBAT
    // =========================

    [Header("Enemy Combat")]

    [SerializeField]
    private BulletPool enemyBulletPool;


    private Transform playerTarget;


    // =========================
    // RUNTIME
    // =========================

    private ObjectPool<GameObject> pool;

    private bool isPrewarmed;


    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        ResolveCombatReferences();


        pool = new ObjectPool<GameObject>(
            CreateEnemy,
            OnGetEnemy,
            OnReleaseEnemy,
            OnDestroyEnemy,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }


    // =========================
    // REFERENCES
    // =========================

    private void ResolveCombatReferences()
    {
        PlayerTargetProvider provider =
            PlayerTargetProvider.Instance;


        if (provider != null)
        {
            playerTarget =
                provider.EnemyTarget;
        }
        else
        {
            Debug.LogWarning(
                "[EnemyPool] No se encontró " +
                "PlayerTargetProvider.",
                this
            );
        }


        if (enemyPrefab == null)
        {
            Debug.LogError(
                "[EnemyPool] No se asignó Enemy Prefab.",
                this
            );

            return;
        }


        EnemyShooter shooter =
            enemyPrefab.GetComponent<EnemyShooter>();


        if (
            shooter != null &&
            enemyBulletPool == null
        )
        {
            Debug.LogError(
                "[EnemyPool] El prefab tiene EnemyShooter " +
                "pero no se asignó Enemy Bullet Pool.",
                this
            );
        }
    }


    // =========================
    // PRELOAD
    // =========================

    public IEnumerator Preload()
    {
        if (isPrewarmed)
        {
            yield break;
        }


        int amount =
            Mathf.Clamp(
                prewarmCount,
                0,
                maxSize
            );


        GameObject[] enemies =
            new GameObject[amount];


        for (int i = 0; i < amount; i++)
        {
            enemies[i] =
                pool.Get();


            // Cada 2 enemigos dejamos
            // pasar un frame.
            if ((i + 1) % 2 == 0)
            {
                yield return null;
            }
        }


        for (int i = 0; i < amount; i++)
        {
            pool.Release(
                enemies[i]
            );
        }


        isPrewarmed = true;


        Debug.Log(
            $"EnemyPool precalentado: " +
            $"{amount} enemigos.",
            this
        );
    }


    // =========================
    // CREATE
    // =========================

    private GameObject CreateEnemy()
    {
        using (CreateMarker.Auto())
        {
            GameObject enemy =
                Instantiate(enemyPrefab);


            EnemyController enemyController =
                enemy.GetComponent<EnemyController>();


            if (enemyController != null)
            {
                enemyController.SetPool(
                    this
                );
            }


            EnemyShooter shooter =
                enemy.GetComponent<EnemyShooter>();


            if (shooter != null)
            {
                shooter.Configure(
                    playerTarget,
                    enemyBulletPool
                );
            }


            EnemyFollowTarget followTarget =
                enemy.GetComponent<EnemyFollowTarget>();


            if (followTarget != null)
            {
                followTarget.SetTarget(
                    playerTarget
                );
            }


            // IMPORTANTE:
            // El objeto permanece desactivado
            // hasta que GetEnemy termine de
            // posicionarlo.
            enemy.SetActive(false);


            return enemy;
        }
    }


    // =========================
    // GET
    // =========================

    private void OnGetEnemy(
        GameObject enemy
    )
    {
        /*
         * No activamos aquí.
         *
         * GetEnemy configura primero:
         * - Parent
         * - Position
         * - Rotation
         *
         * y después activa.
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


            enemy.SetActive(true);


            return enemy;
        }
    }


    // =========================
    // RELEASE
    // =========================

    private void OnReleaseEnemy(
        GameObject enemy
    )
    {
        enemy.SetActive(false);


        enemy.transform.SetParent(
            transform,
            false
        );
    }


    public void ReleaseEnemy(
        GameObject enemy
    )
    {
        if (enemy == null)
        {
            return;
        }


        using (ReleaseMarker.Auto())
        {
            pool.Release(enemy);
        }
    }


    // =========================
    // DESTROY
    // =========================

    private void OnDestroyEnemy(
        GameObject enemy
    )
    {
        Destroy(enemy);
    }
}