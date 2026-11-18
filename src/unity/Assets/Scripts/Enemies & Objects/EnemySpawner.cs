using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int amountOfEnemies = 10;
    [SerializeField] private float spawnDuration = 5f;

    private float spawnInterval;
    private float timer;
    private int enemiesSpawned;

    void Start()
    {
        if (amountOfEnemies > 0)
        {
            spawnInterval = spawnDuration / amountOfEnemies;
        }
    }

    void Update()
    {
        if (enemiesSpawned >= amountOfEnemies)
            return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();

            timer = 0f;
            enemiesSpawned++;
        }
    }

    private void SpawnEnemy()
    {
        Instantiate(enemyPrefab, transform.position, transform.rotation);
    }
}