using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyWaveSpawner :
    MonoBehaviour,
    IExperienceRuntime
{
    // =========================
    // DEPENDENCIES
    // =========================

    [Header("Dependencies")]

    [SerializeField]
    private EnemyPool[] enemyPools;

    [SerializeField]
    private Transform activeEnemiesRoot;


    // =========================
    // SPAWN POINTS
    // =========================

    [Header("Spawn Points")]

    [SerializeField]
    private Transform[] spawnPoints;


    // =========================
    // WAVE
    // =========================

    [Header("Wave")]

    [Min(0)]
    [SerializeField]
    private int amountOfEnemies = 15;

    [Min(0f)]
    [SerializeField]
    private float spawnDuration = 50f;


    // =========================
    // RUNTIME
    // =========================

    private Coroutine waveRoutine;

    private int nextSpawnPointIndex;

    private int nextPoolIndex;


    public bool IsRunning =>
        waveRoutine != null;


    // =========================
    // EXPERIENCE
    // =========================

    public void BeginExperience()
    {
        BeginWave();
    }


    public void EndExperience()
    {
        StopWave();
    }


    // =========================
    // BEGIN WAVE
    // =========================

    public void BeginWave()
    {
        if (waveRoutine != null)
        {
            return;
        }


        if (
            enemyPools == null ||
            enemyPools.Length == 0
        )
        {
            Debug.LogError(
                "[EnemyWaveSpawner] No hay Enemy Pools.",
                this
            );

            return;
        }


        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            Debug.LogError(
                "[EnemyWaveSpawner] No hay Spawn Points.",
                this
            );

            return;
        }


        if (amountOfEnemies <= 0)
        {
            return;
        }


        nextSpawnPointIndex = 0;

        nextPoolIndex = 0;


        waveRoutine =
            StartCoroutine(
                SpawnWaveRoutine()
            );
    }


    // =========================
    // STOP WAVE
    // =========================

    public void StopWave()
    {
        if (waveRoutine == null)
        {
            return;
        }


        StopCoroutine(
            waveRoutine
        );


        waveRoutine = null;
    }


    // =========================
    // SPAWN ROUTINE
    // =========================

    private IEnumerator SpawnWaveRoutine()
    {
        float spawnInterval =
            spawnDuration /
            amountOfEnemies;


        WaitForSeconds wait = null;


        if (spawnInterval > 0f)
        {
            wait =
                new WaitForSeconds(
                    spawnInterval
                );
        }


        for (
            int i = 0;
            i < amountOfEnemies;
            i++
        )
        {
            if (wait != null)
            {
                yield return wait;
            }
            else
            {
                yield return null;
            }


            SpawnEnemy();
        }


        waveRoutine = null;
    }


    // =========================
    // SPAWN
    // =========================

    private void SpawnEnemy()
    {
        Transform spawnPoint =
            GetNextSpawnPoint();


        EnemyPool enemyPool =
            GetNextEnemyPool();


        if (
            spawnPoint == null ||
            enemyPool == null
        )
        {
            return;
        }


        Transform parent =
            activeEnemiesRoot != null
                ? activeEnemiesRoot
                : transform;


        enemyPool.GetEnemy(
            parent,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }


    // =========================
    // SPAWN POINT
    // =========================

    private Transform GetNextSpawnPoint()
    {
        int attempts =
            spawnPoints.Length;


        while (attempts-- > 0)
        {
            Transform point =
                spawnPoints[
                    nextSpawnPointIndex
                ];


            nextSpawnPointIndex =
                (
                    nextSpawnPointIndex + 1
                )
                %
                spawnPoints.Length;


            if (point != null)
            {
                return point;
            }
        }


        return null;
    }


    // =========================
    // ENEMY POOL
    // =========================

    private EnemyPool GetNextEnemyPool()
    {
        int attempts =
            enemyPools.Length;


        while (attempts-- > 0)
        {
            EnemyPool selectedPool =
                enemyPools[
                    nextPoolIndex
                ];


            nextPoolIndex =
                (
                    nextPoolIndex + 1
                )
                %
                enemyPools.Length;


            if (selectedPool != null)
            {
                return selectedPool;
            }
        }


        return null;
    }
}