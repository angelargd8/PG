using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlexThrowDirector : MonoBehaviour, IExperienceRuntime
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private AlexThrowPool _pool;

    [SerializeField]
    private AlexThrowHands _hands;

    [SerializeField]
    private AlexWaypointMotion _movement;

    [SerializeField]
    private ExperienceBeatPlayer _beatPlayer;

    [Tooltip(
        "Opcional: centro de la zona de golpe. " +
        "Por defecto se usa la camara XR."
    )]
    [SerializeField]
    private Transform _hitTarget;

    [SerializeField]
    private Transform _returnTarget;

    [SerializeField]
    private InteractionResultEventChannelSO _interactionRegistered;

    [SerializeField]
    private ScoreProfileSO _scoreProfile;

    [SerializeField]
    private ScoreProfileEventChannelSO _scoreProfileChanged;


    // =========================================================
    // DIFFICULTY
    // =========================================================

    [Header("Difficulty")]

    [SerializeField]
    private DifficultyLevel _difficulty = DifficultyLevel.Normal;

    [Tooltip(
        "Cada cuantos beats aparece un par de objetos " +
        "en dificultad facil."
    )]
    [Min(1)]
    [SerializeField]
    private int _easyThrowEveryBeats = 4;

    [Tooltip(
        "Cada cuantos beats aparece un par de objetos " +
        "en dificultad normal."
    )]
    [Min(1)]
    [SerializeField]
    private int _normalThrowEveryBeats = 2;

    [Tooltip(
        "Cada cuantos beats aparece un par de objetos " +
        "en dificultad dificil."
    )]
    [Min(1)]
    [SerializeField]
    private int _hardThrowEveryBeats = 1;


    // =========================================================
    // RHYTHM
    // =========================================================

    [Header("Rhythm")]

    [Tooltip(
        "Beats que tarda el objeto en llegar " +
        "desde Alex al jugador."
    )]
    [Min(1)]
    [SerializeField]
    private int _travelBeats = 2;

    [Tooltip(
        "Beats que tarda una calabaza golpeada " +
        "en regresar hacia Alex."
    )]
    [Min(1)]
    [SerializeField]
    private int _returnBeats = 2;

    [Tooltip(
        "Ventana permitida para considerar " +
        "un golpe al ritmo."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float _beatWindow = 0.16f;

    [Min(0.01f)]
    [SerializeField]
    private float _missGrace = 0.3f;


    // =========================================================
    // INTENSITY
    // =========================================================

    [Header("Musical Intensity")]

    [Tooltip(
        "Un beat con intensidad igual o superior " +
        "activa el modo intenso."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float _intenseBeatThreshold = 0.7f;

    [Tooltip(
        "En modo intenso se divide el intervalo normal " +
        "entre este valor. 2 significa el doble de frecuencia."
    )]
    [Min(1)]
    [SerializeField]
    private int _intenseFrequencyMultiplier = 2;

    [Tooltip(
        "Altura adicional de los patrones " +
        "durante beats intensos."
    )]
    [Min(0f)]
    [SerializeField]
    private float _intenseVerticalBonus = 0.15f;

    [Tooltip(
        "Separacion lateral adicional durante " +
        "beats intensos."
    )]
    [Min(0f)]
    [SerializeField]
    private float _intenseLaneBonus = 0.08f;


    // =========================================================
    // TRAJECTORY
    // =========================================================

    [Header("Trajectory")]

    [Tooltip(
        "Distancia desde la camara hacia Alex " +
        "donde se encuentra la zona de golpe."
    )]
    [Min(0f)]
    [SerializeField]
    private float _hitForwardDistance = 0.65f;

    [Tooltip(
        "Offset vertical general respecto " +
        "a la camara."
    )]
    [SerializeField]
    private float _hitHeightOffset = -0.3f;

    [Tooltip(
        "Separacion lateral normal. " +
        "Recomiendo aproximadamente 0.55 - 0.65."
    )]
    [Min(0.1f)]
    [SerializeField]
    private float _wideLaneOffset = 0.6f;

    [Tooltip(
        "Separacion utilizada para patrones cerrados."
    )]
    [Min(0.1f)]
    [SerializeField]
    private float _narrowLaneOffset = 0.35f;

    [Tooltip(
        "Diferencia vertical utilizada en diagonales."
    )]
    [Min(0f)]
    [SerializeField]
    private float _verticalPatternOffset = 0.35f;

    [Tooltip(
        "Altura del arco visual del proyectil."
    )]
    [Min(0f)]
    [SerializeField]
    private float _arcHeight = 0.2f;


    // =========================================================
    // HAND VALIDATION
    // =========================================================

    [Header("Hand Validation")]

    [Tooltip(
        "Distancia minima lateral necesaria para decidir " +
        "si un objeto pertenece al carril izquierdo o derecho."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float _handLaneDeadZone = 0.08f;


    // =========================================================
    // RUNTIME
    // =========================================================

    private bool _running;

    private int _throwCount;
    private int _patternIndex;

    private int _pendingBeatIndex = -1;
    private bool _pendingIntenseBeat;


    // =========================================================
    // INTENSITY REFLECTION
    // =========================================================

    /*
     * Esto permite que el director encuentre una propiedad
     * como Intensity, Strength, Energy, Weight o Value
     * dentro de Beat sin acoplar este script a un nombre exacto.
     *
     * Cuando confirmes como se llama realmente en BeatMapSO.Beat,
     * puedes sustituir este sistema por acceso directo.
     */

    private static readonly string[] IntensityMemberNames =
    {
        "Intensity",
        "intensity",
        "Strength",
        "strength",
        "Energy",
        "energy",
        "Weight",
        "weight",
        "Value",
        "value"
    };

    private bool _intensityWarningShown;


    // =========================================================
    // PATTERNS
    // =========================================================

    private enum ThrowPattern
    {
        Wide = 0,
        Narrow = 1,

        LeftHighRightLow = 2,
        LeftLowRightHigh = 3,

        BothHigh = 4,
        BothLow = 5
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public bool IsPlaying =>
        _running &&
        isActiveAndEnabled &&
        _beatPlayer != null &&
        _beatPlayer.IsPlaying;


    public double SongTime =>
        _beatPlayer != null
            ? _beatPlayer.SongTime
            : 0.0;


    public float ArcHeight =>
        _arcHeight;


    public float MissGrace =>
        Mathf.Max(
            _beatWindow,
            _missGrace
        );


    public Vector3 ReturnPosition
    {
        get
        {
            if (_returnTarget != null)
            {
                return _returnTarget.position;
            }

            if (_hands != null &&
                _hands.LeftAnchor != null &&
                _hands.RightAnchor != null)
            {
                return
                    (_hands.LeftAnchor.position +
                     _hands.RightAnchor.position) *
                    0.5f;
            }

            return transform.position;
        }
    }


    // =========================================================
    // EXPERIENCE
    // =========================================================

    public void BeginExperience()
    {
        if (_running)
        {
            return;
        }


        if (_pool == null)
        {
            _pool =
                GetComponent<AlexThrowPool>();
        }


        if (_beatPlayer == null)
        {
            _beatPlayer =
                FindFirstObjectByType<ExperienceBeatPlayer>();
        }


        if (_pool == null ||
            _hands == null ||
            !_hands.ResolveAnchors() ||
            _beatPlayer == null ||
            _beatPlayer.BeatMap == null ||
            _interactionRegistered == null ||
            _scoreProfile == null ||
            _scoreProfileChanged == null)
        {
            Debug.LogError(
                "[AlexThrowDirector] Faltan pool, manos, " +
                "beat player o eventos de puntuacion.",
                this
            );

            return;
        }


        if (_movement == null)
        {
            _movement =
                _hands.GetComponent<AlexWaypointMotion>();
        }


        if (_movement != null &&
            !_movement.Begin(_beatPlayer))
        {
            return;
        }


        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );


        _throwCount = 0;
        _patternIndex = 0;

        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;

        _running = true;


        _beatPlayer.BeatReached += OnBeat;
        _beatPlayer.PlaybackReset += ResetPlayback;
    }


    public void EndExperience()
    {
        _running = false;

        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;


        if (_movement != null)
        {
            _movement.End();
        }


        if (_beatPlayer != null)
        {
            _beatPlayer.BeatReached -= OnBeat;
            _beatPlayer.PlaybackReset -= ResetPlayback;
        }


        if (_pool != null)
        {
            _pool.ReleaseAll();
        }
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void ResetPlayback()
    {
        /*
         * Pausas, reinicios y saltos del Timeline
         * nunca penalizan objetos activos.
         */

        if (_pool != null)
        {
            _pool.ReleaseAll();
        }

        _throwCount = 0;
        _patternIndex = 0;

        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;
    }


    // =========================================================
    // BEAT
    // =========================================================

    private void OnBeat(
        BeatMapSO.Beat beat,
        int index
    )
    {
        if (!IsPlaying)
        {
            return;
        }


        float intensity =
            GetBeatIntensity(beat);

        bool intense =
            intensity >= _intenseBeatThreshold;


        int throwEveryBeats =
            GetThrowEveryBeats(intense);


        if (index % throwEveryBeats != 0)
        {
            return;
        }


        _pendingBeatIndex = index;
        _pendingIntenseBeat = intense;
    }


    private int GetThrowEveryBeats(
        bool intense
    )
    {
        int interval;


        switch (_difficulty)
        {
            case DifficultyLevel.Easy:

                interval =
                    Mathf.Max(
                        1,
                        _easyThrowEveryBeats
                    );

                break;


            case DifficultyLevel.Hard:

                interval =
                    Mathf.Max(
                        1,
                        _hardThrowEveryBeats
                    );

                break;


            case DifficultyLevel.Normal:
            default:

                interval =
                    Mathf.Max(
                        1,
                        _normalThrowEveryBeats
                    );

                break;
        }


        if (!intense)
        {
            return interval;
        }


        /*
         * Ejemplos con multiplier = 2:
         *
         * Easy:
         * 4 -> 2
         *
         * Normal:
         * 2 -> 1
         *
         * Hard:
         * 1 -> 1
         */

        interval =
            Mathf.CeilToInt(
                interval /
                (float)Mathf.Max(
                    1,
                    _intenseFrequencyMultiplier
                )
            );


        return Mathf.Max(
            1,
            interval
        );
    }


    // =========================================================
    // THROW
    // =========================================================

    private void LateUpdate()
    {
        /*
         * El lanzamiento ocurre en LateUpdate para leer
         * las posiciones finales de las manos despues
         * de que Animator haya actualizado la pose.
         */

        int index =
            _pendingBeatIndex;

        bool intense =
            _pendingIntenseBeat;


        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;


        if (!IsPlaying ||
            index < 0)
        {
            return;
        }


        var beats =
            _beatPlayer.BeatMap.Beats;


        int arrivalIndex =
            index +
            Mathf.Max(
                1,
                _travelBeats
            );


        if (arrivalIndex >= beats.Count)
        {
            return;
        }


        double arrival =
            beats[arrivalIndex].Time;


        if (arrival <= SongTime)
        {
            return;
        }


        Transform target =
            ResolveHitTarget();


        if (target == null)
        {
            return;
        }


        CalculateTargetBasis(
            target,
            out Vector3 center,
            out Vector3 forward,
            out Vector3 right
        );


        ThrowPattern pattern =
            GetNextPattern(
                intense
            );


        CalculatePatternPositions(
            pattern,
            intense,
            center,
            right,
            out Vector3 leftPosition,
            out Vector3 rightPosition
        );


        // =====================================================
        // OBJECT TYPES
        // =====================================================

        /*
         * Se consumen dos tipos por cada lanzamiento.
         *
         * Ciclo:
         *
         * Pair 1:
         * Left  -> Dollar
         * Right -> Pumpkin A
         *
         * Pair 2:
         * Left  -> Pumpkin F
         * Right -> Pumpkin G
         *
         * Pair 3:
         * Left  -> Dollar
         * Right -> Pumpkin A
         */

        AlexThrowKind leftKind =
            (AlexThrowKind)(
                _throwCount % 4
            );


        AlexThrowKind rightKind =
            (AlexThrowKind)(
                (_throwCount + 1) % 4
            );


        // =====================================================
        // LEFT
        // =====================================================

        bool launchedLeft =
            _pool.Launch(
                leftKind,
                this,
                _hands.LeftAnchor.position,
                _hands.LeftAnchor.rotation,
                leftPosition,
                SongTime,
                arrival
            );


        // =====================================================
        // RIGHT
        // =====================================================

        bool launchedRight =
            _pool.Launch(
                rightKind,
                this,
                _hands.RightAnchor.position,
                _hands.RightAnchor.rotation,
                rightPosition,
                SongTime,
                arrival
            );


        if (launchedLeft ||
            launchedRight)
        {
            _throwCount += 2;
            _patternIndex++;
        }
    }


    // =========================================================
    // PATTERN SELECTION
    // =========================================================

    private ThrowPattern GetNextPattern(
        bool intense
    )
    {
        /*
         * En vez de Random usamos secuencias.
         *
         * En un juego musical normalmente se siente
         * mejor porque los patrones parecen diseñados
         * y no ruido aleatorio.
         */


        if (intense)
        {
            /*
             * Seccion energetica:
             *
             * diagonal
             * diagonal inversa
             * arriba
             * abierto
             * abajo
             * cerrado
             */

            switch (_patternIndex % 6)
            {
                case 0:
                    return ThrowPattern.LeftHighRightLow;

                case 1:
                    return ThrowPattern.LeftLowRightHigh;

                case 2:
                    return ThrowPattern.BothHigh;

                case 3:
                    return ThrowPattern.Wide;

                case 4:
                    return ThrowPattern.BothLow;

                default:
                    return ThrowPattern.Narrow;
            }
        }


        switch (_difficulty)
        {
            // =================================================
            // EASY
            // =================================================

            case DifficultyLevel.Easy:

                switch (_patternIndex % 4)
                {
                    case 0:
                        return ThrowPattern.Wide;

                    case 1:
                        return ThrowPattern.LeftHighRightLow;

                    case 2:
                        return ThrowPattern.Wide;

                    default:
                        return ThrowPattern.LeftLowRightHigh;
                }


            // =================================================
            // HARD
            // =================================================

            case DifficultyLevel.Hard:

                switch (_patternIndex % 6)
                {
                    case 0:
                        return ThrowPattern.LeftHighRightLow;

                    case 1:
                        return ThrowPattern.LeftLowRightHigh;

                    case 2:
                        return ThrowPattern.Narrow;

                    case 3:
                        return ThrowPattern.BothHigh;

                    case 4:
                        return ThrowPattern.Wide;

                    default:
                        return ThrowPattern.BothLow;
                }


            // =================================================
            // NORMAL
            // =================================================

            case DifficultyLevel.Normal:
            default:

                switch (_patternIndex % 5)
                {
                    case 0:
                        return ThrowPattern.Wide;

                    case 1:
                        return ThrowPattern.LeftHighRightLow;

                    case 2:
                        return ThrowPattern.Narrow;

                    case 3:
                        return ThrowPattern.LeftLowRightHigh;

                    default:
                        return ThrowPattern.Wide;
                }
        }
    }


    // =========================================================
    // PATTERN POSITION
    // =========================================================

    private void CalculatePatternPositions(
        ThrowPattern pattern,
        bool intense,
        Vector3 center,
        Vector3 right,
        out Vector3 leftPosition,
        out Vector3 rightPosition
    )
    {
        float wide =
            _wideLaneOffset;


        float narrow =
            _narrowLaneOffset;


        float vertical =
            _verticalPatternOffset;


        if (intense)
        {
            wide +=
                _intenseLaneBonus;

            vertical +=
                _intenseVerticalBonus;
        }


        switch (pattern)
        {
            // =================================================
            // WIDE
            // =================================================

            case ThrowPattern.Wide:

                leftPosition =
                    center -
                    right * wide;

                rightPosition =
                    center +
                    right * wide;

                break;


            // =================================================
            // NARROW
            // =================================================

            case ThrowPattern.Narrow:

                leftPosition =
                    center -
                    right * narrow;

                rightPosition =
                    center +
                    right * narrow;

                break;


            // =================================================
            // LEFT HIGH / RIGHT LOW
            // =================================================

            case ThrowPattern.LeftHighRightLow:

                leftPosition =
                    center -
                    right * wide +
                    Vector3.up * vertical;

                rightPosition =
                    center +
                    right * wide -
                    Vector3.up * vertical;

                break;


            // =================================================
            // LEFT LOW / RIGHT HIGH
            // =================================================

            case ThrowPattern.LeftLowRightHigh:

                leftPosition =
                    center -
                    right * wide -
                    Vector3.up * vertical;

                rightPosition =
                    center +
                    right * wide +
                    Vector3.up * vertical;

                break;


            // =================================================
            // BOTH HIGH
            // =================================================

            case ThrowPattern.BothHigh:

                leftPosition =
                    center -
                    right * wide +
                    Vector3.up * vertical;

                rightPosition =
                    center +
                    right * wide +
                    Vector3.up * vertical;

                break;


            // =================================================
            // BOTH LOW
            // =================================================

            case ThrowPattern.BothLow:

                leftPosition =
                    center -
                    right * wide -
                    Vector3.up * vertical;

                rightPosition =
                    center +
                    right * wide -
                    Vector3.up * vertical;

                break;


            default:

                leftPosition =
                    center -
                    right * wide;

                rightPosition =
                    center +
                    right * wide;

                break;
        }
    }


    // =========================================================
    // TARGET
    // =========================================================

    private Transform ResolveHitTarget()
    {
        if (_hitTarget != null)
        {
            return _hitTarget;
        }


        if (Camera.main != null)
        {
            return Camera.main.transform;
        }


        return null;
    }


    private void CalculateTargetBasis(
        Transform target,
        out Vector3 center,
        out Vector3 forward,
        out Vector3 right
    )
    {
        /*
         * Direccion horizontal desde el jugador
         * hacia Alex.
         */

        forward =
            Vector3.ProjectOnPlane(
                _hands.transform.position -
                target.position,
                Vector3.up
            );


        if (forward.sqrMagnitude < 0.01f)
        {
            forward =
                Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }


        right =
            Vector3.Cross(
                Vector3.up,
                forward
            ).normalized;


        center =
            target.position;


        /*
         * Si existe Hit Target explicito,
         * su posicion ya representa el centro.
         *
         * Si usamos la camara, desplazamos
         * la zona delante del jugador.
         */

        if (_hitTarget == null)
        {
            center +=
                forward *
                _hitForwardDistance;

            center +=
                Vector3.up *
                _hitHeightOffset;
        }
    }


    // =========================================================
    // HAND VALIDATION
    // =========================================================

    private bool IsCorrectHand(
        AlexThrownObject item,
        AlexWarhammer hammer
    )
    {
        if (item == null ||
            hammer == null)
        {
            return false;
        }


        Transform target =
            ResolveHitTarget();


        if (target == null)
        {
            /*
             * Si no podemos calcular el carril,
             * evitamos invalidar accidentalmente
             * una interaccion.
             */

            return true;
        }


        CalculateTargetBasis(
            target,
            out Vector3 center,
            out Vector3 forward,
            out Vector3 right
        );


        /*
         * Usamos la posicion actual del objeto
         * respecto al centro de la zona.
         *
         * Negativo = carril izquierdo
         * Positivo = carril derecho
         */

        float side =
            Vector3.Dot(
                item.transform.position -
                center,
                right
            );


        /*
         * Ninguno de los patrones actuales
         * deberia pasar por esta zona.
         *
         * Es una seguridad por si luego reduces
         * mucho Narrow Lane Offset.
         */

        if (Mathf.Abs(side) <
            _handLaneDeadZone)
        {
            return true;
        }


        SceneWeaponEquipController.Hand requiredHand =
            side < 0f
                ? SceneWeaponEquipController.Hand.Left
                : SceneWeaponEquipController.Hand.Right;


        return
            hammer.Hand ==
            requiredHand;
    }


    // =========================================================
    // CONTACT
    // =========================================================

    public bool RegisterContact(
        AlexThrownObject item,
        AlexWarhammer hammer,
        bool strike,
        Vector3 position
    )
    {
        double now =
            SongTime;


        // =====================================================
        // HAND
        // =====================================================

        bool correctHand =
            IsCorrectHand(
                item,
                hammer
            );


        // =====================================================
        // ORIGINAL RULES
        // =====================================================

        bool rulesSuccess =
            AlexThrowRules.IsSuccess(
                item.Kind,
                strike,
                now -
                item.ExpectedHitTime,
                _beatWindow
            );


        // =====================================================
        // FINAL RESULT
        // =====================================================

        bool success =
            correctHand &&
            rulesSuccess;


        /*
         * Siempre vibra el control que realmente
         * hizo contacto, incluso si fue incorrecto.
         */

        hammer.Pulse(
            strike
        );


        _interactionRegistered.RaiseEvent(
            new InteractionResult(
                minigameId: "Alex",

                interactionType:
                    strike
                        ? InteractionType.HammerHit
                        : InteractionType.HammerTouch,

                outcome:
                    success
                        ? InteractionOutcome.Success
                        : InteractionOutcome.Failed,

                difficulty:
                    _difficulty,

                expectedTime:
                    item.ExpectedHitTime,

                /*
                 * Dollar y touches no reciben
                 * bonus de timing.
                 */

                actualTime:
                    item.Kind ==
                    AlexThrowKind.Dollar ||
                    !strike
                        ? (double?)null
                        : now,

                feedbackPosition:
                    position
            )
        );


        return success;
    }


    // =========================================================
    // MISS
    // =========================================================

    public void RegisterMiss(
        AlexThrownObject item
    )
    {
        _interactionRegistered.RaiseEvent(
            new InteractionResult(
                "Alex",

                InteractionType.HammerTouch,

                InteractionOutcome.Missed,

                _difficulty,

                item.ExpectedHitTime,

                feedbackPosition:
                    item.transform.position
            )
        );
    }


    // =========================================================
    // RETURN
    // =========================================================

    public bool TryGetReturnTime(
        out double arrival
    )
    {
        var beats =
            _beatPlayer.BeatMap.Beats;


        double now =
            SongTime;


        int low = 0;
        int high =
            beats.Count;


        while (low < high)
        {
            int middle =
                low +
                (high - low) / 2;


            if (beats[middle].Time <= now)
            {
                low =
                    middle + 1;
            }
            else
            {
                high =
                    middle;
            }
        }


        int index =
            low +
            Mathf.Max(
                1,
                _returnBeats
            ) -
            1;


        arrival =
            index < beats.Count
                ? beats[index].Time
                : 0.0;


        return
            index < beats.Count;
    }


    // =========================================================
    // BEAT INTENSITY
    // =========================================================

    private float GetBeatIntensity(
        BeatMapSO.Beat beat
    )
    {
        /*
         * Tu codigo compartido no muestra BeatMapSO.Beat,
         * asi que no puedo asumir que el campo realmente
         * se llama Intensity.
         *
         * Buscamos nombres comunes sin producir
         * dependencia de compilacion.
         *
         * Esto se ejecuta unas pocas veces por segundo,
         * no por frame.
         */

        object boxedBeat =
            beat;


        Type type =
            boxedBeat.GetType();


        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;


        foreach (string memberName
                 in IntensityMemberNames)
        {
            // =================================================
            // PROPERTY
            // =================================================

            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    flags
                );


            if (property != null &&
                property.CanRead)
            {
                object value =
                    property.GetValue(
                        boxedBeat
                    );


                if (TryConvertIntensity(
                        value,
                        out float result
                    ))
                {
                    return result;
                }
            }


            // =================================================
            // FIELD
            // =================================================

            FieldInfo field =
                type.GetField(
                    memberName,
                    flags
                );


            if (field != null)
            {
                object value =
                    field.GetValue(
                        boxedBeat
                    );


                if (TryConvertIntensity(
                        value,
                        out float result
                    ))
                {
                    return result;
                }
            }
        }


        if (!_intensityWarningShown)
        {
            _intensityWarningShown = true;

            Debug.LogWarning(
                "[AlexThrowDirector] No encontre Intensity, " +
                "Strength, Energy, Weight o Value dentro de " +
                "BeatMapSO.Beat. El modo intenso no se activara " +
                "hasta indicar el nombre real del campo.",
                this
            );
        }


        return 0f;
    }


    private static bool TryConvertIntensity(
        object value,
        out float intensity
    )
    {
        intensity = 0f;


        if (value == null)
        {
            return false;
        }


        try
        {
            intensity =
                Convert.ToSingle(
                    value
                );


            return true;
        }
        catch
        {
            return false;
        }
    }
}