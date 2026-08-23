using UnityEngine;
using Unity.Profiling;

public class SegmentPool : MonoBehaviour
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

    // Índice del segmento que está más atrás
    private int oldestIndex;


    private class SegmentData
    {
        public GameObject GameObject;
        public Transform Transform;
        public SegmentEnemySpawns EnemySpawns;
        public SegmentContent Content;
    }


    private void Start()
    {
        CreateSegments();
    }


    // =========================
    // CREAR SEGMENTOS
    // =========================

    private void CreateSegments()
    {
        segments = new SegmentData[maxActiveSegments];

        float currentZ = firstSpawnZ;

        for (int i = 0; i < maxActiveSegments; i++)
        {
            GameObject segmentObject = Instantiate(
                segmentPrefab,
                new Vector3(0f, 0f, currentZ),
                Quaternion.identity,
                transform
            );

            SegmentData data = new SegmentData
            {
                GameObject = segmentObject,
                Transform = segmentObject.transform,

                EnemySpawns =
                    segmentObject.GetComponent<SegmentEnemySpawns>(),

                Content =
                    segmentObject.GetComponent<SegmentContent>()
            };

            segments[i] = data;

            SpawnEnemies(data);

            currentZ += segmentLength;
        }

        oldestIndex = 0;
    }


    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        MoveSegments();

        // Normalmente solo se reciclará uno.
        // Este while también cubre un frame con deltaTime muy grande.
        int safety = segments.Length;

        while (
            safety-- > 0 &&
            segments[oldestIndex].Transform.position.z < recycleZ
        )
        {
            RecycleOldestSegment();
        }
    }


    // =========================
    // MOVER SEGMENTOS
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
                Vector3.back * movement;

            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].Transform.position += offset;
            }
        }
    }


    // =========================
    // RECICLAR SEGMENTO
    // =========================

    private void RecycleOldestSegment()
    {
        using (RecycleMarker.Auto())
        {
            SegmentData segment =
                segments[oldestIndex];


            // =========================
            // LIMPIAR ENEMIGOS
            // =========================

            if (segment.Content != null &&
                enemyPool != null)
            {
                using (ClearEnemiesMarker.Auto())
                {
                    segment.Content.ClearEnemies(enemyPool);
                }
            }


            // =========================
            // BUSCAR POSICIÓN NUEVA
            // =========================

            // El segmento anterior a oldestIndex
            // siempre es el segmento que está más adelante.
            int lastIndex =
                (oldestIndex - 1 + segments.Length)
                % segments.Length;


            float newZ =
                segments[lastIndex].Transform.position.z
                + segmentLength;


            // =========================
            // MOVER SEGMENTO
            // =========================

            segment.Transform.position =
                new Vector3(
                    0f,
                    segment.Transform.position.y,
                    newZ
                );


            // =========================
            // GENERAR ENEMIGOS
            // =========================

            SpawnEnemies(segment);


            // =========================
            // ACTUALIZAR ÍNDICE
            // =========================

            oldestIndex =
                (oldestIndex + 1)
                % segments.Length;
        }
    }


    // =========================
    // SPAWN ENEMIGOS
    // =========================

    private void SpawnEnemies(SegmentData segment)
    {
        if (segment.EnemySpawns == null ||
            enemySpawnDirector == null)
        {
            return;
        }

        using (SpawnEnemiesMarker.Auto())
        {
            enemySpawnDirector.SpawnEnemiesOnSegment(
                segment.EnemySpawns
            );
        }
    }
}