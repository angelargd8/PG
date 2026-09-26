using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlexThrowDirector : MonoBehaviour, IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private AlexThrowPool _pool;
    [SerializeField] private AlexThrowHands _hands;
    [SerializeField] private AlexWaypointMotion _movement;
    [SerializeField] private ExperienceBeatPlayer _beatPlayer;
    [Tooltip("Opcional: centro de la zona de golpe. Por defecto se usa la camara XR.")]
    [SerializeField] private Transform _hitTarget;
    [SerializeField] private Transform _returnTarget;
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;

    [Header("Rhythm")]
    [Min(1)] [SerializeField] private int _throwEveryBeats = 2;
    [Min(1)] [SerializeField] private int _travelBeats = 2;
    [Min(1)] [SerializeField] private int _returnBeats = 2;
    [Min(0.01f)] [SerializeField] private float _beatWindow = 0.16f;
    [Min(0.01f)] [SerializeField] private float _missGrace = 0.3f;
    [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.Normal;

    [Header("Trajectory")]
    [Tooltip("Metros desde la camara hacia el enemigo para la zona de contacto.")]
    [Min(0f)] [SerializeField] private float _hitForwardDistance = 0.65f;
    [SerializeField] private float _hitHeightOffset = -0.3f;
    [Min(0f)] [SerializeField] private float _laneOffset = 0.25f;
    [Min(0f)] [SerializeField] private float _arcHeight = 0.2f;

    private bool _running;
    private bool _leftNext = true;
    private int _throwCount;
    private int _pendingBeatIndex = -1;
    public bool IsPlaying => _running && isActiveAndEnabled && _beatPlayer != null && _beatPlayer.IsPlaying;
    public double SongTime => _beatPlayer != null ? _beatPlayer.SongTime : 0.0;
    public float ArcHeight => _arcHeight;
    public float MissGrace => Mathf.Max(_beatWindow, _missGrace);
    public Vector3 ReturnPosition => _returnTarget != null ? _returnTarget.position :
        (_hands.LeftAnchor.position + _hands.RightAnchor.position) * 0.5f;

    public void BeginExperience()
    {
        if (_running) return;
        if (_pool == null) _pool = GetComponent<AlexThrowPool>();
        if (_beatPlayer == null) _beatPlayer = FindFirstObjectByType<ExperienceBeatPlayer>();
        if (_pool == null || _hands == null || !_hands.ResolveAnchors() ||
            _beatPlayer == null || _beatPlayer.BeatMap == null ||
            _interactionRegistered == null || _scoreProfile == null || _scoreProfileChanged == null)
        {
            Debug.LogError("[AlexThrowDirector] Faltan pool, manos, beat player o eventos de puntuacion.", this);
            return;
        }
        if (_movement == null) _movement = _hands.GetComponent<AlexWaypointMotion>();
        if (_movement != null && !_movement.Begin(_beatPlayer)) return;
        _scoreProfileChanged.RaiseEvent(_scoreProfile);
        _leftNext = true;
        _throwCount = 0;
        _running = true;
        _beatPlayer.BeatReached += OnBeat;
        _beatPlayer.PlaybackReset += ResetPlayback;
    }

    public void EndExperience()
    {
        _running = false;
        _pendingBeatIndex = -1;
        if (_movement != null) _movement.End();
        if (_beatPlayer != null)
        {
            _beatPlayer.BeatReached -= OnBeat;
            _beatPlayer.PlaybackReset -= ResetPlayback;
        }
        if (_pool != null) _pool.ReleaseAll();
    }

    private void OnDisable() => EndExperience();

    private void ResetPlayback()
    {
        _pool.ReleaseAll(); // Pauses and timeline seeks never count as misses.
        _leftNext = true;
        _throwCount = 0;
        _pendingBeatIndex = -1;
    }

    private void OnBeat(BeatMapSO.Beat beat, int index)
    {
        if (!IsPlaying || index % Mathf.Max(1, _throwEveryBeats) != 0) return;
        _pendingBeatIndex = index;
    }

    private void LateUpdate()
    {
        // Sample the hands after the Animator has evaluated this frame's pose,
        // including running, walking and transitions. Never gate throws on a state.
        int index = _pendingBeatIndex;
        _pendingBeatIndex = -1;
        if (!IsPlaying || index < 0) return;
        var beats = _beatPlayer.BeatMap.Beats;
        int arrivalIndex = index + Mathf.Max(1, _travelBeats);
        if (arrivalIndex >= beats.Count) return;
        double arrival = beats[arrivalIndex].Time;
        if (arrival <= SongTime) return;

        Transform target = _hitTarget;
        if (target == null && Camera.main != null) target = Camera.main.transform;
        if (target == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(_hands.transform.position - target.position, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 position = target.position + right * (_leftNext ? -_laneOffset : _laneOffset);
        if (_hitTarget == null) position += forward * _hitForwardDistance + Vector3.up * _hitHeightOffset;
        Transform hand = _leftNext ? _hands.LeftAnchor : _hands.RightAnchor;
        // All four variants are exercised; alternate the starting hand each cycle.
        AlexThrowKind kind = (AlexThrowKind)(_throwCount % 4);
        if (_pool.Launch(kind, this, hand.position, hand.rotation, position, SongTime, arrival))
        {
            _throwCount++;
            _leftNext = _throwCount % 4 == 0 ? _leftNext : !_leftNext;
        }
    }

    public bool RegisterContact(AlexThrownObject item, AlexWarhammer hammer, bool strike, Vector3 position)
    {
        double now = SongTime;
        bool success = AlexThrowRules.IsSuccess(item.Kind, strike, now - item.ExpectedHitTime, _beatWindow);
        hammer.Pulse(strike);
        _interactionRegistered.RaiseEvent(new InteractionResult(
            minigameId: "Alex",
            interactionType: strike ? InteractionType.HammerHit : InteractionType.HammerTouch,
            outcome: success ? InteractionOutcome.Success : InteractionOutcome.Failed,
            difficulty: _difficulty,
            expectedTime: item.ExpectedHitTime,
            // Touches and Dollars have no rhythm requirement or timing bonus.
            actualTime: item.Kind == AlexThrowKind.Dollar || !strike ? (double?)null : now,
            feedbackPosition: position));
        return success;
    }

    public void RegisterMiss(AlexThrownObject item)
    {
        _interactionRegistered.RaiseEvent(new InteractionResult("Alex", InteractionType.HammerTouch,
            InteractionOutcome.Missed, _difficulty, item.ExpectedHitTime,
            feedbackPosition: item.transform.position));
    }

    public bool TryGetReturnTime(out double arrival)
    {
        var beats = _beatPlayer.BeatMap.Beats;
        double now = SongTime;
        int low = 0;
        int high = beats.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (beats[middle].Time <= now) low = middle + 1;
            else high = middle;
        }
        int index = low + Mathf.Max(1, _returnBeats) - 1;
        arrival = index < beats.Count ? beats[index].Time : 0.0;
        return index < beats.Count;
    }
}
