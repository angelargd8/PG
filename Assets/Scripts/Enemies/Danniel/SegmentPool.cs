using UnityEngine;
using Unity.Profiling;
using System.Collections;

public class SegmentPool : MonoBehaviour, IExperiencePreloadable
{
    // =========================
    // PROFILER
    // =========================

    private static readonly ProfilerMarker MoveMarker =
        new ProfilerMarker("SegmentPool.Move");

    private static readonly ProfilerMarker RecycleMarker =
        new ProfilerMarker("SegmentPool.Recycle");

    private static readonly ProfilerMarker ClearEnemiesMarker =
        new ProfilerMarker("SegmentPool.ClearEnemies");

    private static readonly ProfilerMarker SpawnEnemiesMarker =
        new ProfilerMarker("SegmentPool.SpawnEnemies");


    // =========================
    // SEGMENTS
    // =========================

    [Header("Segments")]
    [SerializeField] private GameObject segmentPrefab;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float speedMultiplier = 2f;

    [SerializeField] private int maxActiveSegments = 3;

    [SerializeField] private float segmentLength = 80f;
    [SerializeField] private float firstSpawnZ = -160f;
    [SerializeField] private float recycleZ = -160f;


    // =========================
    // ENEMIES
    // =========================

    [Header("Enemies")]
    [SerializeField] private EnemySpawnDirector enemySpawnDirector;

    [SerializeField] private EnemyPool enemyPool;


    private SegmentData[] segments;

    private int oldestIndex;

    private bool isInitialized;


    private class SegmentData
    {
        public GameObject GameObject;
        public Transform Transform;

        public SegmentEnemySpawns EnemySpawns;

        public SegmentContent Content;
    }


    // =========================
    // PRELOAD
    // =========================

    public IEnumerator Preload()
    {
        if (isInitialized)
        {
            yield break;
        }


        Debug.Log(
            "SegmentPool comienza Preload.",
            this
        );


        segments =
            new SegmentData[maxActiveSegments];


        float currentZ =
            firstSpawnZ;


        for (int i = 0;
             i < maxActiveSegments;
             i++)
        {
            GameObject segmentObject =
                Instantiate(
                    segmentPrefab,
                    new Vector3(
                        0f,
                        0f,
                        currentZ
                    ),
                    Quaternion.identity,
                    transform
                );


            SegmentData data =
                new SegmentData
                {
                    GameObject =
                        segmentObject,

                    Transform =
                        segmentObject.transform,

                    EnemySpawns =
                        segmentObject
                            .GetComponent<
                                SegmentEnemySpawns>(),

                    Content =
                        segmentObject
                            .GetComponent<
                                SegmentContent>()
                };


            segments[i] =
                data;


            SpawnEnemies(
                data
            );


            currentZ +=
                segmentLength;


            // Un segmento por frame
            yield return null;
        }


        oldestIndex = 0;

        isInitialized = true;


        Debug.Log(
            $"SegmentPool preparado con " +
            $"{maxActiveSegments} segmentos. " +
            $"isInitialized = {isInitialized}",
            this
        );
    }


    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }


        MoveSegments();


        int safety =
            segments.Length;


        while (
            safety-- > 0 &&
            segments[oldestIndex]
                .Transform.position.z < recycleZ
        )
        {
            RecycleOldestSegment();
        }
    }


    // =========================
    // MOVEMENT
    // =========================

    private void MoveSegments()
    {
        using (MoveMarker.Auto())
        {
            float movement =
                speed *
                speedMultiplier *
                Time.deltaTime;


            Vector3 offset =
                Vector3.back *
                movement;


            for (int i = 0;
                 i < segments.Length;
                 i++)
            {
                segments[i]
                    .Transform
                    .position += offset;
            }
        }
    }


    // =========================
    // RECYCLE
    // =========================

    private void RecycleOldestSegment()
    {
        using (RecycleMarker.Auto())
        {
            SegmentData segment =
                segments[oldestIndex];


            // -------------------------
            // CLEAR ENEMIES
            // -------------------------

            if (segment.Content != null &&
                enemyPool != null)
            {
                using (ClearEnemiesMarker.Auto())
                {
                    segment.Content
                        .ClearEnemies(
                            enemyPool
                        );
                }
            }


            // -------------------------
            // FIND LAST SEGMENT
            // -------------------------

            int lastIndex =
                (
                    oldestIndex -
                    1 +
                    segments.Length
                )
                % segments.Length;


            float newZ =
                segments[lastIndex]
                    .Transform
                    .position.z
                +
                segmentLength;


            // -------------------------
            // MOVE RECYCLED SEGMENT
            // -------------------------

            segment.Transform.position =
                new Vector3(
                    0f,
                    segment.Transform.position.y,
                    newZ
                );


            // -------------------------
            // NEW ENEMIES
            // -------------------------

            SpawnEnemies(
                segment
            );


            // -------------------------
            // NEXT OLDEST
            // -------------------------

            oldestIndex =
                (
                    oldestIndex + 1
                )
                % segments.Length;
        }
    }


    // =========================
    // SPAWN ENEMIES
    // =========================

    private void SpawnEnemies(
        SegmentData segment
    )
    {
        if (segment.EnemySpawns == null ||
            enemySpawnDirector == null)
        {
            return;
        }


        using (SpawnEnemiesMarker.Auto())
        {
            enemySpawnDirector
                .SpawnEnemiesOnSegment(
                    segment.EnemySpawns
                );
        }
    }
}