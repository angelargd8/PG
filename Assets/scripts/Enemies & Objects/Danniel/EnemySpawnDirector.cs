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

    [Tooltip(
        "Usar la cantidad de Android en Quest standalone " +
        "y la de PC en Editor o Quest Link. " +
        "Estos valores funcionan como fallback si SegmentPool " +
        "no establece un limite runtime."
    )]
    [SerializeField]
    private bool usePlatformEnemyCount = true;


    [Tooltip(
        "Cantidad por defecto para Editor, Windows y Quest Link."
    )]
    [Min(0)]
    [SerializeField]
    private int desktopEnemiesPerSegment = 6;


    [Tooltip(
        "Cantidad fallback para un APK Android. " +
        "Normalmente SegmentPool reemplaza este valor " +
        "segun el modelo de Quest."
    )]
    [Min(0)]
    [SerializeField]
    private int androidEnemiesPerSegment = 4;


    [Tooltip(
        "Cantidad manual. Solo se usa si Use Platform Enemy Count " +
        "esta desactivado y SegmentPool no establece un limite."
    )]
    [Min(0)]
    [SerializeField]
    private int enemiesPerSegment = 4;


    [Header("Runtime Enemy Limit")]

    [Tooltip(
        "Valor establecido automaticamente por SegmentPool. " +
        "-1 significa que no hay override."
    )]
    [SerializeField]
    private int runtimeMaxEnemiesPerSegment = -1;


    public int RuntimeMaxEnemiesPerSegment =>
        runtimeMaxEnemiesPerSegment;



    private DifficultyLevel _spawnDifficulty =
        DifficultyLevel.Normal;


    private float _difficultyEnemyDensity = 1f;



    private int EffectiveEnemiesPerSegment
    {
        get
        {
            int baseAmount =
                BaseEnemiesPerSegment;


            if (
                baseAmount <= 0 ||
                _difficultyEnemyDensity <= 0f
            )
            {
                return 0;
            }


            int amount =
                Mathf.RoundToInt(
                    baseAmount *
                    _difficultyEnemyDensity
                );


            return Mathf.Clamp(
                amount,
                1,
                baseAmount
            );
        }
    }


    private readonly List<Transform> availablePoints =
        new List<Transform>(16);



    /// SegmentPool
    /// Quest 2      -> 3
    /// Quest 3/3S   -> 4
    /// Editor/Link  -> 6
    public void SetMaxEnemiesPerSegment(
        int amount
    )
    {
        runtimeMaxEnemiesPerSegment =
            Mathf.Max(
                0,
                amount
            );


        Debug.Log(
            "[EnemySpawnDirector] " +
            "Limite runtime establecido: " +
            $"{runtimeMaxEnemiesPerSegment} enemigos por segmento.",
            this
        );
    }


    public IEnumerator PreloadForSegments(
        int segmentCount
    )
    {
        if (enemyPool == null)
        {
            yield break;
        }


        int requiredCount =
            Mathf.Max(
                1,
                segmentCount
            )
            *
            EffectiveEnemiesPerSegment;


        Debug.Log(
            "[EnemySpawnDirector] Prewarm | " +
            $"Segmentos: {segmentCount} | " +
            $"Enemigos/segmento: {EffectiveEnemiesPerSegment} | " +
            $"Total pool requerido: {requiredCount}",
            this
        );


        yield return
            enemyPool.EnsurePrewarmed(
                requiredCount
            );
    }


    public void SpawnEnemiesOnSegment(
        SegmentEnemySpawns segmentSpawns
    )
    {
        using (SpawnMarker.Auto())
        {
            if (
                segmentSpawns == null ||
                enemyPool == null
            )
            {
                return;
            }


            Transform[] spawnPoints =
                segmentSpawns.SpawnPoints;


            if (
                spawnPoints == null ||
                spawnPoints.Length == 0
            )
            {
                return;
            }


            SegmentContent segmentContent =
                segmentSpawns.Content;


            if (segmentContent != null)
            {
                segmentContent.SetSpawnDifficulty(
                    _spawnDifficulty
                );
            }


            availablePoints.Clear();


            availablePoints.AddRange(
                spawnPoints
            );


            int amount =
                Mathf.Min(
                    EffectiveEnemiesPerSegment,
                    availablePoints.Count
                );


            for (
                int i = 0;
                i < amount;
                i++
            )
            {
                int randomIndex =
                    Random.Range(
                        0,
                        availablePoints.Count
                    );


                Transform spawnPoint =
                    availablePoints[
                        randomIndex
                    ];


                int lastIndex =
                    availablePoints.Count - 1;


                availablePoints[
                    randomIndex
                ] =
                    availablePoints[
                        lastIndex
                    ];


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



    private int BaseEnemiesPerSegment
    {
        get
        {


            if (
                runtimeMaxEnemiesPerSegment >= 0
            )
            {
                return runtimeMaxEnemiesPerSegment;
            }



            int amount =
                enemiesPerSegment;


            if (usePlatformEnemyCount)
            {
                amount =
                    Application.platform ==
                    RuntimePlatform.Android

                        ? androidEnemiesPerSegment
                        : desktopEnemiesPerSegment;
            }


            return Mathf.Max(
                0,
                amount
            );
        }
    }



    public void SetDifficulty(
        DifficultyLevel difficulty,
        float enemyDensity
    )
    {
        _spawnDifficulty =
            difficulty;


        _difficultyEnemyDensity =
            Mathf.Clamp01(
                enemyDensity
            );
    }
}