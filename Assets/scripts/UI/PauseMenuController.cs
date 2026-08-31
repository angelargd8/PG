using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private PauseMenuFollower _pauseMenuFollower;

    [Header("Input")]
    [SerializeField] private InputActionReference _pauseAction;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;


    private bool _isPaused;


    private void Awake()
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }
    }


    private void OnEnable()
    {
        if (_pauseAction != null)
        {
            _pauseAction.action.performed += HandlePausePerformed;
            _pauseAction.action.Enable();
        }
        else
        {
            Debug.LogError("Pause Action no está asignada.", this);
        }
    }


    private void OnDisable()
    {
        if (_pauseAction != null)
        {
            _pauseAction.action.performed -= HandlePausePerformed;
            _pauseAction.action.Disable();
        }

        RestoreGameplay();
    }


    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }


    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }


    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(true);
        }

        if (_pauseMenuFollower != null)
        {
            _pauseMenuFollower.PlaceInFrontOfPlayer();
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.RaiseEvent(true);
        }

        Debug.Log("Experience paused.", this);
    }


    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.RaiseEvent(false);
        }

        Debug.Log("Experience resumed.", this);
    }


    public void ReturnToMainMenu()
    {
        if (_mainMenuRequested == null)
        {
            Debug.LogError("MainMenuRequested no está asignado.", this);
            return;
        }

        _mainMenuRequested.RaiseEvent();
    }


    private void RestoreGameplay()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        _isPaused = false;

        if (_pausePanel != null)
        {
            _pausePanel.SetActive(false);
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.RaiseEvent(false);
        }
    }
}