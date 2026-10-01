using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremySwordAttachment : MonoBehaviour, IExperienceRuntime
{
    public enum Hand
    {
        Left,
        Right
    }

    [Header("Settings")]
    [SerializeField] private Hand _hand;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;


    private Transform _anchor;
    private Renderer[] _renderers;
    private bool _isAttached;
    private bool _attachmentRequested, _isPaused, _warnedMissingAnchor;
    private float _nextAnchorAttempt;


    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }


    private void OnEnable()
    {
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
        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -= HandlePauseChanged;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
        }
    }


    private void LateUpdate()
    {
        if (!_attachmentRequested) return;
        if (_anchor == null || !_anchor.gameObject.activeInHierarchy)
        {
            _isAttached = false;
            SetSwordVisible(false);
            if (Time.unscaledTime >= _nextAnchorAttempt) BeginExperience();
        }
        if (!_isAttached || _anchor == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            _anchor.position,
            _anchor.rotation
        );
    }


    public void BeginExperience()
    {
        _attachmentRequested = true;
        _nextAnchorAttempt = Time.unscaledTime + 1f;
        string anchorName = _hand == Hand.Left
            ? "LeftWeaponAnchor"
            : "RightWeaponAnchor";

        GameObject anchorObject = GameObject.Find(anchorName);

        if (anchorObject == null)
        {
            if (!_warnedMissingAnchor)
                Debug.LogWarning($"[JeremySwordAttachment] {anchorName} no esta disponible. Se reintentara cuando la mano vuelva a estar activa.", this);
            _warnedMissingAnchor = true;
            SetSwordVisible(false);
            return;
        }

        _anchor = anchorObject.transform;
        _warnedMissingAnchor = false;
        _isAttached = true;

        transform.SetPositionAndRotation(
            _anchor.position,
            _anchor.rotation
        );

        SetSwordVisible(!_isPaused);
    }


    public void EndExperience()
    {
        _attachmentRequested = false;
        _warnedMissingAnchor = false;
        _isAttached = false;
        _anchor = null;

        SetSwordVisible(false);
    }


    private void HandlePauseChanged(bool isPaused)
    {
        _isPaused = isPaused;
        SetSwordVisible(_isAttached && !isPaused);
    }


    private void HandleMainMenuRequested()
    {
        EndExperience();
    }


    private void SetSwordVisible(bool isVisible)
    {
        if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null) _renderers[i].enabled = isVisible;
        }
    }
}
