using UnityEngine;

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

    /// <summary>
    /// Método público llamado por XR Grab Interactable → Activated.
    /// </summary>
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

        if (muzzleFlash != null)
        {
            muzzleFlash.Play(true);
        }

        if (shotAudioSource != null)
        {
            shotAudioSource.Play();
        }

        Debug.Log(
            $"[GunShooter] Disparo desde {bulletPoint.position}",
            this);
    }
}