using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyWaveSpawner : MonoBehaviour, IExperienceRuntime
{
    [Header("Dependencies")]
    [SerializeField] private EnemyPool[] enemyPools;
    [SerializeField] private Transform activeEnemiesRoot;

    [Tooltip("Si se deja vacio, se busca el ExperienceBeatPlayer de ExperienceCore al iniciar.")]
    [SerializeField] private ExperienceBeatPlayer beatPlayer;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Wave")]
    [Tooltip("Total de enemigos de la oleada, no el maximo simultaneo. 0 desactiva la oleada.")]
    [Min(0)]
    [SerializeField] private int amountOfEnemies = 15;

    [Tooltip("Genera un grupo en los beats 4, 8, 12... con el valor inicial de 4.")]
    [Min(1)]
    [SerializeField] private int beatsPerSpawn = 4;

    [Tooltip("Primeros beats de la cancion con apariciones de un solo enemigo.")]
    [Min(0)]
    [SerializeField] private int introductionBeats = 16;

    [Header("Music Intensity")]
    [Tooltip("Cantidad de beats recientes del BeatMap usados para calcular la intensidad media.")]
    [Min(1)]
    [SerializeField] private int intensityAverageBeats = 4;

    [Tooltip("Intensidad media necesaria para generar el grupo intenso, despues de la introduccion.")]
    [Range(0f, 1f)]
    [SerializeField] private float strongIntensityThreshold = 0.7f;

    [Min(1)]
    [SerializeField] private int normalEnemiesPerSpawn = 1;

    [Min(1)]
    [SerializeField] private int strongEnemiesPerSpawn = 2;

    [Header("Active Enemy Limits")]
    [Min(1)]
    [SerializeField] private int introductionMaxActiveEnemies = 4;

    [Min(1)]
    [SerializeField] private int maxActiveEnemies = 6;

    [Header("Debug")]
    [SerializeField] private bool logWaves;

    // Track only this spawner's enemies. Returned pool objects become inactive.
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly HashSet<Transform> usedSpawnPoints = new HashSet<Transform>();
    private ExperienceBeatPlayer subscribedBeatPlayer;
    private int nextSpawnPointIndex;
    private int nextPoolIndex;
    private int lastBeatIndex = -1;

    public bool IsRunning { get; private set; }
    public int SpawnedCount { get; private set; }
    public float RecentIntensity { get; private set; }

    public int ActiveEnemyCount
    {
        get
        {
            RemoveInactiveEnemies();
            return activeEnemies.Count;
        }
    }

    private void OnDisable()
    {
        StopWave();
    }

    private void OnValidate()
    {
        amountOfEnemies = Mathf.Max(0, amountOfEnemies);
        beatsPerSpawn = Mathf.Max(1, beatsPerSpawn);
        introductionBeats = Mathf.Max(0, introductionBeats);
        intensityAverageBeats = Mathf.Max(1, intensityAverageBeats);
        strongIntensityThreshold = Mathf.Clamp01(strongIntensityThreshold);
        normalEnemiesPerSpawn = Mathf.Max(1, normalEnemiesPerSpawn);
        strongEnemiesPerSpawn = Mathf.Max(normalEnemiesPerSpawn, strongEnemiesPerSpawn);
        introductionMaxActiveEnemies = Mathf.Max(1, introductionMaxActiveEnemies);
        maxActiveEnemies = Mathf.Max(introductionMaxActiveEnemies, maxActiveEnemies);
    }

    public void BeginExperience()
    {
        BeginWave();
    }

    public void EndExperience()
    {
        StopWave();
    }

    public void BeginWave()
    {
        if (IsRunning || !isActiveAndEnabled || amountOfEnemies <= 0)
        {
            return;
        }

        OnValidate();
        usedSpawnPoints.Clear();
        if (GetNextSpawnPoint() == null || GetNextEnemyPool() == null)
        {
            Debug.LogError("[EnemyWaveSpawner] Asigna al menos un Spawn Point y un Enemy Pool activos.", this);
            return;
        }

        if (beatPlayer == null)
        {
            beatPlayer = FindFirstObjectByType<ExperienceBeatPlayer>();
        }

        if (beatPlayer == null || !beatPlayer.isActiveAndEnabled ||
            beatPlayer.BeatMap == null || beatPlayer.BeatMap.Beats.Count == 0)
        {
            Debug.LogError("[EnemyWaveSpawner] Falta un ExperienceBeatPlayer activo con Beat Map. Inicia desde Bootstrap y revisa BeatEffects en ExperienceCore.", this);
            return;
        }

        nextSpawnPointIndex = 0;
        nextPoolIndex = 0;
        lastBeatIndex = -1;
        SpawnedCount = 0;
        RecentIntensity = 0f;
        RemoveInactiveEnemies();
        subscribedBeatPlayer = beatPlayer;
        subscribedBeatPlayer.BeatReached += HandleBeatReached;
        IsRunning = true;
    }

    public void StopWave()
    {
        if (subscribedBeatPlayer != null)
        {
            subscribedBeatPlayer.BeatReached -= HandleBeatReached;
        }

        subscribedBeatPlayer = null;
        IsRunning = false;
    }

    private void HandleBeatReached(BeatMapSO.Beat beat, int beatIndex)
    {
        if (!IsRunning || subscribedBeatPlayer == null || !subscribedBeatPlayer.IsPlaying ||
            beatIndex < 0 || beatIndex == lastBeatIndex)
        {
            return;
        }

        BeatMapSO map = subscribedBeatPlayer.BeatMap;
        if (map == null || beatIndex >= map.Beats.Count)
        {
            return;
        }

        lastBeatIndex = beatIndex;
        RecentIntensity = CalculateRecentIntensity(map, beatIndex);

        // usar el index del beat de la cancion para frames/seeks 
        if ((beatIndex + 1) % beatsPerSpawn == 0)
        {
            bool isIntroduction = beatIndex < introductionBeats;
            int requested = isIntroduction ? 1 :
                RecentIntensity >= strongIntensityThreshold ? strongEnemiesPerSpawn : normalEnemiesPerSpawn;
            int activeLimit = isIntroduction ? introductionMaxActiveEnemies : maxActiveEnemies;
            int available = Mathf.Max(0, activeLimit - ActiveEnemyCount);
            int amount = Mathf.Min(requested, Mathf.Min(available, amountOfEnemies - SpawnedCount));
            int spawned = SpawnGroup(amount);

            if (logWaves)
            {
                Debug.Log($"[EnemyWaveSpawner] Beat {beatIndex + 1} | Intensidad media: {RecentIntensity:F2} | Nuevos: {spawned}/{requested} | Activos: {ActiveEnemyCount}/{activeLimit} | Total: {SpawnedCount}/{amountOfEnemies}", this);
            }
        }

        if (SpawnedCount >= amountOfEnemies || beatIndex == map.Beats.Count - 1)
        {
            StopWave();
        }
    }

    private float CalculateRecentIntensity(BeatMapSO map, int beatIndex)
    {
        int firstIndex = Mathf.Max(0, beatIndex - intensityAverageBeats + 1);
        float sum = 0f;
        for (int i = firstIndex; i <= beatIndex; i++)
        {
            sum += map.Beats[i].Intensity;
        }

        return sum / (beatIndex - firstIndex + 1);
    }

    private int SpawnGroup(int amount)
    {
        usedSpawnPoints.Clear();
        int spawned = 0;
        for (int i = 0; i < amount; i++)
        {
            Transform spawnPoint = GetNextSpawnPoint();
            EnemyPool enemyPool = GetNextEnemyPool();
            if (spawnPoint == null || enemyPool == null)
            {
                break;
            }

            usedSpawnPoints.Add(spawnPoint);
            Transform parent = activeEnemiesRoot != null ? activeEnemiesRoot : transform;
            GameObject enemy = enemyPool.GetEnemy(parent, spawnPoint.position, spawnPoint.rotation);
            if (enemy == null)
            {
                continue;
            }

            activeEnemies.Add(enemy);
            SpawnedCount++;
            spawned++;
        }

        return spawned;
    }

    private void RemoveInactiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null || !activeEnemies[i].activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        int attempts = spawnPoints.Length;
        while (attempts-- > 0)
        {
            nextSpawnPointIndex %= spawnPoints.Length;
            Transform point = spawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            if (point != null && point.gameObject.activeInHierarchy && !usedSpawnPoints.Contains(point))
            {
                return point;
            }
        }

        return null;
    }

    private EnemyPool GetNextEnemyPool()
    {
        if (enemyPools == null || enemyPools.Length == 0)
        {
            return null;
        }

        int attempts = enemyPools.Length;
        while (attempts-- > 0)
        {
            nextPoolIndex %= enemyPools.Length;
            EnemyPool selectedPool = enemyPools[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % enemyPools.Length;
            if (selectedPool != null && selectedPool.isActiveAndEnabled)
            {
                return selectedPool;
            }
        }

        return null;
    }
}
