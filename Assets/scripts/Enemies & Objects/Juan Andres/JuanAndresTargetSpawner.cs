using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresTargetSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JuanAndresTargetPool _targetPool;


    [Header("Spawn Points")]
    [SerializeField] private JuanAndresSpawnPoint[] _spawnPoints;


    public JuanAndresTarget ReserveTarget(
        JuanAndresSpawnPoint spawnPoint,
        JuanAndresActionDirection direction,
        JuanAndresToolType tool,
        DifficultyLevel difficulty,
        double spawnTime,
        double expectedTime,
        double expireTime,
        int pairId)
    {
        if (spawnPoint == null ||
            !spawnPoint.IsAvailable)
        {
            return null;
        }

        if (_targetPool == null)
        {
            Debug.LogError(
                "[JuanAndresTargetSpawner] TargetPool no está asignado.",
                this
            );

            return null;
        }

        JuanAndresTarget target = _targetPool.GetTarget();

        if (target == null)
        {
            Debug.LogError(
                "[JuanAndresTargetSpawner] TargetPool devolvió null.",
                this
            );

            return null;
        }

        if (!spawnPoint.TryReserve(target))
        {
            _targetPool.ReleaseTarget(
                target
            );

            return null;
        }

        target.Initialize(
            _targetPool,
            spawnPoint,
            direction,
            tool,
            difficulty,
            spawnTime,
            expectedTime,
            expireTime,
            pairId
        );

        return target;
    }


    public bool ActivateTarget(JuanAndresTarget target)
    {
        if (target == null ||
            target.IsResolved ||
            target.gameObject.activeSelf)
        {
            return false;
        }

        target.gameObject.SetActive(true);

        return true;
    }


    public void CancelReservedTarget(JuanAndresTarget target)
    {
        if (target == null ||
            target.gameObject.activeSelf)
        {
            return;
        }

        _targetPool.ReleaseTarget(
            target
        );
    }


    public int GetAvailableSpawnPoints(List<JuanAndresSpawnPoint> results)
    {
        if (results == null)
        {
            return 0;
        }

        results.Clear();

        if (_spawnPoints == null)
        {
            return 0;
        }

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            JuanAndresSpawnPoint spawnPoint =
                _spawnPoints[i];

            if (spawnPoint != null &&
                spawnPoint.IsAvailable)
            {
                results.Add(
                    spawnPoint
                );
            }
        }

        return results.Count;
    }


    private void OnValidate()
    {
        if (_spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (_spawnPoints[i] == null)
            {
                Debug.LogWarning(
                    $"[JuanAndresTargetSpawner] " +
                    $"SpawnPoint {i} no está asignado.",
                    this
                );
            }
        }
    }
}