using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlexThrownObject : MonoBehaviour
{
    private AlexThrowPool _pool;
    private AlexThrowDirector _director;
    private Rigidbody _body;
    private Collider[] _colliders;
    private Vector3 _origin;
    private Vector3 _target;
    private double _startTime;
    private double _arrivalTime;
    private bool _resolved;
    private bool _returning;
    private float _arcHeight;

    public AlexThrowKind Kind { get; private set; }
    public double ExpectedHitTime { get; private set; }

    public void ConfigurePhysics()
    {
        _body = GetComponent<Rigidbody>();
        if (_body == null) _body = gameObject.AddComponent<Rigidbody>();
        _body.isKinematic = true;
        _body.useGravity = false;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider hitbox in _colliders) hitbox.isTrigger = true;
        if (_colliders.Length == 0)
            Debug.LogError("[AlexThrownObject] El prefab necesita un collider.", this);
    }

    public void Launch(AlexThrowPool pool, AlexThrowKind kind, AlexThrowDirector director,
        Vector3 origin, Quaternion rotation, Vector3 target, double launchTime, double hitTime)
    {
        _pool = pool;
        Kind = kind;
        _director = director;
        _origin = origin;
        _target = target;
        _startTime = launchTime;
        _arrivalTime = ExpectedHitTime = hitTime;
        _arcHeight = director.ArcHeight;
        _resolved = _returning = false;
        foreach (Collider hitbox in _colliders) hitbox.enabled = true;
        transform.SetPositionAndRotation(origin, rotation);
        _body.position = origin;
        _body.rotation = rotation;
    }

    private void FixedUpdate()
    {
        if (_director == null || !_director.IsPlaying) return;
        double now = _director.SongTime;
        // Alex can change waypoints while a reflected pumpkin is in flight.
        if (_returning) _target = _director.ReturnPosition;
        float t = (float)((now - _startTime) / System.Math.Max(0.001, _arrivalTime - _startTime));
        // Continue past the hit plane during the late window; do not home in on the head.
        Vector3 position = Vector3.LerpUnclamped(_origin, _target, t);
        if (t < 1f) position += Vector3.up * (4f * _arcHeight * t * (1f - t));
        _body.MovePosition(position);

        if (_returning && now >= _arrivalTime)
            _pool.Release(this);
        else if (!_resolved && now > ExpectedHitTime + _director.MissGrace)
        {
            _resolved = true;
            _director.RegisterMiss(this);
            _pool.Release(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AlexWarhammer hammer = other.GetComponentInParent<AlexWarhammer>();
        if (hammer != null) TryContact(hammer, other.ClosestPoint(transform.position));
    }

    public void TryContact(AlexWarhammer hammer, Vector3 position)
    {
        if (_resolved || _director == null || !_director.IsPlaying || !hammer.CanContact) return;
        _resolved = true; // Guard before publishing events or touching the pool.
        bool strike = hammer.IsStrike;
        bool success = _director.RegisterContact(this, hammer, strike, position);
        if (success && strike && Kind != AlexThrowKind.Dollar &&
            _director.TryGetReturnTime(out double arrival))
        {
            _returning = true;
            _origin = transform.position;
            _target = _director.ReturnPosition;
            _startTime = _director.SongTime;
            _arrivalTime = arrival;
            foreach (Collider hitbox in _colliders) hitbox.enabled = false;
        }
        else _pool.Release(this);
    }

    public void ResetForPool()
    {
        _resolved = true;
        _returning = false;
        _director = null;
        _pool = null;
    }
}
