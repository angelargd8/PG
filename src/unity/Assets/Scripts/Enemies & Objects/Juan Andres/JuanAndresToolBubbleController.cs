using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresToolBubbleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem _bubbleParticles;
    [SerializeField] private Transform _movementPoint;

    [Header("Emission")]
    [SerializeField] private bool _requireContact = true;
    [SerializeField] private float _minimumSpeed = 0.05f;
    [SerializeField] private float _maximumSpeed = 1.2f;
    [SerializeField] private float _maximumRateOverTime = 35f;


    private ParticleSystem.EmissionModule _emission;
    private Vector3 _previousPosition;
    private int _contactCount;


    private void Awake()
    {
        if (_bubbleParticles == null)
        {
            return;
        }

        _emission = _bubbleParticles.emission;
        _emission.rateOverTime = 0f;

        _bubbleParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }


    private void OnEnable()
    {
        _previousPosition =
            _movementPoint != null
                ? _movementPoint.position
                : transform.position;
    }


    private void Update()
    {
        if (_bubbleParticles == null)
        {
            return;
        }

        Vector3 currentPosition =
            _movementPoint != null
                ? _movementPoint.position
                : transform.position;

        float speed =
            (currentPosition - _previousPosition).magnitude /
            Mathf.Max(Time.deltaTime, 0.0001f);

        _previousPosition = currentPosition;

        bool canEmit =
            !_requireContact ||
            _contactCount > 0;

        float rate = 0f;

        if (canEmit && speed >= _minimumSpeed)
        {
            float t =
                Mathf.InverseLerp(
                    _minimumSpeed,
                    _maximumSpeed,
                    speed
                );

            rate =
                Mathf.Lerp(
                    0f,
                    _maximumRateOverTime,
                    t
                );
        }

        _emission.rateOverTime = rate;

        if (rate > 0f)
        {
            if (!_bubbleParticles.isPlaying)
            {
                _bubbleParticles.Play();
            }
        }
        else
        {
            if (_bubbleParticles.isPlaying)
            {
                _bubbleParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }
    }


    public void BeginContact()
    {
        _contactCount++;
    }


    public void EndContact()
    {
        _contactCount = Mathf.Max(0, _contactCount - 1);
    }
}