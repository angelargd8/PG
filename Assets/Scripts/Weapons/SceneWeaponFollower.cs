using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneWeaponFollower : MonoBehaviour
{
    [Header("Weapon Offset")]

    [SerializeField]
    private Vector3 positionOffset;

    [SerializeField]
    private Vector3 rotationOffset;

    private Transform weaponAnchor;


    public void Bind(Transform anchor)
    {
        if (anchor == null)
        {
            Debug.LogError(
                "[SceneWeaponFollower] Se intentó asignar un anchor nulo.",
                this
            );

            return;
        }

        weaponAnchor = anchor;

        ApplyPose();

        Debug.Log(
            $"[SceneWeaponFollower] {name} conectado a {anchor.name}.",
            this
        );
    }


    public void Unbind()
    {
        weaponAnchor = null;

        Debug.Log(
            $"[SceneWeaponFollower] {name} desconectado del Weapon Anchor.",
            this
        );
    }


    private void LateUpdate()
    {
        if (weaponAnchor == null)
        {
            return;
        }

        ApplyPose();
    }


    private void ApplyPose()
    {
        Vector3 targetPosition =
            weaponAnchor.TransformPoint(positionOffset);

        Quaternion targetRotation =
            weaponAnchor.rotation *
            Quaternion.Euler(rotationOffset);

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );
    }
}