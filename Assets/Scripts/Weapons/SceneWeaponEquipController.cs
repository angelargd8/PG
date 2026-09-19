using UnityEngine;

//consumidor del evento

[DisallowMultipleComponent]
public sealed class SceneWeaponEquipController : MonoBehaviour
{
    public enum Hand
    {
        Right = 0,
        Left = 1
    }

    [Header("Attachment")]
    [Tooltip("Mano que seguira esta arma. Es independiente del input y de la vibracion.")]
    [SerializeField]
    private Hand hand = Hand.Right;

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

        string anchorName = hand == Hand.Left
            ? "LeftWeaponAnchor"
            : "RightWeaponAnchor";

        Transform anchor;

        if (hand == Hand.Left)
        {
            GameObject leftAnchor = GameObject.Find(anchorName);
            anchor = leftAnchor != null ? leftAnchor.transform : null;
        }
        else
        {
            RightWeaponAnchor rightAnchor =
                FindFirstObjectByType<RightWeaponAnchor>();
            anchor = rightAnchor != null ? rightAnchor.transform : null;
        }

        if (anchor == null)
        {
            Debug.LogError(
                $"[SceneWeaponEquipController] No se encontró {anchorName} en Bootstrap. Comprueba que esté activo.",
                this
            );

            return;
        }

        Debug.Log(
            $"[SceneWeaponEquipController] {anchorName} encontrado: {anchor.name}.",
            this
        );

        weaponFollower.Bind(anchor);

        Debug.Log(
            $"[SceneWeaponEquipController] {name} equipada correctamente.",
            this
        );
    }
}