using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremySword : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyHitEvaluator _hitEvaluator;
    [SerializeField] private Transform _directionReference;

    [Header("Cut Detection")]
    [SerializeField] private float _minimumCutSpeed = 0.5f;

    private Vector3 _previousPosition;
    private Vector3 _velocity;


    private void OnEnable()
    {
        ResolveDirectionReference();

        _previousPosition = transform.position;
    }


    private void Update()
    {
        _velocity =
            (transform.position - _previousPosition) /
            Time.deltaTime;

        _previousPosition = transform.position;
    }


    private void OnTriggerEnter(Collider other)
    {
        JeremyEnemy enemy = other.GetComponentInParent<JeremyEnemy>();

        if (enemy == null)
        {
            return;
        }

        if (_velocity.magnitude < _minimumCutSpeed)
        {
            return;
        }

        JeremyCutDirection direction = GetCutDirection(_velocity);

        _hitEvaluator.EvaluateHit(enemy, direction);
    }


    private void ResolveDirectionReference()
    {
        if (_directionReference != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[JeremySword] No se encontró una Main Camera.", this);
            return;
        }

        _directionReference = mainCamera.transform;
    }


    private JeremyCutDirection GetCutDirection(Vector3 velocity)
    {
        Vector3 normalizedVelocity = velocity.normalized;

        Vector3 right = _directionReference != null
            ? _directionReference.right
            : Vector3.right;

        Vector3 up = _directionReference != null
            ? _directionReference.up
            : Vector3.up;

        float horizontal = Vector3.Dot(normalizedVelocity, right);
        float vertical = Vector3.Dot(normalizedVelocity, up);

        if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
        {
            return horizontal > 0f
                ? JeremyCutDirection.Right
                : JeremyCutDirection.Left;
        }

        return vertical > 0f
            ? JeremyCutDirection.Up
            : JeremyCutDirection.Down;
    }
}