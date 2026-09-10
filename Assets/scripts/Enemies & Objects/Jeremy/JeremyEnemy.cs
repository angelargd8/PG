using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JeremyEnemyCue _cue;


    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;


    [Header("Movement")]
    [SerializeField] private float _arrivalDistance = 0.05f;


    private float _movementSpeed;
    private float _idealCutDistance;
    private JeremyEnemyPool _pool;
    private Vector3 _targetPosition;
    private bool _isMoving;
    private bool _isResolved;


    public JeremyCutDirection ExpectedDirection { get; private set; }
    public JeremyHand ExpectedHand { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    public double ExpectedHitTime { get; private set; }
    public float MovementSpeed => _movementSpeed;
    public float IdealCutDistance => _idealCutDistance;


    // Variables para pruebas
    // private bool _hasCrossedIdealDistance;
    // private ExperienceMusicClock _musicClock;


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
        JeremyHand expectedHand,
        DifficultyLevel difficulty,
        float movementSpeed,
        float idealCutDistance,
        double expectedHitTime
    ){
        _pool = pool;
        _targetPosition = targetPosition;
        ExpectedDirection = expectedDirection;
        ExpectedHand = expectedHand;
        Difficulty = difficulty;
        _movementSpeed = movementSpeed;
        _idealCutDistance = idealCutDistance;
        ExpectedHitTime = expectedHitTime;

        // _musicClock = FindFirstObjectByType<ExperienceMusicClock>();

        // if (_musicClock == null)
        // {
        //     Debug.LogError(
        //         "[JeremyEnemy] No se encontró ExperienceMusicClock.",
        //         this
        //     );
        // }

        if (_cue != null)
        {
            _cue.Configure(expectedDirection, expectedHand);
        }

        _isResolved = false;
        _isMoving = true;

        // _hasCrossedIdealDistance = false;
    }


    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _movementSpeed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);

        // if (!_hasCrossedIdealDistance && distanceToTarget <= _idealCutDistance)
        // {
        //     _hasCrossedIdealDistance = true;

        //     if (_musicClock != null)
        //     {
        //         double actualCrossingTime = _musicClock.SongTime;
        //         double offset = actualCrossingTime - ExpectedHitTime;

        //         Debug.Log(
        //             $"[JeremyEnemy] Ideal crossing | " +
        //             $"Expected: {ExpectedHitTime:F3} | " +
        //             $"Actual: {actualCrossingTime:F3} | " +
        //             $"Offset: {offset:F3}",
        //             this
        //         );
        //     }
        // }

        if (distanceToTarget <= _arrivalDistance)
        {
            ReachTarget();
        }
    }


    private void ReachTarget()
    {
        if (_isResolved)
        {
            return;
        }

        _isResolved = true;
        _isMoving = false;

        InteractionResult result = new InteractionResult(
            minigameId: "Jeremy",
            interactionType: InteractionType.SwordCut,
            outcome: InteractionOutcome.Missed,
            difficulty: Difficulty,
            expectedTime: ExpectedHitTime
        );

        if (_interactionRegistered != null)
        {
            _interactionRegistered.RaiseEvent(result);
        }

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
        // _hasCrossedIdealDistance = false;
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