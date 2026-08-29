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


    [Header("Initial Delay")]

    [Min(0f)]
    [SerializeField]
    private float minInitialDelay = 0.5f;

    [Min(0f)]
    [SerializeField]
    private float maxInitialDelay = 1.5f;


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
    }


    private void Update()
    {
        if (target == null ||
            bulletPoint == null ||
            bulletPool == null)
        {
            return;
        }


        Vector3 toTarget =
            target.position -
            bulletPoint.position;


        // Usamos distancia al cuadrado
        // para evitar sqrt.
        if (toTarget.sqrMagnitude >
            shootingRangeSquared)
        {
            return;
        }


        if (Time.time <
            nextFireTime)
        {
            return;
        }


        Shoot(toTarget);


        nextFireTime =
            Time.time +
            fireCooldown;
    }


    // =========================
    // CONFIGURATION
    // =========================

    public void Configure(
        Transform newTarget,
        BulletPool newBulletPool
    )
    {
        target =
            newTarget;

        bulletPool =
            newBulletPool;
    }


    // =========================
    // SHOOTING
    // =========================

    private void Shoot(
        Vector3 toTarget
    )
    {
        if (toTarget.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return;
        }


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