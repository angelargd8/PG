using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ParticipantIdInput :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_InputField _inputField;


    private void Awake()
    {
        if (_inputField == null)
        {
            Debug.LogError(
                "[ParticipantIdInput] " +
                "InputField no asignado.",
                this
            );

            return;
        }

        _inputField.onValueChanged.AddListener(
            HandleValueChanged
        );
    }


    private void OnEnable()
    {
        RefreshFromSession();
    }


    private void OnDestroy()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.RemoveListener(
                HandleValueChanged
            );
        }
    }


    private void HandleValueChanged(
        string value)
    {
        ParticipantSession.SetParticipantId(
            value
        );
    }


    private void RefreshFromSession()
    {
        if (_inputField == null)
        {
            return;
        }

        _inputField.SetTextWithoutNotify(
            ParticipantSession.ParticipantId
        );
    }
}