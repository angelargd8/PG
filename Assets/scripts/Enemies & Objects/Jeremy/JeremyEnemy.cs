using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyEnemyCue _cue;


    [Header("Movement")]
    [SerializeField] private float _movementSpeed = 2f;
    [SerializeField] private float _arrivalDistance = 0.05f;


    private JeremyEnemyPool _pool;
    private Vector3 _targetPosition;
    private bool _isMoving;
    private bool _isResolved;


    public JeremyCutDirection ExpectedDirection { get; private set; }
    public JeremyHand ExpectedHand { get; private set; }


    private void Update()
    {
        if (!_isMoving)
        {
            return;
        }

        MoveTowardsTarget();
    }


    public void Initialize(
        JeremyEnemyPool pool, 
        Vector3 targetPosition, 
        JeremyCutDirection expectedDirection,
        JeremyHand expectedHand
    ){
        _pool = pool;
        _targetPosition = targetPosition;
        ExpectedDirection = expectedDirection;
        ExpectedHand = expectedHand;

        if (_cue != null)
        {
            _cue.Configure(expectedDirection, expectedHand);
        }

        _isResolved = false;
        _isMoving = true;
    }


    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _movementSpeed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);

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
        _isResolved = false;
        _pool = null;
    }

    public bool TryResolveHit()
    {
        if (_isResolved)
        {
            return false;
        }

        _isResolved = true;
        _isMoving = false;

        if (_pool != null)
        {
            _pool.ReleaseEnemy(this);
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }
}