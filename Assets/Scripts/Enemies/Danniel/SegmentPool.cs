using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;


public class SegmentPool : MonoBehaviour
{
    [SerializeField] GameObject segmentPrefab;
    [SerializeField] float speed;
    [SerializeField] int maxActiveSegments;

    [Header("Enemies")]
    [SerializeField] private EnemySpawnDirector enemySpawnDirector;
    [SerializeField] private EnemyPool enemyPool;


    private ObjectPool<GameObject> pool;

    private Queue<GameObject> activeSegments = new Queue<GameObject>();

    private float segmentLength = 80f;
    private float spawnZ = -160f;


    
    private void Awake()
    {
        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(segmentPrefab, transform),
            actionOnGet: segment => GetFunction(segment),
            actionOnRelease: segment => segment.SetActive(false),
            actionOnDestroy: segment => Destroy(segment),
            collectionCheck: false,
            maxSize: maxActiveSegments

        );
    }


    private void Start()
    {
        for (int i = 0; i < maxActiveSegments; i++)
        {
            pool.Get();
        }
    }


    private GameObject GetFunction(GameObject seg)
    {
        seg.SetActive(true);

        seg.transform.position =
            new Vector3(0, 0, spawnZ);

        spawnZ += segmentLength;

        activeSegments.Enqueue(seg);


        // Spawn de enemigos
        SegmentEnemySpawns enemySpawns =
            seg.GetComponent<SegmentEnemySpawns>();

        if (enemySpawns != null &&
            enemySpawnDirector != null)
        {
            enemySpawnDirector.SpawnEnemiesOnSegment(
                enemySpawns
            );
        }


        return seg;
    }


    private void Update()
    {
        // eliminar referencias destruidas al inicio de la cola
        while (activeSegments.Count > 0 &&
               activeSegments.Peek() == null)
        {
            activeSegments.Dequeue();
        }


        // mover segmentos
        foreach (var segment in activeSegments)
        {
            if (segment == null)
                continue;

            segment.transform.Translate(
                Vector3.back *
                speed *
                Time.deltaTime *
                2f
            );
        }


        if (activeSegments.Count > 0 &&
    activeSegments.Peek().transform.position.z <
    -segmentLength * 2f)
        {
            var seg = activeSegments.Dequeue();


            // DEVOLVER ENEMIGOS ANTERIORES
            SegmentContent content =
                seg.GetComponent<SegmentContent>();

            if (content != null)
            {
                content.ClearEnemies(enemyPool);
            }


            // BUSCAR ÚLTIMO SEGMENTO

            GameObject lastSegment = null;

            foreach (var s in activeSegments)
            {
                lastSegment = s;
            }


            float newZ =
                lastSegment.transform.position.z
                + segmentLength;



            // RECICLAR SEGMENTO
            seg.transform.position =
                new Vector3(0, 0, newZ);

            activeSegments.Enqueue(seg);



            // GENERAR NUEVOS ENEMIGOS
            SegmentEnemySpawns spawns =
                seg.GetComponent<SegmentEnemySpawns>();

            if (spawns != null &&
                enemySpawnDirector != null)
            {
                enemySpawnDirector.SpawnEnemiesOnSegment(
                    spawns
                );
            }
        }
    }



}