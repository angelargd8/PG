using UnityEngine;
using Unity.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;

public class SegmentPool : MonoBehaviour, IExperiencePreloadable, IExperienceRuntime
{
    private static readonly ProfilerMarker MoveMarker =
        new ProfilerMarker("SegmentPool.Move");

    private static readonly ProfilerMarker RecycleMarker =
        new ProfilerMarker("SegmentPool.Recycle");

    private static readonly ProfilerMarker ClearEnemiesMarker =
        new ProfilerMarker("SegmentPool.ClearEnemies");

    private static readonly ProfilerMarker SpawnEnemiesMarker =
        new ProfilerMarker("SegmentPool.SpawnEnemies");


    [Header("Segments")]

    [SerializeField]
    private GameObject normalSegmentPrefab;

    [SerializeField]
    private GameObject rotatedSegmentPrefab;

    [SerializeField]
    private float speed = 1.3f;

    private float _difficultySpeedScale = 1f;

    [Tooltip("Multiplicador final de velocidad.")]
    [Min(0f)]
    [SerializeField]
    private float speedMultiplier = 4f;

    [SerializeField]
    private int maxActiveSegments = 4;


    [Header("Android Optimization")]

    [SerializeField]
    private bool useAndroidSegmentSettings = true;

    [Min(2)]
    [SerializeField]
    private int androidMaxActiveSegments = 3;

    [Min(0)]
    [SerializeField]
    private int androidRecycleAdvanceSegments = 1;


    [Header("Enemy Platform Optimization")]

    [SerializeField]
    private bool reduceEnemiesOnQuest2 = true;

    [Min(1)]
    [SerializeField]
    private int quest2EnemiesPerSegment = 3;

    [Min(1)]
    [SerializeField]
    private int quest3EnemiesPerSegment = 4;

    [Min(1)]
    [SerializeField]
    private int pcEnemiesPerSegment = 6;

    [SerializeField]
    private int runtimeEnemiesPerSegment = 4;


    [SerializeField]
    private float firstSpawnZ = 0f;

    [SerializeField]
    private float secondSegmentDelay = 1f;//1

    [SerializeField]
    private float recycleZ = -80f;


    [Header("Progressive Speed")]

    [SerializeField]
    private bool useProgressiveSpeed = true;

    [Tooltip("Velocidad lenta durante los primeros segundos.")]
    [Min(0f)]
    [SerializeField]
    private float startSpeedMultiplier = 1.5f;

    [Tooltip("Tiempo inicial lento para que el jugador observe el entorno.")]
    [Min(0f)]
    [SerializeField]
    private float introDuration = 5f;

    [Tooltip("Velocidad normal alcanzada despues de la introduccion.")]
    [Min(0f)]
    [SerializeField]
    private float normalSpeedMultiplier = 2.21f;

    [Tooltip("Tiempo para pasar suavemente de la velocidad inicial a la normal.")]
    [Min(0.01f)]
    [SerializeField]
    private float normalTransitionDuration = 2f;

    [Tooltip("Tiempo para pasar desde la velocidad normal hasta Speed Multiplier.")]
    [Min(0.01f)]
    [SerializeField]
    private float accelerationDuration = 60f;

    [SerializeField]
    private ExperienceMusicClock musicClock;

    [SerializeField]
    private float currentSpeedMultiplier;


    private double speedStartSongTime;
    private bool hasSpeedStartSongTime;


    public float CurrentSpeedMultiplier =>
        currentSpeedMultiplier;

    public event Action<int, DifficultyLevel> EnemiesMissed;


    [Header("Enemies")]

    [SerializeField]
    private EnemySpawnDirector enemySpawnDirector;

    [SerializeField]
    private EnemyPool enemyPool;


    private SegmentData[] segments;

    private int activeSegmentCount;
    private int segmentLimit;
    private bool useAndroidBudget;

    public int ActiveSegmentCount =>
        activeSegmentCount;

    public int SegmentLimit =>
        segmentLimit;

    public int RuntimeEnemiesPerSegment =>
        runtimeEnemiesPerSegment;


    internal int ResolveSegmentLimit(RuntimePlatform platform)
    {
        return
            useAndroidSegmentSettings &&
            platform == RuntimePlatform.Android
                ? Mathf.Max(2, androidMaxActiveSegments)
                : Mathf.Max(1, maxActiveSegments);
    }


