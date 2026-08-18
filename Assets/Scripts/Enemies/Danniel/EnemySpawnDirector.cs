using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnDirector : MonoBehaviour
{
    [SerializeField] private EnemyPool enemyPool;

    [SerializeField] private int enemiesPerSegment = 3;


    public void SpawnEnemiesOnSegment(SegmentEnemySpawns segmentSpawns)
    {
        if (segmentSpawns == null)
            return;

        Transform[] spawnPoints = segmentSpawns.SpawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return;


        SegmentContent segmentContent =
            segmentSpawns.GetComponent<SegmentContent>();


        List<Transform> availablePoints =
            new List<Transform>(spawnPoints);


        int amount = Mathf.Min(
            enemiesPerSegment,
            availablePoints.Count
        );


        for (int i = 0; i < amount; i++)
        {
            int randomIndex =
                Random.Range(0, availablePoints.Count);

            Transform spawnPoint =
                availablePoints[randomIndex];

            availablePoints.RemoveAt(randomIndex);


            GameObject enemy =
                enemyPool.GetEnemy();


            enemy.transform.SetParent(
                segmentSpawns.transform
            );

            enemy.transform.SetPositionAndRotation(
                spawnPoint.position,
                spawnPoint.rotation
            );


            if (segmentContent != null)
            {
                segmentContent.RegisterEnemy(enemy);
            }
        }
    }
}