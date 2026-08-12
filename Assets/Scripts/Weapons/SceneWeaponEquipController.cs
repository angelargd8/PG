using UnityEngine;

//consumidor del evento

[DisallowMultipleComponent]
public sealed class SceneWeaponEquipController : MonoBehaviour
{
    [Header("Event Channels")]

    [SerializeField]
    private VoidEventChannelSO equipRequested;


    [Header("Dependencies")]

    [SerializeField]
    private SceneWeaponFollower weaponFollower;


    private void OnEnable()
    {
        if (equipRequested != null)
        {
            equipRequested.Raised += HandleEquipRequested;
        }
        else
        {
            Debug.LogWarning(
                "[SceneWeaponEquipController] No se asignó Equip Requested.",
                this
            );
        }
    }


    private void OnDisable()
    {
        if (equipRequested != null)
        {
            equipRequested.Raised -= HandleEquipRequested;
        }
    }


    private void HandleEquipRequested()
    {
        Debug.Log(
            $"[SceneWeaponEquipController] Evento recibido para equipar {name}.",
            this
        );

        if (weaponFollower == null)
        {
            Debug.LogError(
                "[SceneWeaponEquipController] No se asignó SceneWeaponFollower.",
                this
            );

            return;
        }

        RightWeaponAnchor anchor =
            FindFirstObjectByType<RightWeaponAnchor>();

        if (anchor == null)
        {
            Debug.LogError(
                "[SceneWeaponEquipController] No se encontró RightWeaponAnchor en Bootstrap.",
                this
            );

            return;
        }

        Debug.Log(
            $"[SceneWeaponEquipController] RightWeaponAnchor encontrado: {anchor.name}.",
            this
        );

        weaponFollower.Bind(anchor.transform);

        Debug.Log(
            $"[SceneWeaponEquipController] {name} equipada correctamente.",
            this
        );
    }
}