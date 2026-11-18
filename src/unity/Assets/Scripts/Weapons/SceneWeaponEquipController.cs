using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneWeaponEquipController : MonoBehaviour, IExperienceRuntime
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
    private bool _equipPending, _isPaused, _warnedMissingAnchor;
    private float _nextAnchorAttempt;
    private Transform _boundAnchor;

    private void Update()
    {
        if (!_equipPending) return;
        if (_isEquipped && (_boundAnchor == null || !_boundAnchor.gameObject.activeInHierarchy))
        {
            _isEquipped = false;
            if (weaponFollower != null) weaponFollower.Unbind();
            SetWeaponVisible(false);
        }
        if (!_isEquipped && Time.unscaledTime >= _nextAnchorAttempt) HandleEquipRequested();
    }


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
        _equipPending = true;
        _nextAnchorAttempt = Time.unscaledTime + 1f;
        if (_isEquipped) return;

        if (weaponFollower == null)
        {
            _equipPending = false;
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
            if (!_warnedMissingAnchor)
                Debug.LogWarning($"[SceneWeaponEquipController] {anchorName} no esta disponible. El arma esperara a que la mano vuelva a estar activa.", this);
            _warnedMissingAnchor = true;

            return;
        }

        weaponFollower.Bind(anchor);
        _boundAnchor = anchor;
        _warnedMissingAnchor = false;

        _isEquipped = true;
        SetWeaponVisible(!_isPaused);

        Debug.Log(
            $"[SceneWeaponEquipController] {name} equipada correctamente.",
            this
        );
    }


    private void HandlePauseChanged(bool isPaused)
    {
        _isPaused = isPaused;
        SetWeaponVisible(
            _isEquipped && !isPaused
        );
    }


    private void HandleMainMenuRequested()
    {
        _equipPending = false;
        _boundAnchor = null;
        _warnedMissingAnchor = false;
        _isEquipped = false;

        SetWeaponVisible(false);

        if (weaponFollower != null)
        {
            weaponFollower.Unbind();
        }
    }


    public void BeginExperience() => HandleEquipRequested();
    public void EndExperience() => HandleMainMenuRequested();

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
