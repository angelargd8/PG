using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class WeaponInputController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _fireAction;

    [Header("Dependencies")]
    [SerializeField] private GunShooter _gunShooter;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;


    private bool _isPaused;
    private InputAction _subscribedAction;


    private void OnEnable()
    {
        if (_fireAction == null || _fireAction.action == null)
        {
            Debug.LogWarning(
                "[WeaponInputController] No se asignó Fire Action",
                this
            );

            return;
        }

        if (_gunShooter == null)
        {
            Debug.LogError(
                "[WeaponInputController] No se asignó Gun Shooter",
                this
            );

            return;
        }

        _subscribedAction = _fireAction.action;
        _subscribedAction.performed += HandleFirePerformed;

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised += HandlePauseChanged;
        }

        Debug.Log(
            $"[WeaponInputController] Escuchando input: {_fireAction.action.name}",
            this
        );
    }


    private void OnDisable()
    {
        if (_subscribedAction != null)
        {
            _subscribedAction.performed -= HandleFirePerformed;
            _subscribedAction = null;
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -= HandlePauseChanged;
        }
    }


    private void HandleFirePerformed(InputAction.CallbackContext context)
    {
        if (_isPaused || _gunShooter == null)
        {
            return;
        }

        Debug.Log(
            "[WeaponInputController] Input de disparo recibido",
            this
        );

        _gunShooter.Fire();
    }


    private void HandlePauseChanged(bool isPaused)
    {
        _isPaused = isPaused;
    }
}
