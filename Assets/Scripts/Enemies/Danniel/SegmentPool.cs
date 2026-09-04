using UnityEngine;
using Unity.Profiling;

using System.Collections;
using System.Collections.Generic;


public class SegmentPool :  MonoBehaviour, IExperiencePreloadable, IExperienceRuntime
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

    [SerializeField]
    private GameObject normalSegmentPrefab;

    [SerializeField]
    private GameObject rotatedSegmentPrefab;


    [SerializeField]
    private float speed = 1f;

    [SerializeField]
    private float speedMultiplier = 2f;


    [SerializeField]
    private int maxActiveSegments = 3;


    [Tooltip(
        "Posición inicial del primer segmento."
    )]
    [SerializeField]
    private float firstSpawnZ = 0f;


    [Tooltip(
        "Tiempo que se espera SOLO al inicio " +
        "antes de crear el segundo segmento."
    )]
    [SerializeField]
    private float secondSegmentDelay = 5f;


    [Tooltip(
        "Cuando el root del segmento pasa esta Z, " +
        "se recicla."
    )]
    [SerializeField]
    private float recycleZ = -80f;


    // =========================
    // ENEMIES
    // =========================

    [Header("Enemies")]

    [SerializeField]
    private EnemySpawnDirector enemySpawnDirector;

    [SerializeField]
    private EnemyPool enemyPool;


    // =========================
    // SEGMENT DATA
    // =========================

    private SegmentData[] segments;


    private int activeSegmentCount;

    private int oldestIndex;


    private bool isInitialized;

    private bool initialFillComplete;

    private bool isRunning;

    private Coroutine initialFillRoutine;


    // Alterna:
    //
    // NORMAL
    // ROTATED
    // NORMAL
    // ROTATED
    //
    private bool nextShouldBeRotated;


    // =========================
    // INACTIVE POOLS
    // =========================

    private readonly Stack<SegmentData> normalPool =
        new Stack<SegmentData>();


    private readonly Stack<SegmentData> rotatedPool =
        new Stack<SegmentData>();


    // =========================
    // SEGMENT DATA CLASS
    // =========================

    private class SegmentData
    {
        public GameObject GameObject;

        public Transform Transform;

        public SegmentAnchors Anchors;

        public SegmentEnemySpawns EnemySpawns;

        public SegmentContent Content;

        public bool IsRotated;
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


        // =========================
        // INITIAL STATE
        // =========================

        segments =
            new SegmentData[
                maxActiveSegments
            ];


        activeSegmentCount = 0;

        oldestIndex = 0;

        initialFillComplete = false;

        nextShouldBeRotated = false;


        normalPool.Clear();

        rotatedPool.Clear();


        // =========================
        // FIRST SEGMENT
        // =========================

        SegmentData firstSegment =
            GetSegment(
                false
            );


        if (firstSegment == null)
        {
            Debug.LogError(
                "No se pudo crear el primer segmento.",
                this
            );

            yield break;
        }


        firstSegment.Transform.position =
            new Vector3(
                0f,
                0f,
                firstSpawnZ
            );


        segments[0] =
            firstSegment;


        activeSegmentCount = 1;



        // Después del NORMAL
        // debe venir ROTATED.
        nextShouldBeRotated = true;


        // Permitimos que el primer
        // segmento empiece a moverse.
        isInitialized = true;


        Debug.Log(
            "Primer segmento cargado. " +
            "Esperando para cargar el segundo.",
            this
        );



        yield return null;
    }


    // =========================
    // INITIAL FILL
    // =========================

    private IEnumerator InitialFillRoutine()
    {
        // =========================
        // WAIT 5 SECONDS
        // =========================

        yield return new WaitForSeconds(
            secondSegmentDelay
        );


        // =========================
        // SECOND SEGMENT
        // =========================

        if (
            activeSegmentCount <
            maxActiveSegments
        )
        {
            AddInitialSegment();
        }


        // Evitamos crear segundo y tercero
        // exactamente en el mismo frame.
        yield return null;


        // =========================
        // REMAINING SEGMENTS
        // =========================

        while (
            activeSegmentCount <
            maxActiveSegments
        )
        {
            AddInitialSegment();

            yield return null;
        }


        // =========================
        // POOLING READY
        // =========================

        oldestIndex = 0;

        initialFillComplete = true;

        initialFillRoutine = null;


        Debug.Log(
            $"Carga inicial terminada. " +
            $"Segmentos activos: " +
            $"{activeSegmentCount}",
            this
        );
    }


    // =========================
    // ADD INITIAL SEGMENT
    // =========================

    private void AddInitialSegment()
    {
        if (
            activeSegmentCount <= 0 ||
            activeSegmentCount >=
            maxActiveSegments
        )
        {
            return;
        }


        SegmentData previousSegment =
            segments[
                activeSegmentCount - 1
            ];


        SegmentData newSegment =
            GetSegment(
                nextShouldBeRotated
            );


        if (newSegment == null)
        {
            return;
        }


        // =========================
        // CONNECT SEGMENTS
        // =========================

        PlaceAfter(
            previousSegment,
            newSegment
        );


        // =========================
        // STORE
        // =========================

        segments[
            activeSegmentCount
        ] =
            newSegment;


        activeSegmentCount++;


        // =========================
        // ENEMIES
        // =========================

        SpawnEnemies(
            newSegment
        );


        // =========================
        // NEXT TYPE
        // =========================

        nextShouldBeRotated =
            !nextShouldBeRotated;


        Debug.Log(
            $"Segmento agregado. " +
            $"Tipo: " +
            $"{(newSegment.IsRotated ? "ROTATED" : "NORMAL")} | " +
            $"Activos: {activeSegmentCount}",
            newSegment.GameObject
        );
    }


    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        if (
        !isInitialized ||
        !isRunning
    )
        {
            return;
        }


        if (activeSegmentCount == 0)
        {
            return;
        }


        // =========================
        // MOVEMENT
        // =========================

        MoveSegments();


        // Todavía estamos en los
        // primeros 5 segundos / llenado.
        if (!initialFillComplete)
        {
            return;
        }


        // =========================
        // RECYCLE
        // =========================

        int safety =
            activeSegmentCount;


        while (
            safety-- > 0 &&
            segments[oldestIndex]
                .Transform
                .position
                .z < recycleZ
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


            for (
                int i = 0;
                i < activeSegmentCount;
                i++
            )
            {
                if (segments[i] == null)
                {
                    continue;
                }


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
            // =========================
            // OLD SEGMENT
            // =========================

            SegmentData oldSegment =
                segments[
                    oldestIndex
                ];


            // =========================
            // CLEAR ENEMIES
            // =========================

            if (
                oldSegment.Content != null &&
                enemyPool != null
            )
            {
                using (
                    ClearEnemiesMarker.Auto()
                )
                {
                    oldSegment.Content
                        .ClearEnemies(
                            enemyPool
                        );
                }
            }


            // =========================
            // FIND LAST
            // =========================

            int lastIndex =
                (
                    oldestIndex -
                    1 +
                    activeSegmentCount
                )
                %
                activeSegmentCount;


            SegmentData lastSegment =
                segments[
                    lastIndex
                ];


            // =========================
            // RETURN OLD TO POOL
            // =========================

            ReturnToPool(
                oldSegment
            );


            // =========================
            // GET NEXT TYPE
            // =========================

            SegmentData newSegment =
                GetSegment(
                    nextShouldBeRotated
                );


            if (newSegment == null)
            {
                Debug.LogError(
                    "No se pudo obtener el siguiente segmento.",
                    this
                );

                return;
            }


            // =========================
            // CONNECT EXACTLY
            // =========================

            PlaceAfter(
                lastSegment,
                newSegment
            );


            // =========================
            // REPLACE ARRAY SLOT
            // =========================

            segments[
                oldestIndex
            ] =
                newSegment;


            // =========================
            // NEW ENEMIES
            // =========================

            SpawnEnemies(
                newSegment
            );


            // =========================
            // NEXT TYPE
            // =========================

            nextShouldBeRotated =
                !nextShouldBeRotated;


            // =========================
            // NEXT OLDEST
            // =========================

            oldestIndex =
                (
                    oldestIndex + 1
                )
                %
                activeSegmentCount;
        }
    }


    // =========================
    // PLACE AFTER
    // =========================

    private void PlaceAfter(
        SegmentData previousSegment,
        SegmentData newSegment
    )
    {
        if (
            previousSegment == null ||
            newSegment == null
        )
        {
            return;
        }


        if (
            previousSegment.Anchors == null ||
            previousSegment.Anchors.EndPoint == null
        )
        {
            Debug.LogError(
                "El segmento anterior no tiene EndPoint.",
                previousSegment.GameObject
            );

            return;
        }


        if (
            newSegment.Anchors == null ||
            newSegment.Anchors.StartPoint == null
        )
        {
            Debug.LogError(
                "El nuevo segmento no tiene StartPoint.",
                newSegment.GameObject
            );

            return;
        }


        // =========================
        // EXACT CONNECTION
        // =========================

        Vector3 targetPosition =
            previousSegment
                .Anchors
                .EndPoint
                .position;


        Vector3 currentStartPosition =
            newSegment
                .Anchors
                .StartPoint
                .position;


        Vector3 difference =
            targetPosition -
            currentStartPosition;


        // Movemos TODO el segmento
        // exactamente la diferencia
        // necesaria.
        newSegment.Transform.position +=
            difference;


        Debug.Log(
            $"Segmentos conectados. " +
            $"End anterior: {targetPosition} | " +
            $"Start nuevo: " +
            $"{newSegment.Anchors.StartPoint.position}",
            newSegment.GameObject
        );
    }


    // =========================
    // GET SEGMENT
    // =========================

    private SegmentData GetSegment(
        bool isRotated
    )
    {
        Stack<SegmentData> selectedPool =
            isRotated
                ? rotatedPool
                : normalPool;


        // =========================
        // REUSE
        // =========================

        if (selectedPool.Count > 0)
        {
            SegmentData segment =
                selectedPool.Pop();


            segment
                .GameObject
                .SetActive(true);


            return segment;
        }


        // =========================
        // CREATE
        // =========================

        return CreateSegment(
            isRotated
        );
    }


    // =========================
    // CREATE SEGMENT
    // =========================

    private SegmentData CreateSegment(
        bool isRotated
    )
    {
        GameObject prefab =
            isRotated
                ? rotatedSegmentPrefab
                : normalSegmentPrefab;


        if (prefab == null)
        {
            Debug.LogError(
                isRotated
                    ? "Rotated Segment Prefab no está asignado."
                    : "Normal Segment Prefab no está asignado.",
                this
            );

            return null;
        }


        // IMPORTANTE:
        // usamos la rotación propia
        // del prefab.
        //
        // SegmentPool NO rota nada.
        GameObject segmentObject =
            Instantiate(
                prefab,
                Vector3.zero,
                prefab.transform.rotation,
                transform
            );


        SegmentData data =
            new SegmentData
            {
                GameObject =
                    segmentObject,

                Transform =
                    segmentObject.transform,

                Anchors =
                    segmentObject
                        .GetComponent<
                            SegmentAnchors>(),

                EnemySpawns =
                    segmentObject
                        .GetComponent<
                            SegmentEnemySpawns>(),

                Content =
                    segmentObject
                        .GetComponent<
                            SegmentContent>(),

                IsRotated =
                    isRotated
            };


        if (data.Anchors == null)
        {
            Debug.LogError(
                "El prefab necesita SegmentAnchors.",
                segmentObject
            );
        }


        Debug.Log(
            $"Segmento físico creado: " +
            $"{(isRotated ? "ROTATED" : "NORMAL")}",
            segmentObject
        );


        return data;
    }


    // =========================
    // RETURN TO POOL
    // =========================

    private void ReturnToPool(
        SegmentData segment
    )
    {
        segment
            .GameObject
            .SetActive(false);


        if (segment.IsRotated)
        {
            rotatedPool.Push(
                segment
            );
        }
        else
        {
            normalPool.Push(
                segment
            );
        }
    }


    // =========================
    // SPAWN ENEMIES
    // =========================

    private void SpawnEnemies(
        SegmentData segment
    )
    {
        if (
            segment == null ||
            segment.EnemySpawns == null ||
            enemySpawnDirector == null
        )
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

    // =========================
    // EXPERIENCE START
    // =========================

    public void BeginExperience()
    {
        if (!isInitialized)
        {
            Debug.LogError(
                "[SegmentPool] No está preparado.",
                this
            );

            return;
        }


        if (isRunning)
        {
            return;
        }


        isRunning = true;


        // =========================
        // FIRST ENEMIES
        // =========================

        if (
            activeSegmentCount > 0 &&
            segments[0] != null
        )
        {
            SpawnEnemies(
                segments[0]
            );
        }


        // =========================
        // INITIAL FILL
        // =========================

        initialFillRoutine =
            StartCoroutine(
                InitialFillRoutine()
            );


        Debug.Log(
            "[SegmentPool] Gameplay iniciado.",
            this
        );
    }


    // =========================
    // EXPERIENCE END
    // =========================

    public void EndExperience()
    {
        if (!isRunning)
        {
            return;
        }


        isRunning = false;


        if (initialFillRoutine != null)
        {
            StopCoroutine(
                initialFillRoutine
            );

            initialFillRoutine = null;
        }


        Debug.Log(
            "[SegmentPool] Gameplay detenido.",
            this
        );
    }

}