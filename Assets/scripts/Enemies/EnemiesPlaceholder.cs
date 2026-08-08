using UnityEngine;

public class EnemiesPlaceholder : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    private Transform target;

    void Start()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            target = mainCamera.transform;
        }
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 direction = (target.position - transform.position).normalized;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PooledBullet>() != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PooledBullet>() != null)
        {
            Destroy(gameObject);
        }
    }
}