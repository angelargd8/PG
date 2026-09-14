using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponHandAttachment : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================

    [Header("References")]

    [SerializeField]
    private Animator animator;

    [Tooltip(
        "Opcional. Si se asigna, se usa este Transform " +
        "en lugar de buscar la mano en el Animator."
    )]
    [SerializeField]
    private Transform handOverride;


    // =========================
    // HAND
    // =========================

    [Header("Hand")]

    [SerializeField]
    private HumanBodyBones handBone =
        HumanBodyBones.RightHand;


    // =========================
    // POSITION
    // =========================

    [Header("Local Position")]

    [SerializeField]
    private Vector3 positionOffset =
        Vector3.zero;


    // =========================
    // ROTATION
    // =========================

    [Header("Local Rotation")]

    [SerializeField]
    private Vector3 rotationOffset =
        Vector3.zero;


    // =========================
    // SCALE
    // =========================

    [Header("Local Scale")]

    [SerializeField]
    private Vector3 localScale =
        Vector3.one;


    // =========================
    // UNITY
    // =========================

    private void Start()
    {
        AttachWeapon();
    }


    // =========================
    // ATTACH
    // =========================

    public void AttachWeapon()
    {
        Transform hand =
            ResolveHand();


        if (hand == null)
        {
            Debug.LogError(
                "[WeaponHandAttachment] " +
                "No se encontró el hueso de la mano.",
                this
            );

            return;
        }


        transform.SetParent(
            hand,
            false
        );


        transform.localPosition =
            positionOffset;


        transform.localRotation =
            Quaternion.Euler(
                rotationOffset
            );


        transform.localScale =
            localScale;
    }


    // =========================
    // RESOLVE HAND
    // =========================

    private Transform ResolveHand()
    {
        // Si queremos seleccionar
        // manualmente una mano.
        if (handOverride != null)
        {
            return handOverride;
        }


        if (animator == null)
        {
            Debug.LogError(
                "[WeaponHandAttachment] " +
                "Animator no asignado.",
                this
            );

            return null;
        }


        if (!animator.isHuman)
        {
            Debug.LogError(
                "[WeaponHandAttachment] " +
                "El Animator no utiliza un Avatar Humanoid.",
                this
            );

            return null;
        }


        return animator.GetBoneTransform(
            handBone
        );
    }
}