using UnityEngine;

// Sample after SceneWeaponFollower.LateUpdate, including head motion caused by rotation.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class AlexWarhammer : MonoBehaviour
{
    [SerializeField] private SceneWeaponEquipController.Hand _hand;
    [SerializeField] private Collider _headCollider;
    [Min(0.01f)] [SerializeField] private float _minimumStrikeSpeed = 1.2f;
    [Tooltip("Descarta saltos de tracking o teletransportes mayores a esta distancia por frame.")]
    [Min(0.1f)] [SerializeField] private float _maxTrackingStep = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float _touchAmplitude = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float _strikeAmplitude = 0.6f;
    [Min(0.01f)] [SerializeField] private float _hapticDuration = 0.07f;

    private readonly RaycastHit[] _sweepHits = new RaycastHit[32];
    private Vector3 _previousPosition;
    private bool _sampled;
    public bool CanContact { get; private set; }
    public bool IsStrike { get; private set; }

    private void Awake()
    {
        if (_headCollider == null) _headCollider = GetComponentInChildren<Collider>();
    }

    private void OnEnable() { _sampled = CanContact = IsStrike = false; }

    private void LateUpdate()
    {
        if (_headCollider == null || !_headCollider.enabled) { CanContact = false; return; }
        Vector3 current = HeadPosition();
        Vector3 step = current - _previousPosition;
        CanContact = _sampled && Time.deltaTime > 0f && !AudioListener.pause &&
            step.sqrMagnitude <= _maxTrackingStep * _maxTrackingStep;
        IsStrike = CanContact && step.magnitude / Time.deltaTime >= _minimumStrikeSpeed;
        if (CanContact && step.sqrMagnitude > 0.000001f)
        {
            // Supplement triggers for fast hammer swings between physics ticks.
            Vector3 extents = _headCollider.bounds.extents;
            float radius = Mathf.Max(0.01f, Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z)));
            int count = Physics.SphereCastNonAlloc(_previousPosition, radius, step.normalized,
                _sweepHits, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var item = _sweepHits[i].collider.GetComponentInParent<AlexThrownObject>();
                if (item != null) item.TryContact(this, _sweepHits[i].point);
            }
        }
        _previousPosition = current;
        _sampled = true;
    }

    private Vector3 HeadPosition()
    {
        if (_headCollider is BoxCollider box) return box.transform.TransformPoint(box.center);
        if (_headCollider is SphereCollider sphere) return sphere.transform.TransformPoint(sphere.center);
        return _headCollider.bounds.center;
    }

    public void Pulse(bool strike)
    {
        var haptics = XRHapticFeedback.Instance;
        if (haptics == null) return;
        float amplitude = strike ? _strikeAmplitude : _touchAmplitude;
        if (_hand == SceneWeaponEquipController.Hand.Left)
            haptics.PulseLeft(amplitude, _hapticDuration);
        else haptics.PulseRight(amplitude, _hapticDuration);
    }
}
