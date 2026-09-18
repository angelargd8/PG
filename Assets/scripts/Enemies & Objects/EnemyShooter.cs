using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyShooter : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================

    [Header("Weapon")]

    [SerializeField]
    private Transform bulletPoint;


    // =========================
    // ANIMATION
    // =========================

    [Header("Animation")]

    [SerializeField]
    private Animator animator;


    private static readonly int ShootHash =
        Animator.StringToHash(
            "Shoot"
        );


    // =========================
    // AIM
    // =========================

    [Header("Aim")]

    [SerializeField]
    private float rotationSpeed = 120f;

    //[Tooltip(
    //    "orientacion del modelo. " +
    //    "Usar 180 si el modelo mira en direccion opuesta al +Z."
    //)]
    [SerializeField]
    private float rotationOffsetY = 180f;


    // =========================
    // SHOOTING
    // =========================

    [Header("Shot Configuration")]

    [Min(0.1f)]
    [SerializeField]
    private float shootingRange = 15f;

    [Min(0.1f)]
    [SerializeField]
    private float muzzleSpeed = 15f;

    [Min(0.1f)]
    [SerializeField]
    private float bulletLifetime = 5f;

    [Min(0.01f)]
    [SerializeField]
    private float fireCooldown = 1.5f;


    // =========================
    // INITIAL DELAY
    // =========================

    [Header("Initial Delay")]

    [Min(0f)]
    [SerializeField]
    private float minInitialDelay = 0.5f;

    [Min(0f)]
    [SerializeField]
    private float maxInitialDelay = 1.5f;

    [Header("Rhythm Feedback (Optional)")]
    [Tooltip("Particulas de aviso, un beat antes del disparo. Desactiva Play On Awake.")]
    [SerializeField] private ParticleSystem rhythmCueVfx;

    private DannielRhythmDirector rhythmDirector;
    private bool isRhythmControlled;

    public bool CanShootOnBeat =>
        isRhythmControlled && isActiveAndEnabled &&
        target != null && bulletPoint != null && bulletPool != null &&
        (target.position - bulletPoint.position).sqrMagnitude <= shootingRangeSquared &&
        (target.position - bulletPoint.position).sqrMagnitude > Mathf.Epsilon;


    // =========================
    // RUNTIME REFERENCES
    // =========================

    private Transform target;

    private BulletPool bulletPool;


    // =========================
    // RUNTIME
    // =========================

    private float nextFireTime;

    private float shootingRangeSquared;


    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        shootingRangeSquared =
            shootingRange *
            shootingRange;
    }


    private void OnEnable()
    {
        ScheduleInitialShot();
        if (rhythmDirector != null)
        {
            rhythmDirector.RegisterShooter(this);
        }
    }


    private void OnDisable()
    {
        if (rhythmDirector != null)
        {
            rhythmDirector.UnregisterShooter(this);
        }

        ClearRhythmCue();

        if (animator != null)
        {
            animator.ResetTrigger(
                ShootHash
            );
        }
    }


    private void Update()
    {
        if (
            target == null ||
            bulletPoint == null ||
            bulletPool == null
        )
        {
            return;
        }


        Vector3 toTarget =
            target.position -
            bulletPoint.position;


        // =========================
        // RANGE
        // =========================

        if (
            toTarget.sqrMagnitude >
            shootingRangeSquared
        )
        {
            return;
        }


        // =========================
        // ROTATE
        // =========================

        RotateTowardsTarget();

        if (isRhythmControlled)
        {
            return;
        }


        // =========================
        // COOLDOWN
        // =========================

        if (
            Time.time <
            nextFireTime
        )
        {
            return;
        }


        Shoot(
            toTarget
        );


        nextFireTime =
            Time.time +
            fireCooldown;
    }


    // =========================
    // CONFIGURATION
    // =========================

    public void Configure(
        Transform newTarget,
        BulletPool newBulletPool,
        DannielRhythmDirector newRhythmDirector = null
    )
    {
        if (rhythmDirector != null)
        {
            rhythmDirector.UnregisterShooter(this);
        }

        target =
            newTarget;

        bulletPool =
            newBulletPool;

        rhythmDirector = newRhythmDirector;
        isRhythmControlled = newRhythmDirector != null;

        if (rhythmDirector != null && isActiveAndEnabled)
        {
            rhythmDirector.RegisterShooter(this);
        }
    }

    public void ShowRhythmCue()
    {
        if (rhythmCueVfx != null)
        {
            rhythmCueVfx.Play(true);
        }
    }


    public void ClearRhythmCue()
    {
        if (rhythmCueVfx != null)
        {
            rhythmCueVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }


    public bool TryShootOnBeat()
    {
        if (!CanShootOnBeat || rhythmDirector == null || !rhythmDirector.IsRunning)
        {
            return false;
        }

        RotateTowardsTarget();
        Shoot(target.position - bulletPoint.position);
        return true;
    }


    // =========================
    // ROTATION
    // =========================

    private void RotateTowardsTarget()
    {
        if (target == null)
        {
            return;
        }


        Vector3 direction =
            target.position -
            transform.position;


        direction.y = 0f;


        if (
            direction.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return;
        }


        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );


        Quaternion offsetRotation =
            Quaternion.Euler(
                0f,
                rotationOffsetY,
                0f
            );


        Quaternion targetRotation =
            lookRotation *
            offsetRotation;


        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    // =========================
    // SHOOTING
    // =========================

    private void Shoot(
        Vector3 toTarget
    )
    {
        if (
            toTarget.sqrMagnitude <=
            Mathf.Epsilon
        )
        {
            return;
        }


        // =========================
        // ANIMATION
        // =========================

        if (animator != null)
        {
            animator.SetTrigger(
                ShootHash
            );
        }


        // =========================
        // BULLET
        // =========================

        Quaternion shotRotation =
            Quaternion.LookRotation(
                toTarget.normalized,
                Vector3.up
            );


        bulletPool.Spawn(
            bulletPoint.position,
            shotRotation,
            muzzleSpeed,
            bulletLifetime
        );
    }


    // =========================
    // TIMING
    // =========================

    private void ScheduleInitialShot()
    {
        float minDelay =
            Mathf.Min(
                minInitialDelay,
                maxInitialDelay
            );


        float maxDelay =
            Mathf.Max(
                minInitialDelay,
                maxInitialDelay
            );


        nextFireTime =
            Time.time +
            Random.Range(
                minDelay,
                maxDelay
            );
    }
}
