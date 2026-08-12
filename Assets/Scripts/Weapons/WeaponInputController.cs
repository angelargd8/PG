using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class WeaponInputController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference fireAction;

    [Header("Dependencies")]
    [SerializeField]
    private GunShooter gunShooter;

    private void OnEnable()
    {
        if (fireAction == null)
        {
            Debug.LogError(
                "[WeaponInputController] No se asignó Fire Action",
                this
            );

            return;
        }

        if (gunShooter == null)
        {
            Debug.LogError(
                "[WeaponInputController] No se asignó Gun Shooter",
                this
            );

            return;
        }

        fireAction.action.performed += HandleFirePerformed;

        Debug.Log(
            $"[WeaponInputController] Escuchando input: {fireAction.action.name}",
            this
        );
    }

    private void OnDisable()
    {
        if (fireAction != null)
        {
            fireAction.action.performed -= HandleFirePerformed;
        }
    }

    private void HandleFirePerformed(InputAction.CallbackContext context)
    {
        Debug.Log(
            "[WeaponInputController] Input de disparo recibido",
            this
        );

        gunShooter.Fire();
    }
}