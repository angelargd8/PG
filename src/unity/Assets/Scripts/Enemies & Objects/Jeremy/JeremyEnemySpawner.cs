using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyEnemyPool _enemyPool;
    [SerializeField] private Transform _enemyTarget;


    [Header("Movement")]
    [SerializeField] private float _idealCutDistance = 1.3f;


    public Vector3 TargetPosition => _enemyTarget.position;
    public float IdealCutDistance => _idealCutDistance;


    public void SpawnAt(
        Transform spawnPoint, 
        double expectedHitTime,
        DifficultyLevel difficulty,
        float movementSpeed,
        float specificHandProbability
    ){
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
        JeremyHand hand = GetHand(specificHandProbability);

        _enemyPool.GetEnemy(
            spawnPoint.position,
            spawnPoint.rotation,
            _enemyTarget.position,
            direction,
            hand,
            difficulty,
            movementSpeed,
            _idealCutDistance,
            expectedHitTime
        );
    }

    private JeremyHand GetHand(float specificHandProbability)
    {
        if (Random.value > specificHandProbability)
        {
            return JeremyHand.Any;
        }

        return Random.value < 0.5f
            ? JeremyHand.Left
            : JeremyHand.Right;
    }
}