    private int ResolveEnemyLimit(RuntimePlatform platform)
    {
        int quest2Limit =
            Mathf.Max(1, quest2EnemiesPerSegment);

        int quest3Limit =
            Mathf.Max(1, quest3EnemiesPerSegment);

        int pcLimit =
            Mathf.Max(1, pcEnemiesPerSegment);

        if (platform != RuntimePlatform.Android)
        {
            if (logLifecycle)
            {
                Debug.Log(
                    $"[SegmentPool] PC / Editor / Quest Link | Enemigos: {pcLimit}",
                    this
                );
            }

            return pcLimit;
        }

        string deviceModel =
            SystemInfo.deviceModel ?? string.Empty;

        string deviceName =
            SystemInfo.deviceName ?? string.Empty;

        string deviceInfo =
            deviceModel + " " + deviceName;

        bool isQuest2 =
            deviceInfo.IndexOf(
                "Quest 2",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        if (reduceEnemiesOnQuest2 && isQuest2)
        {
            Debug.Log(
                $"[SegmentPool] Quest 2 detectado | Enemigos: {quest2Limit}",
                this
            );

            return quest2Limit;
        }

        Debug.Log(
            $"[SegmentPool] Android / Quest 3 / Quest 3S | " +
            $"Model: {deviceModel} | " +
            $"Name: {deviceName} | " +
            $"Enemigos: {quest3Limit}",
            this
        );

        return quest3Limit;
    }


    private int oldestIndex;


    [Header("Debug")]

    [SerializeField]
    private bool logLifecycle;


    private bool isInitialized;
    private bool initialFillComplete;
    private bool isRunning;

    private Coroutine initialFillRoutine;

    private bool nextShouldBeRotated;


    private readonly Stack<SegmentData> normalPool =
        new Stack<SegmentData>();

    private readonly Stack<SegmentData> rotatedPool =
        new Stack<SegmentData>();


    private class SegmentData
    {
        public GameObject GameObject;
        public Transform Transform;
        public SegmentAnchors Anchors;
        public SegmentEnemySpawns EnemySpawns;
        public SegmentContent Content;
        public bool IsRotated;
    }


    public IEnumerator Preload()
    {
        return PreloadForPlatform(
            Application.platform
        );
    }


    internal IEnumerator PreloadForPlatform(
        RuntimePlatform platform
    )
    {
        if (isInitialized)
        {
            yield break;
        }

        if (logLifecycle)
        {
            Debug.Log(
                "SegmentPool comienza Preload.",
                this
            );
        }

        useAndroidBudget =
            useAndroidSegmentSettings &&
            platform == RuntimePlatform.Android;

        segmentLimit =
            ResolveSegmentLimit(
                platform
            );

        runtimeEnemiesPerSegment =
            ResolveEnemyLimit(
                platform
            );

        if (enemySpawnDirector != null)
        {
            enemySpawnDirector.SetMaxEnemiesPerSegment(
                runtimeEnemiesPerSegment
            );
        }

        segments =
            new SegmentData[
                segmentLimit
            ];

        activeSegmentCount = 0;
        oldestIndex = 0;
        initialFillComplete = false;
        nextShouldBeRotated = false;

        normalPool.Clear();
        rotatedPool.Clear();

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
        nextShouldBeRotated = true;

        yield return null;

        int instancesPerType =
            (segmentLimit + 1) / 2;

        for (
            int i = 1;
            i < instancesPerType;
            i++
        )
        {
            SegmentData segment =
                CreateSegment(
                    false
                );

            if (segment == null)
            {
                yield break;
            }

            ReturnToPool(
                segment
            );

            yield return null;
        }

        for (
            int i = 0;
            i < instancesPerType;
            i++
        )
        {
            SegmentData segment =
                CreateSegment(
                    true
                );

            if (segment == null)
            {
                yield break;
            }

            ReturnToPool(
                segment
            );

            yield return null;
        }

        if (enemySpawnDirector != null)
        {
            yield return enemySpawnDirector.PreloadForSegments(
                segmentLimit
            );
        }

        isInitialized = true;

        if (logLifecycle)
        {
            Debug.Log(
                "Primer segmento cargado. Esperando para cargar el segundo.",
                this
            );
        }

        yield return null;
    }


    private IEnumerator InitialFillRoutine()
    {
        yield return new WaitForSeconds(
            secondSegmentDelay
        );

        if (
            activeSegmentCount <
            segmentLimit
        )
        {
            AddInitialSegment();
        }

        yield return null;

        while (
            activeSegmentCount <
            segmentLimit
        )
        {
            AddInitialSegment();

            yield return null;
        }

        oldestIndex = 0;
        initialFillComplete = true;
        initialFillRoutine = null;

        if (logLifecycle)
        {
            Debug.Log(
                $"Carga inicial terminada. Segmentos activos: {activeSegmentCount}",
                this
            );
        }
    }


    private void AddInitialSegment()
    {
        if (
            activeSegmentCount <= 0 ||
            activeSegmentCount >= segmentLimit
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

        PlaceAfter(
            previousSegment,
            newSegment
        );

        segments[
            activeSegmentCount
        ] = newSegment;

        activeSegmentCount++;

        SpawnEnemies(
            newSegment
        );

        nextShouldBeRotated =
            !nextShouldBeRotated;

        if (logLifecycle)
        {
            Debug.Log(
                $"Segmento agregado. Tipo: " +
                $"{(newSegment.IsRotated ? "ROTATED" : "NORMAL")} | " +
                $"Activos: {activeSegmentCount}",
                newSegment.GameObject
            );
        }
    }


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

        if (!UpdateSpeedProgression())
        {
            return;
        }

        MoveSegments();

        if (!initialFillComplete)
        {
            return;
        }

        int safety =
            activeSegmentCount;

        while (
            safety-- > 0 &&
            segments[oldestIndex]
                .Transform
                .position
                .z <
            GetRecycleZ(
                segments[oldestIndex]
            )
        )
        {
            RecycleOldestSegment();
        }
    }


    private float GetRecycleZ(
        SegmentData segment
    )
    {
        if (
            !useAndroidBudget ||
            androidRecycleAdvanceSegments <= 0 ||
            segment.Anchors == null ||
            segment.Anchors.StartPoint == null ||
            segment.Anchors.EndPoint == null
        )
        {
            return recycleZ;
        }

        float length =
            Mathf.Abs(
                segment.Anchors.EndPoint.position.z -
                segment.Anchors.StartPoint.position.z
            );

        float endOffset =
            segment.Anchors.EndPoint.position.z -
            segment.Transform.position.z;

        return Mathf.Min(
            recycleZ +
            length *
            androidRecycleAdvanceSegments,

            firstSpawnZ -
            endOffset -
            1f
        );
    }


    private void ResetSpeedProgression()
    {
        hasSpeedStartSongTime = false;

        float finalMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        float normalMultiplier =
            Mathf.Clamp(
                normalSpeedMultiplier,
                0f,
                finalMultiplier
            );

        float initialMultiplier =
            Mathf.Clamp(
                startSpeedMultiplier,
                0f,
                normalMultiplier
            );

        currentSpeedMultiplier =
            useProgressiveSpeed
                ? initialMultiplier
                : finalMultiplier;

        if (!useProgressiveSpeed)
        {
            return;
        }

        if (musicClock == null)
        {
            musicClock =
                FindFirstObjectByType<
                    ExperienceMusicClock
                >();
        }

        if (musicClock == null)
        {
            Debug.LogWarning(
                "[SegmentPool] No se encontro ExperienceMusicClock. " +
                "Se mantiene la velocidad inicial. " +
                "Inicia desde Bootstrap para cargar ExperienceCore.",
                this
            );

            return;
        }

        if (musicClock.IsPlaying)
        {
            speedStartSongTime =
                musicClock.SongTime;

            hasSpeedStartSongTime =
                true;
        }
    }


    public void SetDifficultySpeedScale(
        float speedScale
    )
    {
        _difficultySpeedScale =
            Mathf.Max(
                0f,
                speedScale
            );
    }


    private bool UpdateSpeedProgression()
    {
        if (
            Time.timeScale <= 0f ||
            AudioListener.pause
        )
        {
            return false;
        }

        float finalMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        if (!useProgressiveSpeed)
        {
            currentSpeedMultiplier =
                finalMultiplier;

            return true;
        }

        float normalMultiplier =
            Mathf.Clamp(
                normalSpeedMultiplier,
                0f,
                finalMultiplier
            );

        float initialMultiplier =
            Mathf.Clamp(
                startSpeedMultiplier,
                0f,
                normalMultiplier
            );

        if (musicClock == null)
        {
            currentSpeedMultiplier =
                initialMultiplier;

            return true;
        }

        if (!musicClock.IsPlaying)
        {
            return false;
        }

        double songTime =
            musicClock.SongTime;

        if (
            !hasSpeedStartSongTime ||
            songTime < speedStartSongTime
        )
        {
            speedStartSongTime =
                songTime;

            hasSpeedStartSongTime =
                true;
        }

        float elapsed =
            (float)(
                songTime -
                speedStartSongTime
            );

        float introTime =
            Mathf.Max(
                0f,
                introDuration
            );

        float transitionTime =
            Mathf.Max(
                0.01f,
                normalTransitionDuration
            );

        float accelerationTime =
            Mathf.Max(
                0.01f,
                accelerationDuration
            );


        if (elapsed < introTime)
        {
            currentSpeedMultiplier =
                initialMultiplier;

            return true;
        }


        float transitionElapsed =
            elapsed -
            introTime;

        if (transitionElapsed < transitionTime)
        {
            float transitionProgress =
                transitionElapsed /
                transitionTime;

            currentSpeedMultiplier =
                Mathf.Lerp(
                    initialMultiplier,
                    normalMultiplier,
                    transitionProgress
                );

            return true;
        }


        float accelerationElapsed =
            transitionElapsed -
            transitionTime;

        float accelerationProgress =
            accelerationElapsed /
            accelerationTime;

        currentSpeedMultiplier =
            Mathf.Lerp(
                normalMultiplier,
                finalMultiplier,
                accelerationProgress
            );

        return true;
    }


    private void MoveSegments()
    {
        using (MoveMarker.Auto())
        {
            float movement =
                speed *
                currentSpeedMultiplier *
                _difficultySpeedScale *
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


    private void RecycleOldestSegment()
    {
        using (RecycleMarker.Auto())
        {
            SegmentData oldSegment =
                segments[
                    oldestIndex
                ];

            if (
                oldSegment.Content != null &&
                enemyPool != null
            )
            {
                using (
                    ClearEnemiesMarker.Auto()
                )
                {
                    int missedEnemies =
                        oldSegment
                            .Content
                            .ActiveEnemyCount;

                    if (missedEnemies > 0)
                    {
                        EnemiesMissed?.Invoke(
                            missedEnemies,
                            oldSegment
                                .Content
                                .SpawnDifficulty
                        );
                    }

                    oldSegment
                        .Content
                        .ClearEnemies(
                            enemyPool
                        );
                }
            }

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

            ReturnToPool(
                oldSegment
            );

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

            PlaceAfter(
                lastSegment,
                newSegment
            );

            segments[
                oldestIndex
            ] = newSegment;

            SpawnEnemies(
                newSegment
            );

            nextShouldBeRotated =
                !nextShouldBeRotated;

            oldestIndex =
                (
                    oldestIndex + 1
                )
                %
                activeSegmentCount;
        }
    }


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

        newSegment.Transform.position +=
            difference;

        if (logLifecycle)
        {
            Debug.Log(
                $"Segmentos conectados. " +
                $"End anterior: {targetPosition} | " +
                $"Start nuevo: {newSegment.Anchors.StartPoint.position}",
                newSegment.GameObject
            );
        }
    }


    private SegmentData GetSegment(
        bool isRotated
    )
    {
        Stack<SegmentData> selectedPool =
            isRotated
                ? rotatedPool
                : normalPool;

        if (selectedPool.Count > 0)
        {
            SegmentData segment =
                selectedPool.Pop();

            segment
                .GameObject
                .SetActive(
                    true
                );

            return segment;
        }

        return CreateSegment(
            isRotated
        );
    }


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
                    ? "Rotated Segment Prefab no esta asignado."
                    : "Normal Segment Prefab no esta asignado.",
                this
            );

            return null;
        }

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
                            SegmentAnchors
                        >(),

