using UnityEngine;

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
    [SerializeField] private Hand hand = Hand.Right;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannelSO equipRequested;
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;

    [Header("Dependencies")]
    [SerializeField] private SceneWeaponFollower weaponFollower;


    private Renderer[] _renderers;
    private bool _isEquipped;


    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);

        SetWeaponVisible(false);
    }


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

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised += HandlePauseChanged;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
        }
    }


    private void OnDisable()
    {
        if (equipRequested != null)
        {
            equipRequested.Raised -= HandleEquipRequested;
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -= HandlePauseChanged;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
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

            anchor = rightAnchor != null
                ? rightAnchor.transform
                : null;
        }

        if (anchor == null)
        {
            Debug.LogError(
                $"[SceneWeaponEquipController] No se encontró {anchorName} en Bootstrap. Comprueba que esté activo.",
                this
            );

            return;
        }

        weaponFollower.Bind(anchor);

        _isEquipped = true;
        SetWeaponVisible(true);

        Debug.Log(
            $"[SceneWeaponEquipController] {name} equipada correctamente.",
            this
        );
    }


    private void HandlePauseChanged(bool isPaused)
    {
        SetWeaponVisible(
            _isEquipped && !isPaused
        );
    }


    private void HandleMainMenuRequested()
    {
        _isEquipped = false;

        SetWeaponVisible(false);

        if (weaponFollower != null)
        {
            weaponFollower.Unbind();
        }
    }


    private void SetWeaponVisible(bool isVisible)
    {
        if (_renderers == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = isVisible;
            }
        }
    }
}