using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremySpawnDirector :
    MonoBehaviour,
    IExperienceRuntime
{
    private sealed class ScheduledSpawn
    {
        public int BeatIndex;
        public Transform SpawnPoint;
        public double ExpectedHitTime;
        public double SpawnTime;
    }


    [Header("Spawning")]
    [SerializeField] private JeremyEnemySpawner _enemySpawner;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Beat Map")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Scheduling")]
    [SerializeField] private int _lookAheadBeats = 8;

    [Header("Temporary Difficulty")]
    [SerializeField] private int _spawnEveryNBeats = 2;


    private readonly List<ScheduledSpawn> _scheduledSpawns = new List<ScheduledSpawn>();

    private bool _isRunning;
    private float _experienceStartTime;
    private int _nextBeatIndex;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        _isRunning = true;
        _nextBeatIndex = 0;
        _experienceStartTime = Time.time;

        _scheduledSpawns.Clear();

        FillSchedule();
    }


    public void EndExperience()
    {
        _isRunning = false;
        _scheduledSpawns.Clear();
    }


    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        ProcessScheduledSpawns();
        FillSchedule();
    }


    private void FillSchedule()
    {
        while (
            _scheduledSpawns.Count < _lookAheadBeats &&
            _nextBeatIndex < _beatMap.BeatTimes.Count)
        {
            int beatIndex = _nextBeatIndex;
            _nextBeatIndex++;

            if (beatIndex % _spawnEveryNBeats != 0)
            {
                continue;
            }

            ScheduleBeat(beatIndex);
        }
    }


    private void ScheduleBeat(int beatIndex)
    {
        double expectedHitTime = _beatMap.BeatTimes[beatIndex];

        Transform spawnPoint = GetSpawnPointForSchedule();

        if (spawnPoint == null)
        {
            return;
        }

        float travelTime = CalculateTravelTime(spawnPoint);
        double spawnTime = expectedHitTime - travelTime;

        if (spawnTime < 0.0)
        {
            return;
        }

        ScheduledSpawn scheduledSpawn = new ScheduledSpawn
        {
            BeatIndex = beatIndex,
            SpawnPoint = spawnPoint,
            ExpectedHitTime = expectedHitTime,
            SpawnTime = spawnTime
        };

        InsertSorted(scheduledSpawn);
    }


    private void ProcessScheduledSpawns()
    {
        double elapsedTime = Time.time - _experienceStartTime;

        while (
            _scheduledSpawns.Count > 0 &&
            elapsedTime >= _scheduledSpawns[0].SpawnTime)
        {
            ScheduledSpawn scheduledSpawn = _scheduledSpawns[0];
            _scheduledSpawns.RemoveAt(0);

            _enemySpawner.SpawnAt(
                scheduledSpawn.SpawnPoint,
                scheduledSpawn.ExpectedHitTime
            );
        }
    }


    private void InsertSorted(ScheduledSpawn scheduledSpawn)
    {
        int insertIndex = 0;

        while (
            insertIndex < _scheduledSpawns.Count &&
            _scheduledSpawns[insertIndex].SpawnTime <= scheduledSpawn.SpawnTime)
        {
            insertIndex++;
        }

        _scheduledSpawns.Insert(insertIndex, scheduledSpawn);
    }


    private Transform GetSpawnPointForSchedule()
    {
        if (_spawnPoints.Length == 1)
        {
            return _spawnPoints[0];
        }

        const int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int randomIndex = Random.Range(0, _spawnPoints.Length);
            Transform candidate = _spawnPoints[randomIndex];

            if (!WouldCreateTooManyConsecutive(candidate))
            {
                return candidate;
            }
        }

        return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
    }


    private bool WouldCreateTooManyConsecutive(Transform candidate)
    {
        if (_scheduledSpawns.Count < 3)
        {
            return false;
        }

        int sameCount = 0;

        for (int i = _scheduledSpawns.Count - 1; i >= 0; i--)
        {
            if (_scheduledSpawns[i].SpawnPoint != candidate)
            {
                break;
            }

            sameCount++;

            if (sameCount >= 3)
            {
                return true;
            }
        }

        return false;
    }


    private float CalculateTravelTime(Transform spawnPoint)
    {
        float distanceToTarget = Vector3.Distance(
            spawnPoint.position,
            _enemySpawner.TargetPosition
        );

        float travelDistance =
            distanceToTarget - _enemySpawner.IdealCutDistance;

        travelDistance = Mathf.Max(0f, travelDistance);

        return travelDistance / _enemySpawner.MovementSpeed;
    }


    private bool ValidateReferences()
    {
        if (_enemySpawner == null)
        {
            Debug.LogError("[JeremySpawnDirector] No se asignó JeremyEnemySpawner.", this);
            return false;
        }

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[JeremySpawnDirector] No hay SpawnPoints asignados.", this);
            return false;
        }

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (_spawnPoints[i] == null)
            {
                Debug.LogError($"[JeremySpawnDirector] SpawnPoint {i} es null.", this);
                return false;
            }
        }

        if (_beatMap == null || _beatMap.BeatTimes.Count == 0)
        {
            Debug.LogError("[JeremySpawnDirector] No se asignó un BeatMap válido.", this);
            return false;
        }

        if (_spawnEveryNBeats <= 0)
        {
            Debug.LogError("[JeremySpawnDirector] Spawn Every N Beats debe ser mayor que 0.", this);
            return false;
        }

        if (_lookAheadBeats <= 0)
        {
            Debug.LogError("[JeremySpawnDirector] Look Ahead Beats debe ser mayor que 0.", this);
            return false;
        }

        if (_enemySpawner.MovementSpeed <= 0f)
        {
            Debug.LogError("[JeremySpawnDirector] Movement Speed debe ser mayor que 0.", this);
            return false;
        }

        return true;
    }
}