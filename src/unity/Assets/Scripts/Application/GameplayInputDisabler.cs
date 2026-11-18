using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class GameplayInputDisabler :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("Actions")]
    [SerializeField] private InputActionReference[] _actionsToDisable;


    private bool[] _wasEnabled;


    public void BeginExperience()
    {
        if (_actionsToDisable == null)
        {
            return;
        }

        _wasEnabled = new bool[_actionsToDisable.Length];

        for (int i = 0; i < _actionsToDisable.Length; i++)
        {
            InputActionReference actionReference = _actionsToDisable[i];

            if (actionReference == null || actionReference.action == null)
            {
                continue;
            }

            InputAction action = actionReference.action;

            _wasEnabled[i] = action.enabled;

            if (action.enabled)
            {
                action.Disable();
            }
        }
    }


    public void EndExperience()
    {
        if (_actionsToDisable == null || _wasEnabled == null)
        {
            return;
        }

        for (int i = 0; i < _actionsToDisable.Length; i++)
        {
            InputActionReference actionReference = _actionsToDisable[i];

            if (actionReference == null || actionReference.action == null)
            {
                continue;
            }

            if (_wasEnabled[i])
            {
                actionReference.action.Enable();
            }
        }

        _wasEnabled = null;
    }
}