using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Profiling;

public class EnemyPool :
    MonoBehaviour,
    IExperiencePreloadable
{

    private static readonly ProfilerMarker GetMarker =
        new ProfilerMarker("EnemyPool.Get");

    private static readonly ProfilerMarker ReleaseMarker =
        new ProfilerMarker("EnemyPool.Release");

    private static readonly ProfilerMarker CreateMarker =
        new ProfilerMarker("EnemyPool.Instantiate");



    [Header("Pool")]

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private int defaultCapacity = 12;

    [SerializeField]
    private int maxSize = 30;

    [Tooltip(
        "Cantidad de enemigos que se crear�n al iniciar."
    )]
    [SerializeField]
    private int prewarmCount = 12;



    [Header("Enemy Combat")]

    [Tooltip(
        "Solo es necesario si el prefab " +
        "tiene EnemyShooter."
    )]
    [SerializeField]
    private BulletPool enemyBulletPool;

    [Tooltip("Asigna el director de Danniel para que los enemigos disparen con el beat.")]
    [SerializeField]
    private DannielRhythmDirector rhythmDirector;


    private Transform playerTarget;


    private ObjectPool<GameObject> pool;


    private void Awake()
    {

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



    private void ValidateReferences()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "[EnemyPool] No se asign� Enemy Prefab.",
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
                "EnemyShooter pero no se asign� " +
                "Enemy Bullet Pool.",
                this
            );
        }
    }


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


    public IEnumerator Preload()
    {
        return EnsurePrewarmed(prewarmCount);
    }


    public IEnumerator EnsurePrewarmed(int minimumCount)
    {
        if (pool == null)
        {
            Debug.LogError(
                "[EnemyPool] El pool no esta inicializado.",
                this
            );

            yield break;
        }


        int amount =
            Mathf.Clamp(
                Mathf.Max(prewarmCount, minimumCount),
                0,
                maxSize
            );


        if (pool.CountInactive >= amount)
        {
            yield break;
        }


        GameObject[] enemies =
            new GameObject[amount];


        for (
            int i = 0;
            i < amount;
            i++
        )
        {
            enemies[i] =
                pool.Get();


            // Cada 2 enemigos se deja
            // pasar un frame para reducir
            // picos durante Loading.
            if (
                (i + 1) % 2 == 0
            )
            {
                yield return null;
            }
        }


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


        Debug.Log(
            $"EnemyPool precalentado: " +
            $"{amount} enemigos.",
            this
        );
    }


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


            enemy.SetActive(
                false
            );


            return enemy;
        }
    }



    private void OnGetEnemy(
        GameObject enemy
    )
    {
        /*
         * No activamos aqui
         *
         * GetEnemy configura primero:
         *
         * - Parent
         * - Position
         * - Rotation
         * - Target
         * - Combat
         *
         * y despues activa.
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


            GameObject enemy =
                pool.Get();


            if (enemy == null)
            {
                Debug.LogError(
                    "[EnemyPool] El pool devolvi� null.",
                    this
                );

                return null;
            }


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


            EnemyMeleeAI meleeAI =
                enemy.GetComponent<
                    EnemyMeleeAI>();


            if (meleeAI != null)
            {
                meleeAI.SetTarget(
                    playerTarget
                );
            }



            EnemyShooter shooter =
                enemy.GetComponent<
                    EnemyShooter>();


            if (shooter != null)
            {
                shooter.Configure(
                    playerTarget,
                    enemyBulletPool,
                    rhythmDirector
                );
            }


            enemy.SetActive(
                true
            );


            return enemy;
        }
    }


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
