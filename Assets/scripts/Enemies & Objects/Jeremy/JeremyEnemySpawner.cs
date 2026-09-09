using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyEnemyPool _enemyPool;
    [SerializeField] private Transform _enemyTarget;


    [Header("Movement")]
    [SerializeField] private float _movementSpeed = 2f;
    [SerializeField] private float _idealCutDistance = 0.5f;


    [Header("Temporary")]
    [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.Normal; // Esto lo voy a quitar cuando haga el sistema de DDA


    public Vector3 TargetPosition => _enemyTarget.position;
    public float MovementSpeed => _movementSpeed;
    public float IdealCutDistance => _idealCutDistance;


    public void SpawnAt(Transform spawnPoint, double expectedHitTime)
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

        JeremyCutDirection direction = (JeremyCutDirection)Random.Range(0, 4);
        JeremyHand hand = (JeremyHand)Random.Range(0, 3);

        _enemyPool.GetEnemy(
            spawnPoint.position,
            spawnPoint.rotation,
            _enemyTarget.position,
            direction,
            hand,
            _difficulty,
            _movementSpeed,
            _idealCutDistance,
            expectedHitTime
        );
    }
}