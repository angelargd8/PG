using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class AlexWaypointMotion : MonoBehaviour
{
    [Header("Route: center, left, center, right")]
    [SerializeField] private Transform _center;
    [SerializeField] private Transform _left;
    [SerializeField] private Transform _right;
    [SerializeField] private Animator _animator;
    [Tooltip("Si esta vacio, se utiliza la camara del jugador o PlayerTargetProvider.")]
    [SerializeField] private Transform _player;

    [Header("Waypoint pauses")]
    [Tooltip("Segundos de espera al iniciar y al llegar a cada waypoint. Sigue mirando y lanzando al jugador.")]
    [Min(0f)] [SerializeField] private float _waitTimeAtWaypoint = 4f;

    [Header("Horizontal movement")]
    [Min(0.01f)] [SerializeField] private float _runLeftSpeed = 2f;
    [Min(0.01f)] [SerializeField] private float _walkRightSpeed = 1f;
    [Min(0.01f)] [SerializeField] private float _acceleration = 3f;
    [Min(0.01f)] [SerializeField] private float _deceleration = 4f;
    [Min(0.01f)] [SerializeField] private float _waypointDistance = 0.08f;
    [Tooltip("Velocidad maxima de giro hacia el jugador, en grados por segundo.")]
    [Min(1f)] [SerializeField] private float _turnSpeed = 180f;

    [Header("Stride calibration")]
    [Tooltip("Metros/segundo que corresponden al clip Run Left a velocidad 1x.")]
    [Min(0.01f)] [SerializeField] private float _runAnimationReferenceSpeed = 2f;
    [Tooltip("Metros/segundo que corresponden al clip Walk Right a velocidad 1x.")]
    [Min(0.01f)] [SerializeField] private float _walkAnimationReferenceSpeed = 1f;

    private static readonly int MoveDirection = Animator.StringToHash("MoveDirection");
    private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    private ExperienceBeatPlayer _beatPlayer;
    private Rigidbody _body;
    private Vector3 _velocity;
    private bool _running;
    private float _groundHeight;
    private float _originalAnimatorSpeed;
    private bool _originalRootMotion;
    private AnimatorCullingMode _originalCulling;
    private int _routeIndex;
    private int _lastLateralDirection = -1;
    private float _waitRemaining;

    public Vector3 HorizontalVelocity => _velocity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        if (_body == null) _body = gameObject.AddComponent<Rigidbody>();
        // The route owns horizontal translation; interpolation smooths rendering.
        _body.isKinematic = true;
        _body.useGravity = false;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        if (_running) return;
        ExperienceBeatPlayer beatPlayer = FindFirstObjectByType<ExperienceBeatPlayer>();
        if (beatPlayer != null) Begin(beatPlayer);
    }

    public bool Begin(ExperienceBeatPlayer beatPlayer)
    {
        if (_running) return true;
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_center == null || _left == null || _right == null || beatPlayer == null ||
            _animator == null || _animator.runtimeAnimatorController == null ||
            !HasParameter(MoveDirection, AnimatorControllerParameterType.Int) ||
            !HasParameter(MoveSpeed, AnimatorControllerParameterType.Float))
        {
            Debug.LogError("[AlexWaypointMotion] Asigna los tres waypoints y un Animator con MoveDirection (int) y MoveSpeed (float).", this);
            return false;
        }
        _beatPlayer = beatPlayer;
        _groundHeight = _body.position.y;
        ResolvePlayer();
        _originalAnimatorSpeed = _animator.speed;
        _originalRootMotion = _animator.applyRootMotion;
        _originalCulling = _animator.cullingMode;
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _running = true;
        InitializeRoute();
        return true;
    }

    public void End()
    {
        if (!_running) return;
        _running = false;
        _velocity = Vector3.zero;
        if (_animator != null)
        {
            _animator.SetInteger(MoveDirection, 0);
            _animator.SetFloat(MoveSpeed, 1f);
            _animator.speed = _originalAnimatorSpeed;
            _animator.applyRootMotion = _originalRootMotion;
            _animator.cullingMode = _originalCulling;
        }
        _beatPlayer = null;
    }

    private void OnDisable() => End();

    private void InitializeRoute()
    {
        _routeIndex = 0;
        _body.position = RoutePosition(0);
        _velocity = Vector3.zero;
        _lastLateralDirection = -1;
        _waitRemaining = Mathf.Max(0f, _waitTimeAtWaypoint);
        UpdateAnimation();
    }

    private void Update()
    {
        if (!_running || _beatPlayer == null) return;
        _animator.speed = _beatPlayer.IsPlaying ? _originalAnimatorSpeed : 0f;
        if (!_beatPlayer.IsPlaying) return;
        // The music clock controls playback, not route progress. Clock corrections,
        // seeks and missed beats must not teleport Alex or restart a waypoint wait.
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!_running || _beatPlayer == null || !_beatPlayer.IsPlaying) return;
        ResolvePlayer();
        if (_player == null)
        {
            _velocity = Vector3.zero;
            return;
        }

        float dt = Time.fixedDeltaTime;
        FacePlayer(dt);
        if (_waitRemaining > 0f)
        {
            _velocity = Vector3.zero;
            _waitRemaining = Mathf.Max(0f, _waitRemaining - dt);
            return;
        }
        int next = (_routeIndex + 1) % 4;
        Vector3 toWaypoint = RoutePosition(next) - _body.position;
        toWaypoint.y = 0f;
        float distance = toWaypoint.magnitude;
        float arrivalRadius = Mathf.Max(0.01f, _waypointDistance);
        if (distance <= arrivalRadius + 0.001f && _velocity.sqrMagnitude < 0.01f)
        {
            _routeIndex = next;
            _velocity = Vector3.zero;
            _waitRemaining = Mathf.Max(0f, _waitTimeAtWaypoint);
            return;
        }

        Vector3 direction = distance > 0.0001f ? toWaypoint / distance : Vector3.zero;
        float side = Vector3.Dot(direction, _body.rotation * Vector3.right);
        int desiredSide = Mathf.Abs(side) > 0.05f ? (side < 0f ? -1 : 1) : _lastLateralDirection;
        float maxSpeed = desiredSide < 0 ? _runLeftSpeed : _walkRightSpeed;
        float remaining = Mathf.Max(0f, distance - arrivalRadius);
        // v^2 = 2*a*d: brake near each waypoint rather than stopping at full speed.
        float brakingSpeed = Mathf.Sqrt(2f * Mathf.Max(0.01f, _deceleration) * remaining);
        Vector3 desiredVelocity = direction * Mathf.Min(Mathf.Max(0.01f, maxSpeed), brakingSpeed);
        bool slowing = desiredVelocity.sqrMagnitude < _velocity.sqrMagnitude ||
            Vector3.Dot(desiredVelocity, _velocity) < 0f;
        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity,
            Mathf.Max(0.01f, slowing ? _deceleration : _acceleration) * dt);

        Vector3 step = _velocity * dt;
        if (step.magnitude > remaining) step = step.normalized * remaining;
        _velocity = step / dt;
        _body.MovePosition(_body.position + step);
    }

    private void FacePlayer(float dt)
    {
        Vector3 direction = _player.position - _body.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        _body.MoveRotation(Quaternion.RotateTowards(_body.rotation,
            Quaternion.LookRotation(direction), Mathf.Max(1f, _turnSpeed) * dt));
    }

    private void ResolvePlayer()
    {
        if (_player != null) return;
        if (Camera.main != null) _player = Camera.main.transform;
        else if (PlayerTargetProvider.Instance != null) _player = PlayerTargetProvider.Instance.EnemyTarget;
    }

    private void UpdateAnimation()
    {
        float speed = _velocity.magnitude;
        int direction = 0;
        if (speed > 0.03f)
        {
            float side = Vector3.Dot(_velocity, _body.rotation * Vector3.right);
            if (Mathf.Abs(side) > 0.01f) _lastLateralDirection = side < 0f ? -1 : 1;
            direction = _lastLateralDirection;
        }
        float referenceSpeed = direction < 0 ? _runAnimationReferenceSpeed : _walkAnimationReferenceSpeed;
        _animator.SetInteger(MoveDirection, direction);
        _animator.SetFloat(MoveSpeed, direction == 0 ? 1f :
            Mathf.Clamp(speed / Mathf.Max(0.01f, referenceSpeed), 0.05f, 3f));
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
            if (parameter.nameHash == hash && parameter.type == type) return true;
        return false;
    }

    private Vector3 RoutePosition(int index)
    {
        Transform point = index == 1 ? _left : index == 3 ? _right : _center;
        Vector3 position = point.position;
        position.y = _groundHeight;
        return position;
    }
}
