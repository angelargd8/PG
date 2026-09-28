using UnityEngine;
using Unity.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;


public class SegmentPool :  MonoBehaviour, IExperiencePreloadable, IExperienceRuntime
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
    private float speed = 1f;
    private float _difficultySpeedScale = 1f;

    [Tooltip("Multiplicador fijo, o final cuando Use Progressive Speed esta activado.")]
    [Min(0f)]
    [SerializeField]
    private float speedMultiplier = 2f;


    [SerializeField]
    private int maxActiveSegments = 3;

    [Header("Android Optimization")]
    [Tooltip("Usar el presupuesto reducido solo en el APK de Android. Editor y Quest Link conservan Max Active Segments.")]
    [SerializeField] private bool useAndroidSegmentSettings;

    [Min(2)]
    [SerializeField] private int androidMaxActiveSegments = 3;

    [Tooltip("Adelantar el reciclaje del segmento trasero esta cantidad de longitudes de segmento.")]
    [Min(0)]
    [SerializeField] private int androidRecycleAdvanceSegments = 1;


    [Tooltip(
        "Posicion inicial del primer segmento."
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


    [Header("Progressive Speed")]
    [Tooltip("Aumenta la velocidad con el tiempo musical transcurrido en esta escena.")]
    [SerializeField] private bool useProgressiveSpeed;

    [Tooltip("Multiplicador inicial. Se limita al valor final de Speed Multiplier.")]
    [Min(0f)]
    [SerializeField] private float startSpeedMultiplier = 1.5f;

    [Tooltip("Segundos de musica en esta escena para alcanzar Speed Multiplier.")]
    [Min(0.01f)]
    [SerializeField] private float accelerationDuration = 120f;

    [Tooltip("Opcional. Se busca en ExperienceCore al comenzar la experiencia.")]
    [SerializeField] private ExperienceMusicClock musicClock;

    [Tooltip("Valor calculado durante Play. No es un ajuste de velocidad.")]
    [SerializeField] private float currentSpeedMultiplier;

    private double speedStartSongTime;
    private bool hasSpeedStartSongTime;

    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public event Action<int, DifficultyLevel> EnemiesMissed;


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
    private int segmentLimit;
    private bool useAndroidBudget;

    public int ActiveSegmentCount => activeSegmentCount;
    public int SegmentLimit => segmentLimit;

    internal int ResolveSegmentLimit(RuntimePlatform platform) =>
        useAndroidSegmentSettings && platform == RuntimePlatform.Android
            ? Mathf.Max(2, androidMaxActiveSegments)
            : Mathf.Max(1, maxActiveSegments);

    private int oldestIndex;


    [Header("Debug")]
    [SerializeField] private bool logLifecycle;

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


    public IEnumerator Preload() => PreloadForPlatform(Application.platform);

    internal IEnumerator PreloadForPlatform(RuntimePlatform platform)
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



        useAndroidBudget = useAndroidSegmentSettings && platform == RuntimePlatform.Android;
        segmentLimit = ResolveSegmentLimit(platform);
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



        // despues del NORMAL
        // debe venir ROTATED.
        nextShouldBeRotated = true;


        // Crear el resto durante Loading, distribuyendo las instancias por frame.
        // Para cantidades impares hace falta una reserva de ambos tipos:
        // al reciclar se alterna cual de ellos ocupa mas segmentos activos.
        yield return null;
        int instancesPerType = (segmentLimit + 1) / 2;
        for (int i = 1; i < instancesPerType; i++)
        {
            SegmentData segment = CreateSegment(false);
            if (segment == null)
            {
                yield break;
            }

            ReturnToPool(segment);
            yield return null;
        }

        for (int i = 0; i < instancesPerType; i++)
        {
            SegmentData segment = CreateSegment(true);
            if (segment == null)
            {
                yield break;
            }

            ReturnToPool(segment);
            yield return null;
        }

        if (enemySpawnDirector != null)
        {
            yield return enemySpawnDirector.PreloadForSegments(segmentLimit);
        }

        isInitialized = true;


        if (logLifecycle)
        {
            Debug.Log(
                "Primer segmento cargado. " +
                "Esperando para cargar el segundo.",
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
                $"Carga inicial terminada. " +
                $"Segmentos activos: " +
                $"{activeSegmentCount}",
                this
            );
        }
    }


    private void AddInitialSegment()
    {
        if (
            activeSegmentCount <= 0 ||
            activeSegmentCount >=
            segmentLimit
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
        ] =
            newSegment;


        activeSegmentCount++;




        SpawnEnemies(
            newSegment
        );



        nextShouldBeRotated =
            !nextShouldBeRotated;


        if (logLifecycle)
        {
            Debug.Log(
                $"Segmento agregado. " +
                $"Tipo: " +
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
                .z < GetRecycleZ(segments[oldestIndex])
        )
        {
            RecycleOldestSegment();
        }
    }


    private float GetRecycleZ(SegmentData segment)
    {
        if (!useAndroidBudget || androidRecycleAdvanceSegments <= 0 ||
            segment.Anchors == null || segment.Anchors.StartPoint == null ||
            segment.Anchors.EndPoint == null) return recycleZ;

        float length = Mathf.Abs(segment.Anchors.EndPoint.position.z - segment.Anchors.StartPoint.position.z);
        float endOffset = segment.Anchors.EndPoint.position.z - segment.Transform.position.z;
        // Never recycle a segment until its end is behind the initial play area.
        return Mathf.Min(recycleZ + length * androidRecycleAdvanceSegments,
            firstSpawnZ - endOffset - 1f);
    }

    private void ResetSpeedProgression()
    {
        hasSpeedStartSongTime = false;
        currentSpeedMultiplier = useProgressiveSpeed
            ? Mathf.Clamp(startSpeedMultiplier, 0f, Mathf.Max(0f, speedMultiplier))
            : Mathf.Max(0f, speedMultiplier);

        if (!useProgressiveSpeed)
        {
            return;
        }

        if (musicClock == null)
        {
            musicClock = FindFirstObjectByType<ExperienceMusicClock>();
        }

        if (musicClock == null)
        {
            Debug.LogWarning(
                "[SegmentPool] No se encontro ExperienceMusicClock. " +
                "Se mantiene la velocidad inicial. Inicia desde Bootstrap para cargar ExperienceCore.",
                this);
            return;
        }

        // Si la musica ya sigue sonando al entrar en Danniel, contar desde aqui.
        // Si aun no arranca, capturar el inicio en el primer frame de reproduccion.
        if (musicClock.IsPlaying)
        {
            speedStartSongTime = musicClock.SongTime;
            hasSpeedStartSongTime = true;
        }
    }


    public void SetDifficultySpeedScale(float speedScale)
    {
        _difficultySpeedScale =
            Mathf.Max(0f, speedScale);
    }


    private bool UpdateSpeedProgression()
    {
        if (Time.timeScale <= 0f || AudioListener.pause)
        {
            return false;
        }

        float finalMultiplier = Mathf.Max(0f, speedMultiplier);
        if (!useProgressiveSpeed)
        {
            currentSpeedMultiplier = finalMultiplier;
            return true;
        }

        float initialMultiplier = Mathf.Clamp(startSpeedMultiplier, 0f, finalMultiplier);
        if (musicClock == null)
        {
            currentSpeedMultiplier = initialMultiplier;
            return true;
        }

        if (!musicClock.IsPlaying)
        {
            return false;
        }

        double songTime = musicClock.SongTime;
        if (!hasSpeedStartSongTime || songTime < speedStartSongTime)
        {
            // Tambien reinicia al volver a un punto anterior al inicio de esta escena.
            speedStartSongTime = songTime;
            hasSpeedStartSongTime = true;
        }

        float progress = (float)((songTime - speedStartSongTime) /
            Mathf.Max(0.01f, accelerationDuration));
        currentSpeedMultiplier = Mathf.Lerp(initialMultiplier, finalMultiplier, progress);
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
                    int missedEnemies =
                        oldSegment.Content.ActiveEnemyCount;

                    if (missedEnemies > 0)
                    {
                        EnemiesMissed?.Invoke(
                            missedEnemies,
                            oldSegment.Content.SpawnDifficulty
                        );
                    }

                    oldSegment.Content.ClearEnemies(
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
            ] =
                newSegment;




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
                $"Start nuevo: " +
                $"{newSegment.Anchors.StartPoint.position}",
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
                .SetActive(true);


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
                    ? "Rotated Segment Prefab no est� asignado."
                    : "Normal Segment Prefab no est� asignado.",
                this
            );

            return null;
        }

        // usar la rotaci�n propia
        // del prefab.

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


        if (logLifecycle)
        {
            Debug.Log(
                $"Segmento f�sico creado: " +
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
                "[SegmentPool] No est� preparado.",
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
