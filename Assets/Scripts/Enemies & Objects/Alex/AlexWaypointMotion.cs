using System;
using UnityEngine;

// Move before animation evaluation. The controller owns all state transitions.
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class AlexWaypointMotion : MonoBehaviour
{
    [Header("Route: center, left, center, right")]
    [SerializeField] private Transform _center;
    [SerializeField] private Transform _left;
    [SerializeField] private Transform _right;
    [SerializeField] private Animator _animator;

    [Header("Movement")]
    [Min(0.01f)] [SerializeField] private float _runLeftSpeed = 2f;
    [Min(0.01f)] [SerializeField] private float _walkRightSpeed = 1f;
    [Tooltip("Tiempo de espera en cada waypoint, independiente de los lanzamientos.")]
    [Min(0f)] [SerializeField] private float _idleDuration = 1f;

    private static readonly int MoveDirection = Animator.StringToHash("MoveDirection");
    private ExperienceBeatPlayer _beatPlayer;
    private bool _running;
    private bool _moving;
    private bool _hasHeightOffset;
    private float _heightOffset;
    private float _originalAnimatorSpeed;
    private bool _originalRootMotion;
    private AnimatorCullingMode _originalCulling;
    private int _routeIndex;
    private double _lastSongTime;
    private double _departureTime = double.PositiveInfinity;

    public bool Begin(ExperienceBeatPlayer beatPlayer)
    {
        if (_running) return true;
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_center == null || _left == null || _right == null || beatPlayer == null ||
            _animator == null || _animator.runtimeAnimatorController == null || !HasMoveDirectionParameter())
        {
            Debug.LogError("[AlexWaypointMotion] Asigna los tres waypoints y un Animator con el parametro entero MoveDirection.", this);
            return false;
        }
        _beatPlayer = beatPlayer;
        if (!_hasHeightOffset)
        {
            _heightOffset = transform.position.y - _center.position.y;
            _hasHeightOffset = true;
        }
        _originalAnimatorSpeed = _animator.speed;
        _originalRootMotion = _animator.applyRootMotion;
        _originalCulling = _animator.cullingMode;
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _running = true;
        _beatPlayer.PlaybackReset += HandlePlaybackReset;
        ResetRoute();
        return true;
    }

    public void End()
    {
        if (!_running) return;
        _running = false;
        if (_beatPlayer != null) _beatPlayer.PlaybackReset -= HandlePlaybackReset;
        if (_animator != null)
        {
            _animator.SetInteger(MoveDirection, 0);
            _animator.speed = _originalAnimatorSpeed;
            _animator.applyRootMotion = _originalRootMotion;
            _animator.cullingMode = _originalCulling;
        }
        _beatPlayer = null;
    }

    private void OnDisable() => End();

    private void ResetRoute()
    {
        _routeIndex = 0;
        transform.position = RoutePosition(0);
        _lastSongTime = _beatPlayer.SongTime;
        EnterIdle();
    }

    private void HandlePlaybackReset()
    {
        double now = _beatPlayer.SongTime;
        // Pauses keep the current leg. Seeking the song starts the route at center.
        if (now < _lastSongTime || Math.Abs(now - _lastSongTime) > 0.5) ResetRoute();
    }

    private void Update()
    {
        if (!_running || _beatPlayer == null) return;
        _animator.speed = _beatPlayer.IsPlaying ? _originalAnimatorSpeed : 0f;
        if (!_beatPlayer.IsPlaying) return;
        double now = _beatPlayer.SongTime;
        double elapsed = now - _lastSongTime;
        if (elapsed < 0 || elapsed > 0.5)
        {
            ResetRoute();
            return;
        }
        _lastSongTime = now;

        if (!_moving)
        {
            if (now < _departureTime) return;
            _moving = true;
            bool leftward = _routeIndex == 0 || _routeIndex == 3;
            _animator.SetInteger(MoveDirection, leftward ? -1 : 1);
        }

        int next = (_routeIndex + 1) % 4;
        Vector3 destination = RoutePosition(next);
        float speed = _routeIndex == 0 || _routeIndex == 3 ? _runLeftSpeed : _walkRightSpeed;
        transform.position = Vector3.MoveTowards(transform.position, destination, Mathf.Max(0.01f, speed) * (float)elapsed);
        if ((transform.position - destination).sqrMagnitude <= 0.000001f)
        {
            transform.position = destination;
            _routeIndex = next;
            EnterIdle();
        }
    }

    private bool HasMoveDirectionParameter()
    {
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
            if (parameter.nameHash == MoveDirection && parameter.type == AnimatorControllerParameterType.Int)
                return true;
        return false;
    }

    private void EnterIdle()
    {
        _moving = false;
        _departureTime = _beatPlayer.SongTime + Mathf.Max(0f, _idleDuration);
        _animator.SetInteger(MoveDirection, 0);
    }

    private Vector3 RoutePosition(int index)
    {
        Transform point = index == 1 ? _left : index == 3 ? _right : _center;
        return point.position + Vector3.up * _heightOffset;
    }
}
