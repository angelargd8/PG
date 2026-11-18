using UnityEngine;

public sealed class PauseMenuFollower : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private float _distance = 1.5f;
    [SerializeField] private float _verticalOffset = -0.1f;

    [Header("Follow")]
    [SerializeField] private bool _followWhileVisible = false;

    private Transform _head;

    private void Awake()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "No se encontró una cámara con el tag MainCamera.",
                this
            );

            return;
        }

        _head = mainCamera.transform;
    }

    private void LateUpdate()
    {
        if (!_followWhileVisible)
        {
            return;
        }

        PlaceInFrontOfPlayer();
    }

    public void PlaceInFrontOfPlayer()
    {
        if (_head == null)
        {
            return;
        }

        Vector3 forward = _head.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            return;
        }

        forward.Normalize();

        transform.position =
            _head.position +
            forward * _distance +
            Vector3.up * _verticalOffset;

        Vector3 directionToPlayer =
            _head.position - transform.position;

        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToPlayer);
        }
    }
}