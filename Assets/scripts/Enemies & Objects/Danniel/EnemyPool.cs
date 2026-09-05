using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Profiling;

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

    [Tooltip(
        "Solo es necesario si el prefab " +
        "tiene EnemyShooter."
    )]
    [SerializeField]
    private BulletPool enemyBulletPool;


    // =========================
    // TARGET
    // =========================

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
        // Intentamos resolverlo ahora,
        // pero no es obligatorio que
        // ya exista durante Awake.
        TryResolvePlayerTarget();


        ValidateReferences();


        pool =
            new ObjectPool<GameObject>(
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
    // VALIDATION
    // =========================

    private void ValidateReferences()
    {
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
                "[EnemyPool] El prefab tiene " +
                "EnemyShooter pero no se asignó " +
                "Enemy Bullet Pool.",
                this
            );
        }
    }


    // =========================
    // PLAYER TARGET
    // =========================

    private bool TryResolvePlayerTarget()
    {
        if (playerTarget != null)
        {
            return true;
        }


        PlayerTargetProvider provider =
            PlayerTargetProvider.Instance;


        if (provider == null)
        {
            return false;
        }


        playerTarget =
            provider.EnemyTarget;


        return playerTarget != null;
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


        if (pool == null)
        {
            Debug.LogError(
                "[EnemyPool] El pool no está inicializado.",
                this
            );

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


        // =========================
        // GET
        // =========================

        for (
            int i = 0;
            i < amount;
            i++
        )
        {
            enemies[i] =
                pool.Get();


            // Cada 2 enemigos dejamos
            // pasar un frame para reducir
            // picos durante Loading.
            if (
                (i + 1) % 2 == 0
            )
            {
                yield return null;
            }
        }


        // =========================
        // RELEASE
        // =========================

        for (
            int i = 0;
            i < amount;
            i++
        )
        {
            if (enemies[i] == null)
            {
                continue;
            }


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
            if (enemyPrefab == null)
            {
                Debug.LogError(
                    "[EnemyPool] No se puede crear " +
                    "un enemigo porque Enemy Prefab " +
                    "es null.",
                    this
                );

                return null;
            }


            GameObject enemy =
                Instantiate(
                    enemyPrefab
                );


            // =========================
            // CONTROLLER
            // =========================

            EnemyController enemyController =
                enemy.GetComponent<
                    EnemyController>();


            if (enemyController != null)
            {
                enemyController.SetPool(
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    "[EnemyPool] El prefab no tiene " +
                    "EnemyController.",
                    enemy
                );
            }


            // No configuramos aquí:
            //
            // - EnemyMeleeAI
            // - EnemyShooter
            //
            // porque durante el prewarm
            // PlayerTargetProvider podría
            // todavía no estar disponible.


            enemy.SetActive(
                false
            );


            return enemy;
        }
    }


    // =========================
    // ON GET
    // =========================

    private void OnGetEnemy(
        GameObject enemy
    )
    {
        /*
         * No activamos aquí.
         *
         * GetEnemy configura primero:
         *
         * - Parent
         * - Position
         * - Rotation
         * - Target
         * - Combat
         *
         * y después activa.
         */
    }


    // =========================
    // GET ENEMY
    // =========================

    public GameObject GetEnemy(
        Transform parent,
        Vector3 position,
        Quaternion rotation
    )
    {
        using (GetMarker.Auto())
        {
            // =========================
            // TARGET
            // =========================

            bool hasTarget =
                TryResolvePlayerTarget();


            if (!hasTarget)
            {
                Debug.LogError(
                    "[EnemyPool] No se pudo resolver " +
                    "PlayerTargetProvider o EnemyTarget.",
                    this
                );
            }


            // =========================
            // GET
            // =========================

            GameObject enemy =
                pool.Get();


            if (enemy == null)
            {
                Debug.LogError(
                    "[EnemyPool] El pool devolvió null.",
                    this
                );

                return null;
            }


            Transform enemyTransform =
                enemy.transform;


            // =========================
            // PARENT
            // =========================

            enemyTransform.SetParent(
                parent,
                false
            );


            // =========================
            // POSITION
            // =========================

            enemyTransform.SetPositionAndRotation(
                position,
                rotation
            );


            // =========================
            // MELEE AI
            // =========================

            EnemyMeleeAI meleeAI =
                enemy.GetComponent<
                    EnemyMeleeAI>();


            if (meleeAI != null)
            {
                meleeAI.SetTarget(
                    playerTarget
                );
            }


            // =========================
            // SHOOTER
            // =========================

            EnemyShooter shooter =
                enemy.GetComponent<
                    EnemyShooter>();


            if (shooter != null)
            {
                shooter.Configure(
                    playerTarget,
                    enemyBulletPool
                );
            }


            // =========================
            // ACTIVATE
            // =========================

            enemy.SetActive(
                true
            );


            return enemy;
        }
    }


    // =========================
    // ON RELEASE
    // =========================

    private void OnReleaseEnemy(
        GameObject enemy
    )
    {
        if (enemy == null)
        {
            return;
        }


        enemy.SetActive(
            false
        );


        enemy.transform.SetParent(
            transform,
            false
        );
    }


    // =========================
    // RELEASE ENEMY
    // =========================

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
            pool.Release(
                enemy
            );
        }
    }


    // =========================
    // DESTROY
    // =========================

    private void OnDestroyEnemy(
        GameObject enemy
    )
    {
        if (enemy == null)
        {
            return;
        }


        Destroy(
            enemy
        );
    }
}