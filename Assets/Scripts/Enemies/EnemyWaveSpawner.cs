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
    private EnemyPool enemyPool;

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
    private int amountOfEnemies = 10;

    [Min(0f)]
    [SerializeField]
    private float spawnDuration = 50f;


    // =========================
    // RUNTIME
    // =========================

    private Coroutine waveRoutine;

    private int nextSpawnPointIndex;


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
    // WAVE
    // =========================

    public void BeginWave()
    {
        if (waveRoutine != null)
        {
            return;
        }


        if (enemyPool == null)
        {
            Debug.LogError(
                "[EnemyWaveSpawner] EnemyPool no asignado.",
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


        waveRoutine =
            StartCoroutine(
                SpawnWaveRoutine()
            );
    }


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
            // Mantiene el comportamiento
            // del spawner anterior:
            //
            // primero espera el intervalo
            // y después aparece el enemigo.
            if (wait != null)
            {
                yield return wait;
            }
            else
            {
                // Si duration es 0,
                // evitamos crear todos
                // exactamente en el mismo frame.
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


        if (spawnPoint == null)
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
        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            return null;
        }


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
}