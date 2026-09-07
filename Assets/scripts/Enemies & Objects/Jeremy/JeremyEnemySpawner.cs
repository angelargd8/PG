using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyEnemyPool _enemyPool;
    [SerializeField] private Transform _enemyTarget;


    public void SpawnAt(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return;
        }

        if (_enemyPool == null)
        {
            Debug.LogError(
                "[JeremyEnemySpawner] No se asignó JeremyEnemyPool.",
                this
            );

            return;
        }

        if (_enemyTarget == null)
        {
            Debug.LogError(
                "[JeremyEnemySpawner] No se asignó EnemyTarget.",
                this
            );

            return;
        }

        _enemyPool.GetEnemy(
            spawnPoint.position,
            spawnPoint.rotation,
            _enemyTarget.position
        );
    }
}