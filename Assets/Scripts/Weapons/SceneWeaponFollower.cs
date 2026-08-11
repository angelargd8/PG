using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneWeaponFollower : MonoBehaviour
{
    [Header("Weapon Offset")]
    [SerializeField]
    private Vector3 positionOffset;

    [SerializeField]
    private Vector3 rotationOffset;

    private RightWeaponAnchor weaponAnchor;

    private void Start()
    {
        weaponAnchor = FindFirstObjectByType<RightWeaponAnchor>();

        if (weaponAnchor == null)
        {
            Debug.LogError(
                "[SceneWeaponFollower] No se encontro RightWeaponAnchor en las escenas cargadas",
                this
            );

            enabled = false;
            return;
        }

        Debug.Log(
            $"[SceneWeaponFollower] {name} conectado a RightWeaponAnchor",
            this
        );
    }

    private void LateUpdate()
    {
        if (weaponAnchor == null)
        {
            return;
        }

        Transform anchorTransform = weaponAnchor.transform;

        transform.position =
            anchorTransform.TransformPoint(positionOffset);

        transform.rotation =
            anchorTransform.rotation *
            Quaternion.Euler(rotationOffset);
    }
}