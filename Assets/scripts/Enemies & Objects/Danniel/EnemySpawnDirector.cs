using UnityEngine;
using Unity.Profiling;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnDirector : MonoBehaviour
{
    private static readonly ProfilerMarker SpawnMarker =
        new ProfilerMarker("EnemySpawnDirector.Spawn");


    [SerializeField]
    private EnemyPool enemyPool;

    [Header("Enemies Per Platform")]
    [Tooltip("Usar la cantidad de Android en Quest standalone y la de PC en el Editor o Quest Link.")]
    [SerializeField] private bool usePlatformEnemyCount = true;

    [Min(0)]
    [SerializeField] private int desktopEnemiesPerSegment = 6;

    [Tooltip("Cantidad para el APK que se ejecuta directamente en Quest, incluso conectado por USB.")]
    [Min(0)]
    [SerializeField] private int androidEnemiesPerSegment = 3;

    [Tooltip("Cantidad manual. Solo se usa si Use Platform Enemy Count esta desactivado.")]
    [Min(0)]
    [SerializeField]
    private int enemiesPerSegment = 3;

    private int EffectiveEnemiesPerSegment
    {
        get
        {
            int amount = enemiesPerSegment;
            if (usePlatformEnemyCount)
            {
                amount = Application.platform == RuntimePlatform.Android
                    ? androidEnemiesPerSegment
                    : desktopEnemiesPerSegment;
            }

            return Mathf.Max(0, amount);
        }
    }


    private readonly List<Transform> availablePoints =
        new List<Transform>(16);


    public IEnumerator PreloadForSegments(int segmentCount)
    {
        if (enemyPool != null)
        {
            // Precalentar para el maximo simultaneo, no solo para el primer segmento.
            int requiredCount = Mathf.Max(1, segmentCount) * EffectiveEnemiesPerSegment;
            yield return enemyPool.EnsurePrewarmed(requiredCount);
        }
    }

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
                    EffectiveEnemiesPerSegment,
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