using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public sealed class DannielPlayerHitbox :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("Placement")]
    [SerializeField] private float _verticalOffset = -0.15f;


    public event Action<Vector3> HitByEnemyProjectile;


    private Transform _head;
    private SphereCollider _collider;
    private int _enemyProjectileLayer;
    private bool _isRunning;


    private void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        _collider.isTrigger = true;

        _enemyProjectileLayer =
            LayerMask.NameToLayer("EnemyProjectile");
    }


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[DannielPlayerHitbox] No se encontró Main Camera.",
                this
            );

            return;
        }

        if (_enemyProjectileLayer < 0)
        {
            Debug.LogError(
                "[DannielPlayerHitbox] No existe la layer EnemyProjectile.",
                this
            );

            return;
        }

        _head = mainCamera.transform;
        _isRunning = true;

        UpdatePosition();
    }


    public void EndExperience()
    {
        _isRunning = false;
        _head = null;
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void LateUpdate()
    {
        if (!_isRunning ||
            _head == null)
        {
            return;
        }

        UpdatePosition();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!_isRunning)
        {
            return;
        }

        if (other.gameObject.layer != _enemyProjectileLayer)
        {
            return;
        }

        PooledBullet enemyBullet =
            other.GetComponentInParent<PooledBullet>();

        if (enemyBullet == null)
        {
            return;
        }

        Vector3 hitPosition =
            other.bounds.center;

        HitByEnemyProjectile?.Invoke(
            hitPosition
        );

        enemyBullet.Despawn();
    }


    private void UpdatePosition()
    {
        transform.position =
            _head.position +
            Vector3.up * _verticalOffset;
    }
}