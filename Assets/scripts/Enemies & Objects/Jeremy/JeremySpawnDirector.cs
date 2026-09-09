using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremySpawnDirector :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("Spawning")]
    [SerializeField] private JeremyEnemySpawner _enemySpawner;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Beat Map")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Temporary Difficulty")]
    [SerializeField] private int _spawnEveryNBeats = 2;


    private Coroutine _spawnRoutine;
    private bool _isRunning;
    private float _experienceStartTime;
    private int _nextBeatIndex;

    private int _lastSpawnIndex = -1;
    private int _consecutiveSameSpawn;


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

        _lastSpawnIndex = -1;
        _consecutiveSameSpawn = 0;

        _experienceStartTime = Time.time;

        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }


    public void EndExperience()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }


    private IEnumerator SpawnRoutine()
    {
        while (_isRunning && _nextBeatIndex < _beatMap.BeatTimes.Count)
        {
            if (_nextBeatIndex % _spawnEveryNBeats != 0)
            {
                _nextBeatIndex++;
                continue;
            }

            double expectedHitTime = _beatMap.BeatTimes[_nextBeatIndex];

            int spawnIndex = GetNextSpawnIndex();
            Transform spawnPoint = _spawnPoints[spawnIndex];

            float travelTime = CalculateTravelTime(spawnPoint);
            double spawnTime = expectedHitTime - travelTime;

            if (spawnTime < 0.0)
            {
                _nextBeatIndex++;
                continue;
            }

            while (_isRunning)
            {
                float elapsedTime = Time.time - _experienceStartTime;

                if (elapsedTime >= spawnTime)
                {
                    break;
                }

                yield return null;
            }

            if (!_isRunning)
            {
                yield break;
            }

            _enemySpawner.SpawnAt(spawnPoint, expectedHitTime);

            RegisterSpawn(spawnIndex);

            _nextBeatIndex++;

            yield return null;
        }
    }


    private int GetNextSpawnIndex()
    {
        if (_spawnPoints.Length == 1)
        {
            return 0;
        }

        if (_lastSpawnIndex >= 0 && _consecutiveSameSpawn >= 3)
        {
            int randomIndex = Random.Range(0, _spawnPoints.Length - 1);

            if (randomIndex >= _lastSpawnIndex)
            {
                randomIndex++;
            }

            return randomIndex;
        }

        return Random.Range(0, _spawnPoints.Length);
    }


    private void RegisterSpawn(int spawnIndex)
    {
        if (spawnIndex == _lastSpawnIndex)
        {
            _consecutiveSameSpawn++;
            return;
        }

        _lastSpawnIndex = spawnIndex;
        _consecutiveSameSpawn = 1;
    }


    private float CalculateTravelTime(Transform spawnPoint)
    {
        float distanceToTarget = Vector3.Distance(spawnPoint.position, _enemySpawner.TargetPosition);

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

        if (_enemySpawner.MovementSpeed <= 0f)
        {
            Debug.LogError("[JeremySpawnDirector] Movement Speed debe ser mayor que 0.", this);
            return false;
        }

        return true;
    }
}