                EnemySpawns =
                    segmentObject
                        .GetComponent<
                            SegmentEnemySpawns
                        >(),

                Content =
                    segmentObject
                        .GetComponent<
                            SegmentContent
                        >(),

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

        if (logLifecycle)
        {
            Debug.Log(
                $"Segmento fisico creado: " +
                $"{(isRotated ? "ROTATED" : "NORMAL")}",
                segmentObject
            );
        }

        return data;
    }


    private void ReturnToPool(
        SegmentData segment
    )
    {
        segment
            .GameObject
            .SetActive(
                false
            );

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


    public void BeginExperience()
    {
        if (!isInitialized)
        {
            Debug.LogError(
                "[SegmentPool] No esta preparado.",
                this
            );

            return;
        }

        if (isRunning)
        {
            return;
        }

        ResetSpeedProgression();

        isRunning = true;

        if (
            activeSegmentCount > 0 &&
            segments[0] != null
        )
        {
            SpawnEnemies(
                segments[0]
            );
        }

        initialFillRoutine =
            StartCoroutine(
                InitialFillRoutine()
            );

        if (logLifecycle)
        {
            Debug.Log(
                "[SegmentPool] Gameplay iniciado.",
                this
            );
        }
    }


    public void EndExperience()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        hasSpeedStartSongTime = false;

        if (initialFillRoutine != null)
        {
            StopCoroutine(
                initialFillRoutine
            );

            initialFillRoutine = null;
        }

        if (logLifecycle)
        {
            Debug.Log(
                "[SegmentPool] Gameplay detenido.",
                this
            );
        }
    }
}