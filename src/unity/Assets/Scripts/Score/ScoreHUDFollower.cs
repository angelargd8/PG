using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreHUDFollower : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private float _distance = 1.5f;
    [SerializeField] private float _horizontalOffset = 0.45f;
    [SerializeField] private float _verticalOffset = 0.3f;

    private Transform _head;


    private void Awake()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[ScoreHUDFollower] No se encontró una Main Camera.",
                this
            );

            return;
        }

        _head = mainCamera.transform;
    }


    private void LateUpdate()
    {
        if (_head == null)
        {
            return;
        }

        transform.position =
            _head.position +
            _head.forward * _distance +
            _head.right * _horizontalOffset +
            _head.up * _verticalOffset;

        transform.rotation = _head.rotation;
    }
}