using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;

public class EnemySpawnDirector : MonoBehaviour
{
    private static readonly ProfilerMarker SpawnMarker =
        new ProfilerMarker("EnemySpawnDirector.Spawn");


    [SerializeField]
    private EnemyPool enemyPool;

    [SerializeField]
    private int enemiesPerSegment = 3;


    private readonly List<Transform> availablePoints =
        new List<Transform>(16);


    public void SpawnEnemiesOnSegment(
        SegmentEnemySpawns segmentSpawns
    )
    {
        using (SpawnMarker.Auto())
        {
            if (segmentSpawns == null ||
                enemyPool == null)
            {
                return;
            }


            Transform[] spawnPoints =
                segmentSpawns.SpawnPoints;


            if (spawnPoints == null ||
                spawnPoints.Length == 0)
            {
                return;
            }


            // Ya no usamos GetComponent
            SegmentContent segmentContent = segmentSpawns.Content;


            // Reutilizamos la misma lista
            availablePoints.Clear();

            availablePoints.AddRange(
                spawnPoints
            );


            int amount =
                Mathf.Min(
                    enemiesPerSegment,
                    availablePoints.Count
                );


            for (int i = 0; i < amount; i++)
            {
                int randomIndex =
                    Random.Range(
                        0,
                        availablePoints.Count
                    );


                Transform spawnPoint =
                    availablePoints[randomIndex];


                // Remove rápido
                int lastIndex =
                    availablePoints.Count - 1;

                availablePoints[randomIndex] =
                    availablePoints[lastIndex];

                availablePoints.RemoveAt(
                    lastIndex
                );


                GameObject enemy =
                    enemyPool.GetEnemy(
                        segmentSpawns.transform,
                        spawnPoint.position,
                        spawnPoint.rotation
                    );


                if (segmentContent != null)
                {
                    segmentContent.RegisterEnemy(
                        enemy
                    );
                }
            }
        }
    }
}