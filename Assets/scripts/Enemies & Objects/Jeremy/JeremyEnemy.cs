using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _movementSpeed = 2f;
    [SerializeField] private float _arrivalDistance = 0.05f;


    private JeremyEnemyPool _pool;
    private Vector3 _targetPosition;
    private bool _isMoving;


    private void Update()
    {
        if (!_isMoving)
        {
            return;
        }

        MoveTowardsTarget();
    }


    public void Initialize(JeremyEnemyPool pool, Vector3 targetPosition)
    {
        _pool = pool;
        _targetPosition = targetPosition;
        _isMoving = true;
    }


    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            _movementSpeed * Time.deltaTime
        );

        float distanceToTarget = Vector3.Distance(
            transform.position,
            _targetPosition
        );

        if (distanceToTarget <= _arrivalDistance)
        {
            ReachTarget();
        }
    }


    private void ReachTarget()
    {
        _isMoving = false;

        if (_pool != null)
        {
            _pool.ReleaseEnemy(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


    public void ResetEnemy()
    {
        _isMoving = false;
        _pool = null;
    }
}