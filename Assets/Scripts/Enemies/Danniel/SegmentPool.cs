using UnityEngine;

public class SegmentPool : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] private GameObject segmentPrefab;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float speedMultiplier = 2f;

    [SerializeField] private int maxActiveSegments = 3;

    [SerializeField] private float segmentLength = 80f;
    [SerializeField] private float firstSpawnZ = -160f;
    [SerializeField] private float recycleZ = -160f;


    [Header("Enemies")]
    [SerializeField] private EnemySpawnDirector enemySpawnDirector;
    [SerializeField] private EnemyPool enemyPool;


    private SegmentData[] segments;

    // indice del segmento que esta mas atrás
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


    private void Update()
    {
        MoveSegments();

        // Normalmente solo sera uno
        // El while tmb cubre un frame con deltaTime muy grande
        int safety = segments.Length;

        while (
            safety-- > 0 &&
            segments[oldestIndex].Transform.position.z < recycleZ
        )
        {
            RecycleOldestSegment();
        }
    }


    private void MoveSegments()
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


    private void RecycleOldestSegment()
    {
        SegmentData segment =
            segments[oldestIndex];


        // Limpiar enemigos del segmento anterior
        if (segment.Content != null &&
            enemyPool != null)
        {
            segment.Content.ClearEnemies(enemyPool);
        }


        // El segmento anterior al oldestIndex
        // es siempre el segmento que está más adelante
        int lastIndex =
            (oldestIndex - 1 + segments.Length)
            % segments.Length;


        float newZ =
            segments[lastIndex].Transform.position.z
            + segmentLength;


        segment.Transform.position =
            new Vector3(
                0f,
                segment.Transform.position.y,
                newZ
            );


        // Generar enemigos nuevos
        SpawnEnemies(segment);


        // El siguiente pasa a ser el más viejo
        oldestIndex =
            (oldestIndex + 1)
            % segments.Length;
    }


    private void SpawnEnemies(SegmentData segment)
    {
        if (segment.EnemySpawns == null ||
            enemySpawnDirector == null)
        {
            return;
        }

        enemySpawnDirector.SpawnEnemiesOnSegment(
            segment.EnemySpawns
        );
    }
}