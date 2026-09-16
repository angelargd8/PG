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
        string anchorName = _hand == Hand.Left
            ? "LeftWeaponAnchor"
            : "RightWeaponAnchor";

        GameObject anchorObject = GameObject.Find(anchorName);

        if (anchorObject == null)
        {
            Debug.LogError($"[JeremySwordAttachment] No se encontró {anchorName}.", this);
            return;
        }

        _anchor = anchorObject.transform;
        _isAttached = true;

        transform.SetPositionAndRotation(
            _anchor.position,
            _anchor.rotation
        );

        SetSwordVisible(true);
    }


    public void EndExperience()
    {
        _isAttached = false;
        _anchor = null;

        SetSwordVisible(false);
    }


    private void HandlePauseChanged(bool isPaused)
    {
        SetSwordVisible(!isPaused);
    }


    private void HandleMainMenuRequested()
    {
        EndExperience();
    }


    private void SetSwordVisible(bool isVisible)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = isVisible;
        }
    }
}