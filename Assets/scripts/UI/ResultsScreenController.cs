using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ResultsScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _modeText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _highScoreText;
    [SerializeField] private TMP_Text _newHighScoreText;
    [SerializeField] private GameObject _scoreHud;

    [Header("Placement")]
    [SerializeField] private ResultsScreenFollower _follower;

    [Header("Events")]
    [SerializeField] private RunResultEventChannelSO _runResultReady;
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;


    private bool _isShowing;


    private void Awake()
    {
        if (_resultsPanel != null)
        {
            _resultsPanel.SetActive(false);
        }
    }


    private void OnEnable()
    {
        if (_runResultReady != null)
        {
            _runResultReady.Raised += HandleRunResultReady;
        }
    }


    private void OnDisable()
    {
        if (_runResultReady != null)
        {
            _runResultReady.Raised -= HandleRunResultReady;
        }

        RestoreGameplay();
    }


    private void HandleRunResultReady(RunResult result)
    {
        if (_isShowing)
        {
            return;
        }

        _isShowing = true;

        if (_modeText != null)
        {
            _modeText.text = result.Context.DisplayName;
        }

        if (_scoreText != null)
        {
            _scoreText.text = result.FinalScore.ToString();
        }

        if (_highScoreText != null)
        {
            _highScoreText.text =
                $"RÉCORD: {result.HighScore}";
        }

        if (_newHighScoreText != null)
        {
            _newHighScoreText.gameObject.SetActive(
                result.IsNewHighScore
            );
        }

        if (_scoreHud != null)
        {
            _scoreHud.SetActive(false);
        }

        if (_resultsPanel != null)
        {
            _resultsPanel.SetActive(true);
        }

        if (_follower != null)
        {
            _follower.PlaceInFrontOfPlayer();
        }

        FreezeGameplay();

        Debug.Log(
            $"[ResultsScreen] Showing results | " +
            $"Score: {result.FinalScore} | " +
            $"High Score: {result.HighScore} | " +
            $"New High Score: {result.IsNewHighScore}",
            this
        );
    }


    public void ReturnToMainMenu()
    {
        if (!_isShowing)
        {
            return;
        }

        RestoreGameplay();

        if (_mainMenuRequested == null)
        {
            Debug.LogError(
                "[ResultsScreen] MainMenuRequested no está asignado.",
                this
            );

            return;
        }

        _mainMenuRequested.RaiseEvent();
    }


    private void FreezeGameplay()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.RaiseEvent(true);
        }
    }


    private void RestoreGameplay()
    {
        if (!_isShowing)
        {
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.RaiseEvent(false);
        }

        if (_resultsPanel != null)
        {
            _resultsPanel.SetActive(false);
        }

        _isShowing = false;
    }
}