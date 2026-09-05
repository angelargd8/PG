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
    [SerializeField] private float _spawnInterval = 2f;


    private Coroutine _spawnRoutine;
    private bool _isRunning;


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
        while (_isRunning)
        {
            SpawnRandomEnemy();

            yield return new WaitForSeconds(_spawnInterval);
        }
    }


    private void SpawnRandomEnemy()
    {
        int randomIndex = Random.Range(0, _spawnPoints.Length);

        Transform spawnPoint = _spawnPoints[randomIndex];

        if (spawnPoint == null)
        {
            return;
        }

        _enemySpawner.SpawnAt(spawnPoint);
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

        if (_spawnInterval <= 0f)
        {
            Debug.LogError("[JeremySpawnDirector] Spawn Interval debe ser mayor que 0.", this);

            return false;
        }

        return true;
    }
}