using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresDogVisibilityController :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private GameObject _dogRoot;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;


    private bool _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (_dogRoot == null)
        {
            Debug.LogError(
                "[JuanAndresDogVisibilityController] Dog Root no está asignado.",
                this
            );

            return;
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised +=
                HandlePauseChanged;
        }

        _dogRoot.SetActive(true);

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -=
                HandlePauseChanged;
        }

        if (_dogRoot != null)
        {
            _dogRoot.SetActive(false);
        }

        _isRunning = false;
    }


    private void HandlePauseChanged(bool isPaused)
    {
        if (_dogRoot == null)
        {
            return;
        }

        _dogRoot.SetActive(
            !isPaused
        );
    }
}