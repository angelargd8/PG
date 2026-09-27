using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlexThrowDirector : MonoBehaviour, IExperienceRuntime
{

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


    [Header("Difficulty")]

    [SerializeField]
    private DifficultyLevel _difficulty = DifficultyLevel.Normal;

    [Tooltip("Canal de DynamicDifficultySystem en ExperienceCore. PlayerStateSystem determina Overloaded/Engaged.")]
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    [Header("Miss Penalty")]
    [Tooltip("Puntos que se restan por cada calabaza que pasa sin contacto. Dollar no penaliza por omision.")]
    [Min(1)] [SerializeField] private int _missedPumpkinPenalty = 75;

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

    [Header("Difficulty Spacing")]

    [Tooltip(
        "Multiplicador horizontal para Easy. " +
        "0.55 significa 55% de la separacion de Hard."
    )]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float _easyHorizontalMultiplier = 0.55f;

    [Tooltip(
        "Multiplicador horizontal para Normal. " +
        "0.80 significa 80% de la separacion de Hard."
    )]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float _normalHorizontalMultiplier = 0.8f;

    [Tooltip(
        "Hard conserva la separacion original."
    )]
    [Range(0.1f, 1.5f)]
    [SerializeField]
    private float _hardHorizontalMultiplier = 1f;

    [Header("Low Target Height")]
    [Tooltip("Descenso maximo en metros bajo el centro de impacto en Facil, incluso en beats intensos.")]
    [Min(0f)] [SerializeField] private float _easyMaxDrop = 0.1f;

    [Tooltip("Descenso maximo en metros bajo el centro de impacto en Normal, incluso en beats intensos.")]
    [Min(0f)] [SerializeField] private float _normalMaxDrop = 0.2f;


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
    // MUSICAL INTENSITY
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
        "entre este valor."
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
        "Separacion del patron abierto en HARD. " +
        "Normal y Easy se reducen automaticamente."
    )]
    [Min(0.1f)]
    [SerializeField]
    private float _wideLaneOffset = 0.6f;

    [Tooltip(
        "Separacion del patron cerrado en HARD. " +
        "Normal y Easy se reducen automaticamente."
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


    [Header("Hand Validation")]

    [Tooltip(
        "Zona central donde no se intenta decidir " +
        "automaticamente izquierda o derecha."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float _handLaneDeadZone = 0.08f;
    private bool _running;

    private ScoreProfileSO _runtimeScoreProfile;
    private int _appliedMissPenalty;

    private int _throwCount;
    private int _patternIndex;

    private int _pendingBeatIndex = -1;
    private bool _pendingIntenseBeat;



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


    private enum ThrowPattern
    {
        Wide = 0,
        Narrow = 1,
        LeftHighRightLow = 2,
        LeftLowRightHigh = 3,
        BothHigh = 4,
        BothLow = 5
    }

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
            _scoreProfileChanged == null ||
            _difficultyChanged == null)
        {
            Debug.LogError(
                "[AlexThrowDirector] Faltan pool, manos, " +
                "beat player o eventos de puntuacion/dificultad.",
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

        _runtimeScoreProfile = Instantiate(_scoreProfile);
        _runtimeScoreProfile.name = _scoreProfile.name + " (Alex Runtime)";
        _appliedMissPenalty = 0;
        UpdateMissPenalty();
        _scoreProfileChanged.RaiseEvent(_runtimeScoreProfile);


        _throwCount = 0;
        _patternIndex = 0;

        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;

        _running = true;

        _difficultyChanged.Raised += HandleDifficultyChanged;
        HandleDifficultyChanged(_difficultyChanged.CurrentDifficulty);


        _beatPlayer.BeatReached += OnBeat;
        _beatPlayer.PlaybackReset += ResetPlayback;
    }


    public void EndExperience()
    {
        _running = false;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -= HandleDifficultyChanged;
        }

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

        if (_runtimeScoreProfile != null)
        {
            if (_scoreProfileChanged != null)
                _scoreProfileChanged.RaiseEvent(_scoreProfile);
            Destroy(_runtimeScoreProfile);
            _runtimeScoreProfile = null;
        }
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void ResetPlayback()
    {
        if (_pool != null)
        {
            _pool.ReleaseAll();
        }

        _throwCount = 0;
        _patternIndex = 0;

        _pendingBeatIndex = -1;
        _pendingIntenseBeat = false;
    }


    private void Update()
    {
        if (!_running) return;
        UpdateMissPenalty();
    }

    private void UpdateMissPenalty()
    {
        int penalty = Mathf.Max(1, _missedPumpkinPenalty);
        if (_runtimeScoreProfile == null || penalty == _appliedMissPenalty) return;

        JsonUtility.FromJsonOverwrite("{\"_missedPoints\":" + (-penalty) + "}", _runtimeScoreProfile);
        _appliedMissPenalty = penalty;
    }

    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        if (!_running) return;
        _difficulty = difficulty;
    }

    private int GetBaseThrowEveryBeats()
    {
        switch (_difficulty)
        {
            case DifficultyLevel.Easy:

                return Mathf.Max(
                    1,
                    _easyThrowEveryBeats
                );


            case DifficultyLevel.Hard:

                return Mathf.Max(
                    1,
                    _hardThrowEveryBeats
                );


            case DifficultyLevel.Normal:
            default:

                return Mathf.Max(
                    1,
                    _normalThrowEveryBeats
                );
        }
    }


    private float GetHorizontalDifficultyMultiplier()
    {
        switch (_difficulty)
        {
            case DifficultyLevel.Easy:

                return Mathf.Max(
                    0.1f,
                    _easyHorizontalMultiplier
                );


            case DifficultyLevel.Hard:

                return Mathf.Max(
                    0.1f,
                    _hardHorizontalMultiplier
                );


            case DifficultyLevel.Normal:
            default:

                return Mathf.Max(
                    0.1f,
                    _normalHorizontalMultiplier
                );
        }
    }


    private int GetThrowEveryBeats(
        bool intense
    )
    {
        int interval =
            GetBaseThrowEveryBeats();


        if (!intense)
        {
            return interval;
        }


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
            intensity >=
            _intenseBeatThreshold;


        int throwEveryBeats =
            GetThrowEveryBeats(
                intense
            );


        if (index % throwEveryBeats != 0)
        {
            return;
        }


        _pendingBeatIndex =
            index;

        _pendingIntenseBeat =
            intense;
    }

    private void LateUpdate()
    {
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
        // VARIANTS
        // =====================================================

        AlexThrowKind leftKind =
            (AlexThrowKind)(
                _throwCount % 4
            );


        AlexThrowKind rightKind =
            (AlexThrowKind)(
                (_throwCount + 1) % 4
            );


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


    private ThrowPattern GetNextPattern(
        bool intense
    )
    {
        if (intense)
        {
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

    private void CalculatePatternPositions(
        ThrowPattern pattern,
        bool intense,
        Vector3 center,
        Vector3 right,
        out Vector3 leftPosition,
        out Vector3 rightPosition
    )
    {

        float difficultyMultiplier =
            GetHorizontalDifficultyMultiplier();


        float wide =
            _wideLaneOffset *
            difficultyMultiplier;


        float narrow =
            _narrowLaneOffset *
            difficultyMultiplier;


        float vertical =
            _verticalPatternOffset;


        if (intense)
        {

            float horizontalBonus =
                _intenseLaneBonus *
                difficultyMultiplier;


            wide +=
                horizontalBonus;


            narrow +=
                horizontalBonus;


            vertical +=
                _intenseVerticalBonus;
        }


        // Limit only the low side of a pattern, including the intense-beat bonus.
        float downwardOffset = _difficulty switch
        {
            DifficultyLevel.Easy => Mathf.Min(vertical, Mathf.Max(0f, _easyMaxDrop)),
            DifficultyLevel.Normal => Mathf.Min(vertical, Mathf.Max(0f, _normalMaxDrop)),
            _ => vertical
        };

        switch (pattern)
        {

            case ThrowPattern.Wide:

                leftPosition =
                    center -
                    right * wide;

                rightPosition =
                    center +
                    right * wide;

                break;



            case ThrowPattern.Narrow:

                leftPosition =
                    center -
                    right * narrow;

                rightPosition =
                    center +
                    right * narrow;

                break;

            case ThrowPattern.LeftHighRightLow:

                leftPosition =
                    center -
                    right * wide +
                    Vector3.up * vertical;

                rightPosition =
                    center +
                    right * wide -
                    Vector3.up * downwardOffset;

                break;


            case ThrowPattern.LeftLowRightHigh:

                leftPosition =
                    center -
                    right * wide -
                    Vector3.up * downwardOffset;

                rightPosition =
                    center +
                    right * wide +
                    Vector3.up * vertical;

                break;


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


            case ThrowPattern.BothLow:

                leftPosition =
                    center -
                    right * wide -
                    Vector3.up * downwardOffset;

                rightPosition =
                    center +
                    right * wide -
                    Vector3.up * downwardOffset;

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
            return true;
        }


        CalculateTargetBasis(
            target,
            out Vector3 center,
            out Vector3 forward,
            out Vector3 right
        );


        float side =
            Vector3.Dot(
                item.transform.position -
                center,
                right
            );


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

    public bool RegisterContact(
        AlexThrownObject item,
        AlexWarhammer hammer,
        bool strike,
        Vector3 position
    )
    {
        double now =
            SongTime;


        bool correctHand =
            IsCorrectHand(
                item,
                hammer
            );


        bool isDollar =
            item.Kind ==
            AlexThrowKind.Dollar;


        bool rulesSuccess;


        if (isDollar)
        {
            rulesSuccess = true;
        }
        else
        {
            rulesSuccess =
                AlexThrowRules.IsSuccess(
                    item.Kind,
                    strike,
                    now -
                    item.ExpectedHitTime,
                    _beatWindow
                );
        }


        bool success =
            correctHand &&
            rulesSuccess;


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


                actualTime:
                    isDollar ||
                    !strike
                        ? (double?)null
                        : now,

                feedbackPosition:
                    position
            )
        );


        return success;
    }


    public void RegisterMiss(
        AlexThrownObject item
    )
    {
        if (!IsPlaying || item == null || item.Kind == AlexThrowKind.Dollar) return;
        UpdateMissPenalty();
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


    private float GetBeatIntensity(
        BeatMapSO.Beat beat
    )
    {
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
