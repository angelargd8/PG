using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class GunShooter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private Transform bulletPoint;

    [SerializeField]
    private BulletPool bulletPool;

    [Header("Shot Configuration")]
    [Min(0.01f)]
    [SerializeField]
    private float muzzleSpeed = 25f;

    [Min(0.01f)]
    [SerializeField]
    private float bulletLifetime = 3f;

    [Min(0f)]
    [SerializeField]
    private float fireCooldown = 0.15f;

    [Header("Optional Feedback")]
    [SerializeField]
    private ParticleSystem muzzleFlash;

    [SerializeField]
    private AudioSource shotAudioSource;

    private float nextAllowedFireTime;

    [Header("Debug")]
    [SerializeField] private bool logShots;


    [Header("Haptic Feedback")]

    [SerializeField]
    private XRNode hapticHand =
        XRNode.RightHand;


    [Range(0f, 1f)]
    [SerializeField]
    private float hapticAmplitude = 0.3f;


    [Min(0.01f)]
    [SerializeField]
    private float hapticDuration = 0.05f;

    private void PlayShotHaptic()
    {
        XRHapticFeedback haptics =
            XRHapticFeedback.Instance;


        if (haptics == null)
        {
            return;
        }


        haptics.Pulse(
            hapticHand,
            hapticAmplitude,
            hapticDuration
        );
    }


    public void Fire()
    {
        if (Time.time < nextAllowedFireTime)
        {
            return;
        }

        if (bulletPoint == null)
        {
            Debug.LogWarning(
                "[GunShooter] No se asignó Bullet Point.",
                this);

            return;
        }

        if (bulletPool == null)
        {
            Debug.LogWarning(
                "[GunShooter] No se asignó Bullet Pool.",
                this);

            return;
        }

        nextAllowedFireTime = Time.time + fireCooldown;

        bulletPool.Spawn(
            bulletPoint.position,
            bulletPoint.rotation,
            muzzleSpeed,
            bulletLifetime
        );

        PlayShotHaptic();

        if (muzzleFlash != null)
        {
            muzzleFlash.Play(true);
        }

        if (shotAudioSource != null)
        {
            shotAudioSource.Play();
        }

        if (logShots)
        {
            Debug.Log(
                $"[GunShooter] Disparo desde {bulletPoint.position}",
                this);
        }
    }
}
