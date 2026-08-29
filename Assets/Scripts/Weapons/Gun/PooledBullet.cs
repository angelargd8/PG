using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class PooledBullet : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private Rigidbody bulletRigidbody;

    private BulletPool ownerPool;
    private float remainingLifetime;
    private bool isInUse;

    private void Reset()
    {
        bulletRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (bulletRigidbody == null)
        {
            bulletRigidbody = GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Prepara y lanza la bala desde el punto indicado.
    /// </summary>
    public void Launch(
        BulletPool pool,
        Vector3 position,
        Quaternion rotation,
        float speed,
        float lifetime)
    {
        ownerPool = pool;
        remainingLifetime = Mathf.Max(0.01f, lifetime);
        isInUse = true;

        // La bala deja temporalmente el contenedor del pool.
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);

        bulletRigidbody.linearVelocity = Vector3.zero;
        bulletRigidbody.angularVelocity = Vector3.zero;

        gameObject.SetActive(true);

        bulletRigidbody.WakeUp();

        // La dirección de disparo es el eje Z azul de Bullet Point.
        bulletRigidbody.linearVelocity = transform.forward * speed;
    }

    private void Update()
    {
        if (!isInUse)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        ReturnToPool();
    }

    /// <summary>
    /// Limpia el estado físico antes de devolver la bala al pool.
    /// </summary>
    public void PrepareForPool()
    {
        isInUse = false;
        remainingLifetime = 0f;
        ownerPool = null;

        bulletRigidbody.linearVelocity = Vector3.zero;
        bulletRigidbody.angularVelocity = Vector3.zero;
        bulletRigidbody.Sleep();
    }

    public void Despawn()
    {
        ReturnToPool();
    }


    private void ReturnToPool()
    {
        if (!isInUse)
        {
            return;
        }

        isInUse = false;

        BulletPool pool = ownerPool;
        ownerPool = null;

        pool?.Release(this);
    }
}